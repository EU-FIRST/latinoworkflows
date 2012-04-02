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
        private Set<string> mUrlCache
            = new Set<string>();
        private Queue<string> mUrlQueue
            = new Queue<string>();
        private int mMaxCacheSize // TODO: make configurable
            = 400000;
        
        private UrlNormalizer mUrlNormalizer;

        private bool mCloneDumpOnFork // TODO: make configurable
            = false;
        private DispatchPolicy mDumpDispatchPolicy // TODO: make configurable
            = DispatchPolicy.BalanceLoadMax;
        private Set<IDataConsumer> mDumpDataConsumers
            = new Set<IDataConsumer>();

        public UrlFilterComponent(string shitListConfigKey, string rulesConfigKey) : base(typeof(UrlFilterComponent))
        {
            mUrlNormalizer = new UrlNormalizer(shitListConfigKey, rulesConfigKey);
        }

        public UrlFilterComponent() : base(typeof(UrlFilterComponent))
        {
            mUrlNormalizer = new UrlNormalizer();
        }

        public void Initialize(DatabaseConnection dbConnection)
        {
            Utils.ThrowException(dbConnection == null ? new ArgumentNullException("dbConnection") : null);
            DataTable table = dbConnection.ExecuteQuery(string.Format("select top {0} urlKey from Documents where dump = 0 order by time desc", mMaxCacheSize));
            for (int i = table.Rows.Count - 1; i >= 0; i--)
            {
                AddToCache((string)table.Rows[i]["urlKey"]);
            }
            mLogger.Info("Initialize", "Loaded {0} URL keys ({1} URL keys now cached).", table.Rows.Count, mUrlCache.Count);
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

        private void AddToCache(string url)
        {
            if (mUrlQueue.Count == mMaxCacheSize)
            {
                string rmvUrl = mUrlQueue.Dequeue();
                mUrlCache.Remove(rmvUrl);
            }
            mUrlQueue.Enqueue(url);
            mUrlCache.Add(url);
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
                ArrayList<Document> dumpDocList = new ArrayList<Document>();
                foreach (Document doc in corpus.Documents)
                {
                    string responseUrl = doc.Features.GetFeatureValue("_responseUrl");
                    if (responseUrl == null) { dumpDocList.Add(doc); continue; }
                    bool onShitList;
                    string nUrl = mUrlNormalizer.NormalizeUrl(responseUrl, doc.Name, out onShitList, UrlNormalizer.NormalizationMode.Heuristics);
                    doc.Features.SetFeatureValue("_urlKey", nUrl);
                    if (mUrlCache.Contains(nUrl) || onShitList)
                    {
                        dumpDocList.Add(doc);
                        mLogger.Info("ProcessData", "Document dumped (urlKey={0}).", nUrl);
                        continue;
                    }
                    else
                    {
                        AddToCache(nUrl);
                    }
                }
                foreach (Document doc in dumpDocList)
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
            catch (Exception e)
            {
                mLogger.Error("ProcessData", e);
                return data;
            }
        }
    }
}
