/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    UrlFilterComponent.cs
 *  Desc:    URL filter component
 *  Created: Mar-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using Latino.WebMining;
using Latino.Persistance;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.WebMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class UrlFilterComponent
       |
       '-----------------------------------------------------------------------
    */
    public class UrlFilterComponent : StreamDataProcessor
    {
        /* .-----------------------------------------------------------------------
           |
           |  Class HistoryEntry
           |
           '-----------------------------------------------------------------------
        */
        private class HistoryEntry
        {
            public string mUrlKey;
            public DateTime mTime;

            public HistoryEntry(string urlKey, DateTime time)
            {
                mUrlKey = urlKey;
                mTime = time;
            }
        }

        private static Dictionary<string, Pair<Set<string>, Queue<HistoryEntry>>> mUrlInfo
            = new Dictionary<string, Pair<Set<string>, Queue<HistoryEntry>>>();

        private static int mMinQueueSize // TODO: make configurable
            = 100;
        private static int mMaxQueueSize // TODO: make configurable
            = 10000;
        private static int mHistoryAgeDays // TODO: make configurable
            = 14;

        private static UrlNormalizer mUrlNormalizer
            = new UrlNormalizer();

        private bool mCloneDumpOnFork // TODO: make configurable
            = false;
        private DispatchPolicy mDumpDispatchPolicy // TODO: make configurable
            = DispatchPolicy.ToAll;
        private Set<IDataConsumer> mDumpDataConsumers
            = new Set<IDataConsumer>();

        private DatabaseConnection mDbConnection
            = null;

        public UrlFilterComponent() : base(typeof(UrlFilterComponent))
        {
        }

        public UrlFilterComponent(string dbConnectionString) : base(typeof(UrlFilterComponent))
        {
            Utils.ThrowException(dbConnectionString == null ? new ArgumentNullException("dbConnectionString") : null);
            mDbConnection = new DatabaseConnection();
            mDbConnection.ConnectionString = dbConnectionString;
            mDbConnection.Connect();
        }

        public static void InitializeHistory(DatabaseConnection dbConnection)
        {
            Utils.ThrowException(dbConnection == null ? new ArgumentNullException("dbConnection") : null);
            Logger logger = Logger.GetLogger(typeof(UrlFilterComponent));
            logger.Info("InitializeHistory", "Loading history ...");
            mUrlInfo.Clear();
            DataTable domainsTbl = dbConnection.ExecuteQuery("select distinct domain from Documents where dump = 0");
            int domainCount = 0;
            DateTime then = DateTime.Now - new TimeSpan(mHistoryAgeDays, 0, 0, 0);
            foreach (DataRow row in domainsTbl.Rows)
            {
                string domainName = (string)row["domain"];
                DataTable urlInfoTbl = dbConnection.ExecuteQuery(string.Format("select top {0} h.time, d.urlKey from UrlHistory h, Documents d where d.id = h.id and d.domain = ? and h.time >= ? order by h.time desc", mMaxQueueSize), domainName, then.ToString(Utils.DATE_TIME_SIMPLE));
                if (urlInfoTbl.Rows.Count == 0) { continue; }
                domainCount++;
                Pair<Set<string>, Queue<HistoryEntry>> urlInfo = GetUrlInfo(domainName);
                for (int j = urlInfoTbl.Rows.Count - 1; j >= 0; j--)
                {
                    string urlKey = (string)urlInfoTbl.Rows[j]["urlKey"];
                    string timeStr = (string)urlInfoTbl.Rows[j]["time"];
                    urlInfo.First.Add(urlKey);
                    urlInfo.Second.Enqueue(new HistoryEntry(urlKey, DateTime.Parse(timeStr)));
                }
            }
            logger.Info("InitializeHistory", "Loaded history for {0} distinct domains.", domainCount);            
        }

        public void SubscribeDumpConsumer(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            mDumpDataConsumers.Add(dataConsumer);
        }

        public void UnsubscribeDumpConsumer(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            mDumpDataConsumers.Remove(dataConsumer);
        }

        public Set<IDataConsumer>.ReadOnly SubscribedDumpConsumers
        {
            get { return mDumpDataConsumers; }
        }

        private static string GetDomainName(string urlKey)
        {
            string domainName = urlKey.Split(':')[1].Trim('/');
            string tld = UrlNormalizer.GetTldFromDomainName(domainName);
            if (tld != null)
            {
                int c = tld.Split('.').Length + 1;
                string[] parts = domainName.Split('.');
                domainName = "";
                for (int i = parts.Length - 1; c > 0; c--, i--)
                {
                    domainName = parts[i] + "." + domainName;
                }
                domainName = domainName.TrimEnd('.');
            }
            return domainName;
        }

        private static Pair<Set<string>, Queue<HistoryEntry>> GetUrlInfo(string domainName)
        {
            lock (mUrlInfo)
            {
                if (!mUrlInfo.ContainsKey(domainName))
                {
                    Pair<Set<string>, Queue<HistoryEntry>> urlInfo = new Pair<Set<string>, Queue<HistoryEntry>>(new Set<string>(), new Queue<HistoryEntry>());
                    mUrlInfo.Add(domainName, urlInfo);
                    return urlInfo;
                }
                return mUrlInfo[domainName];
            }
        }

        private void AddUrlToCache(string documentId, string urlKey, Pair<Set<string>, Queue<HistoryEntry>> urlInfo)
        {
            DateTime time = DateTime.Now;
            lock (urlInfo.First)
            {
                urlInfo.First.Add(urlKey);
            }
            lock (urlInfo.Second)
            {                
                urlInfo.Second.Enqueue(new HistoryEntry(urlKey, time));
                if (urlInfo.Second.Count > mMinQueueSize)
                {
                    double ageDays = (time - urlInfo.Second.Peek().mTime).TotalDays;
                    if (urlInfo.Second.Count > mMaxQueueSize || ageDays > (double)mHistoryAgeDays)
                    {
                        // dequeue and remove
                        lock (urlInfo.First)
                        {
                            urlInfo.First.Remove(urlInfo.Second.Dequeue().mUrlKey);
                        }
                    }
                }                
            }
            if (mDbConnection != null)
            {
                mDbConnection.ExecuteNonQuery("insert into UrlHistory (id, time) values (?, ?)", documentId, time.ToString(Utils.DATE_TIME_SIMPLE));
            }
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            try
            {
                DocumentCorpus corpus = (DocumentCorpus)data;
                DocumentCorpus filteredCorpus = new DocumentCorpus();
                DocumentCorpus dumpCorpus = new DocumentCorpus();
                filteredCorpus.CopyFeaturesFrom(corpus);
                dumpCorpus.CopyFeaturesFrom(corpus);
                ArrayList<Document> dumpDocumentList = new ArrayList<Document>();
                foreach (Document document in corpus.Documents)
                {
                    try
                    {
                        string responseUrl = document.Features.GetFeatureValue("responseUrl");
                        if (responseUrl == null) { dumpDocumentList.Add(document); continue; }
                        bool blacklisted;
                        string urlKey = mUrlNormalizer.NormalizeUrl(responseUrl, document.Name, out blacklisted, UrlNormalizer.NormalizationMode.Heuristics);
                        document.Features.SetFeatureValue("urlKey", urlKey);
                        string domainName = GetDomainName(urlKey);
                        document.Features.SetFeatureValue("domainName", domainName);
                        Pair<Set<string>, Queue<HistoryEntry>> urlInfo = GetUrlInfo(domainName);
                        bool cached;
                        lock (urlInfo.First)
                        {
                            cached = urlInfo.First.Contains(urlKey);
                        }
                        if (cached || blacklisted)
                        {
                            dumpDocumentList.Add(document);
                            mLogger.Info("ProcessDocument", "Document dumped (urlKey={0}).", urlKey);
                            continue;
                        }
                        else
                        {
                            string documentId = new Guid(document.Features.GetFeatureValue("guid")).ToString("N");
                            AddUrlToCache(documentId, urlKey, urlInfo);
                        }
                    }
                    catch (Exception exception)
                    {
                        mLogger.Error("ProcessDocument", exception);                    
                    }
                }
                foreach (Document doc in dumpDocumentList)
                {
                    corpus.Remove(doc);
                    dumpCorpus.AddDocument(doc);
                }
                if (dumpCorpus.Documents.Count > 0)
                {
                    WorkflowUtils.DispatchData(this, dumpCorpus, mCloneDumpOnFork, mDumpDispatchPolicy, mDumpDataConsumers, mLogger);
                }
                return corpus.Documents.Count > 0 ? corpus : null;
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessData", exception);
                return data;
            }
        }

        // *** IDisposable interface implementation ***

        public new void Dispose()
        {
            base.Dispose();
            if (mDbConnection != null)
            {
                try { mDbConnection.Disconnect(); }
                catch { }
            }
        }
    }
}
