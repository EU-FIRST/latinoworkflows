/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    UrlTreeBoilerplateRemoverComponent.cs
 *  Desc:    Boilerplate remover component 
 *  Created: Apr-2012
 *
 *  Authors: Borut Sluban, Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Latino.WebMining;
using Latino.Persistance;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class UrlTreeBoilerplateRemoverComponent
       |
       '-----------------------------------------------------------------------
    */
    public class UrlTreeBoilerplateRemoverComponent : DocumentProcessor
    {
        /* .-----------------------------------------------------------------------
           |
           |  Class UrlHistoryEntry
           |
           '-----------------------------------------------------------------------
        */
        private class UrlHistoryEntry
        {
            public string mUrlKey;
            public DateTime mTime;

            public UrlHistoryEntry(string urlKey, DateTime time)
            {
                mUrlKey = urlKey;
                mTime = time;
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class TextBlockHistoryEntry
           |
           '-----------------------------------------------------------------------
        */
        private class TextBlockHistoryEntry
        {
            public string mResponseUrl;
            public ArrayList<ulong> mHashCodes;
            public bool mFullPath;
            public DateTime mTime;

            public TextBlockHistoryEntry(string responseUrl, ArrayList<ulong> hashCodes, bool fullPath, DateTime time)
            {
                mResponseUrl = responseUrl;
                mHashCodes = hashCodes;
                mFullPath = fullPath;
                mTime = time;
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Enum HeuristicsType
           |
           '-----------------------------------------------------------------------
        */
        public enum HeuristicsType
        {
            Simple,
            Slow,
            Fast
        }

        /* .-----------------------------------------------------------------------
           |
           |  Enum RevInfo
           |
           '-----------------------------------------------------------------------
        */
        public class RevInfo
        {
            public int mRev
                = 1;

            public RevInfo(int rev)
            {
                mRev = rev;
            }
        }

        private static Dictionary<string, Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>>> mUrlInfo
            = new Dictionary<string, Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>>>();
        private static Dictionary<string, Pair<UrlTree, Queue<TextBlockHistoryEntry>>> mTextBlockInfo
            = new Dictionary<string, Pair<UrlTree, Queue<TextBlockHistoryEntry>>>();

        private static UrlNormalizer mUrlNormalizer
            = new UrlNormalizer();
        private static Dictionary<string, object> mLocks
            = new Dictionary<string, object>();

        private static int mMinQueueSize // TODO: make configurable
            = 100;
        private static int mMaxQueueSize // TODO: make configurable
            = 10000;//20000;
        private static int mHistoryAgeDays // TODO: make configurable
            = 14;//30;

        private static int mMinNodeDocCount // TODO: make configurable
            = 5;
        private static HeuristicsType mHeuristicsType // TODO: make configurable
            = HeuristicsType.Simple;
        private static int mExactDuplicateThreshold // TODO: make configurable
            = 100 - 1;

        private DatabaseConnection mDbConnection
            = null;

        public UrlTreeBoilerplateRemoverComponent() : base(typeof(UrlTreeBoilerplateRemoverComponent))
        {
            mBlockSelector = "TextBlock";
        }

        public UrlTreeBoilerplateRemoverComponent(string dbConnectionString) : base(typeof(UrlTreeBoilerplateRemoverComponent))
        {
            mDbConnection = new DatabaseConnection();
            mDbConnection.ConnectionString = dbConnectionString;
            mDbConnection.Connect();
            mBlockSelector = "TextBlock";
        }

        private static object AcquireLock(string name)
        {
            lock (mLocks)
            { 
                object lockObj;
                if (!mLocks.TryGetValue(name, out lockObj))
                {
                    mLocks.Add(name, lockObj = new object());
                }
                return lockObj;
            }
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

        private static Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>> GetUrlInfo(string domainName)
        {
            lock (mUrlInfo)
            {
                if (!mUrlInfo.ContainsKey(domainName))
                {
                    Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>> urlInfo = new Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>>(new Dictionary<string, RevInfo>(), new Queue<UrlHistoryEntry>());
                    mUrlInfo.Add(domainName, urlInfo);
                    return urlInfo;
                }
                return mUrlInfo[domainName];
            }
        }

        public static void InitializeHistory(DatabaseConnection dbConnection)
        {
            Logger logger = Logger.GetLogger(typeof(UrlTreeBoilerplateRemoverComponent));
            logger.Info("InitializeHistory", "Loading history ...");
            mUrlInfo.Clear();
            mTextBlockInfo.Clear();
            DataTable domainsTbl = dbConnection.ExecuteQuery("select distinct domain from Documents where domain is not null");
            int domainCount = 0;
            foreach (DataRow row in domainsTbl.Rows)
            {
                string domainName = (string)row["domain"];
                // load URL history
                DataTable urlInfoTbl = dbConnection.ExecuteQuery(string.Format("select top {0} d.id, d.corpusId, d.time, d.urlKey, r.maxRev from Documents d, (select urlKey, max(rev) as maxRev from Documents group by urlKey) r where r.urlKey = d.urlKey and d.rev = 1 and d.domain = ? order by time desc", mMaxQueueSize), domainName);
                if (urlInfoTbl.Rows.Count == 0) { continue; }
                DateTime then = DateTime.Parse((string)urlInfoTbl.Rows[0]["time"]) - new TimeSpan(mHistoryAgeDays, 0, 0, 0);
                domainCount++;
                Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>> urlInfo = GetUrlInfo(domainName);
                for (int j = urlInfoTbl.Rows.Count - 1; j >= 0; j--)
                {
                    int maxRev = (int)urlInfoTbl.Rows[j]["maxRev"];
                    string urlKey = (string)urlInfoTbl.Rows[j]["urlKey"];
                    string timeStr = (string)urlInfoTbl.Rows[j]["time"];
                    Guid corpusId = new Guid((string)urlInfoTbl.Rows[j]["corpusId"]);
                    Guid docId = new Guid((string)urlInfoTbl.Rows[j]["id"]);
                    DateTime time = DateTime.Parse(timeStr);
                    if (time >= then)
                    {
                        urlInfo.First.Add(urlKey, new RevInfo(maxRev));
                        urlInfo.Second.Enqueue(new UrlHistoryEntry(urlKey, time));
                    }
                }
                // load text block history
                DataTable textBlockInfoTbl = dbConnection.ExecuteQuery(string.Format("select top {0} tb.hashCodes, d.time, d.responseUrl, d.urlKey from TextBlocks tb, Documents d where d.corpusId = tb.corpusId and d.id = tb.docId and d.domain = ? order by d.time desc", mMaxQueueSize), domainName);
                if (textBlockInfoTbl.Rows.Count == 0) { continue; }
                //Console.WriteLine(domainName + " " + textBlockInfoTbl.Rows.Count.ToString());
                then = DateTime.Parse((string)textBlockInfoTbl.Rows[0]["time"]) - new TimeSpan(mHistoryAgeDays, 0, 0, 0); 
                Pair<UrlTree, Queue<TextBlockHistoryEntry>> textBlockInfo = GetTextBlockInfo(domainName);
                for (int j = textBlockInfoTbl.Rows.Count - 1; j >= 0; j--)
                {
                    string timeStr = (string)textBlockInfoTbl.Rows[j]["time"];
                    DateTime time = DateTime.Parse(timeStr);
                    if (time >= then)
                    {
                        string hashCodesBase64 = (string)textBlockInfoTbl.Rows[j]["hashCodes"];
                        string responseUrl = (string)textBlockInfoTbl.Rows[j]["responseUrl"];
                        string urlKey = (string)textBlockInfoTbl.Rows[j]["urlKey"];
                        byte[] buffer = Convert.FromBase64String(hashCodesBase64);
                        BinarySerializer memSer = new BinarySerializer(new MemoryStream(buffer));
                        ArrayList<ulong> hashCodes = new ArrayList<ulong>(memSer);
                        bool fullPath = urlKey.Contains("?");
                        TextBlockHistoryEntry entry = new TextBlockHistoryEntry(responseUrl, hashCodes, fullPath, time);
                        textBlockInfo.First.Insert(responseUrl, hashCodes, mMinNodeDocCount, fullPath, /*insertUnique=*/true);
                        textBlockInfo.Second.Enqueue(entry);
                    }
                }
            }
            logger.Info("InitializeHistory", "Loaded history for {0} distinct domains.", domainCount);
        }

        private void AddUrlToCache(string urlKey, DateTime time, Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>> urlInfo)
        {
            urlInfo.First.Add(urlKey, new RevInfo(1));
            urlInfo.Second.Enqueue(new UrlHistoryEntry(urlKey, time));
            if (urlInfo.Second.Count > mMinQueueSize)
            {
                double ageDays = (time - urlInfo.Second.Peek().mTime).TotalDays;
                if (urlInfo.Second.Count > mMaxQueueSize || ageDays > (double)mHistoryAgeDays)
                {
                    // dequeue and remove
                    urlInfo.First.Remove(urlInfo.Second.Dequeue().mUrlKey);
                }
            }
        }

        private static Pair<UrlTree, Queue<TextBlockHistoryEntry>> GetTextBlockInfo(string domainName)
        {
            lock (mTextBlockInfo)
            {
                if (!mTextBlockInfo.ContainsKey(domainName))
                {
                    Pair<UrlTree, Queue<TextBlockHistoryEntry>> textBlockInfo = new Pair<UrlTree, Queue<TextBlockHistoryEntry>>(new UrlTree(), new Queue<TextBlockHistoryEntry>());
                    mTextBlockInfo.Add(domainName, textBlockInfo);
                    return textBlockInfo;
                }
                return mTextBlockInfo[domainName];
            }
        }

        private void AddToUrlTree(Pair<UrlTree, Queue<TextBlockHistoryEntry>> textBlockInfo, string responseUrl, ArrayList<ulong> hashCodes, bool fullPath, string corpusId, string documentId, string domainName, DateTime time)
        {            
            UrlTree urlTree = textBlockInfo.First;
            Queue<TextBlockHistoryEntry> queue = textBlockInfo.Second;
            TextBlockHistoryEntry historyEntry = new TextBlockHistoryEntry(responseUrl, hashCodes, fullPath, time);
            urlTree.Insert(responseUrl, hashCodes, mMinNodeDocCount, fullPath, /*insertUnique=*/true);
            queue.Enqueue(historyEntry);
            if (queue.Count > mMinQueueSize)
            {
                double ageDays = (time - queue.Peek().mTime).TotalDays;
                if (queue.Count > mMaxQueueSize || ageDays > (double)mHistoryAgeDays)
                {
                    // dequeue and remove
                    TextBlockHistoryEntry oldestEntry = queue.Dequeue();
                    urlTree.Remove(oldestEntry.mResponseUrl, oldestEntry.mHashCodes, oldestEntry.mFullPath, /*unique=*/true);
                }
            }
            if (mDbConnection != null)
            {
                BinarySerializer memSer = new BinarySerializer();
                historyEntry.mHashCodes.Save(memSer);
                byte[] buffer = ((MemoryStream)memSer.Stream).GetBuffer();
                string hashCodesBase64 = Convert.ToBase64String(buffer, 0, (int)memSer.Stream.Position);
                mDbConnection.ExecuteNonQuery("insert into TextBlocks (corpusId, docId, hashCodes) values (?, ?, ?)", corpusId, documentId, hashCodesBase64);
            }
        }

        private static string GetPathInfo(UrlTree.NodeInfo[] result, int i)
        {
            string pathInfo = "";
            foreach (UrlTree.NodeInfo nodeInfo in result)
            {
                pathInfo += nodeInfo.UrlPart + ": " + nodeInfo.TextBlockCounts[i] + "/" + nodeInfo.NodeDocumentCount + ", ";
            }
            return pathInfo.TrimEnd(' ', ',');
        }

        private static Pair<bool, string> BpHeuristics(UrlTree.NodeInfo[] result, int i, HeuristicsType type)
        {
            if (type == HeuristicsType.Simple)
            {
                return result[0].TextBlockCounts[i] > 1 ? new Pair<bool, string>(true, null) : new Pair<bool, string>(false, null);
            }
            else
            {
                int voters = 0;
                foreach (UrlTree.NodeInfo nodeInfo in result)
                {
                    if ((nodeInfo.NodeLocation & UrlTree.NodeLocation.WithinTld) != 0 || (nodeInfo.NodeLocation & UrlTree.NodeLocation.Root) != 0) { break; }
                    voters++;
                }
                if (voters == 0) { voters = 1; }
                int bp = 0;
                int ct = 0;
                for (int j = 0; j < voters; j++)
                {
                    if (type == HeuristicsType.Slow)
                    {
                        if (result[j].TextBlockCounts[i] > ((result[j].NodeDocumentCount / 100) + 1)) { bp += 1; }
                        else { ct += 1; }
                    }
                    else if (type == HeuristicsType.Fast)
                    {
                        if (result[j].TextBlockCounts[i] > ((result[j].NodeDocumentCount / 50) + 1)) { bp += 1; }
                        else { ct += 1; }
                    }
                }
                if (bp == ct)
                {
                    string outStr = string.Format(@"{0} : {1}", ct, bp);
                    return type == HeuristicsType.Slow ? new Pair<bool, string>(true, outStr) : new Pair<bool, string>(false, outStr);
                }
                else
                {
                    string outStr = string.Format(@"{0} : {1}", ct, bp);
                    return bp > ct ? new Pair<bool, string>(true, outStr) : new Pair<bool, string>(false, outStr);
                }
            }
        }

        private static void SetBlockAnnotation(Document doc, UrlTree.NodeInfo[] result, HeuristicsType hType, int i, string pathInfo, TextBlock textBlock)
        {
            UrlTree.NodeInfo firstNode = result[0];
            Pair<bool, string> heurResult = BpHeuristics(result, i, hType);
            if (heurResult.First)
            {
                textBlock.Annotation.Type = "TextBlock/Boilerplate";
            }
            else if (firstNode.TextBlockCounts[i] == 0)
            {
                textBlock.Annotation.Type = "TextBlock/Content/Unseen";
            }
            else
            {
                textBlock.Annotation.Type = "TextBlock/Content";
            }
            textBlock.Annotation.Features.SetFeatureValue("bprNodeBlockCount", firstNode.TextBlockCounts[i].ToString());
            textBlock.Annotation.Features.SetFeatureValue("bprNodeLocation", firstNode.NodeLocation.ToString());
            textBlock.Annotation.Features.SetFeatureValue("bprNodeDocumentCount", firstNode.NodeDocumentCount.ToString());
            textBlock.Annotation.Features.SetFeatureValue("bprUrlPart", firstNode.UrlPart);
            textBlock.Annotation.Features.SetFeatureValue("bprPathInfo", pathInfo);
            if (hType != HeuristicsType.Simple)
            {
                textBlock.Annotation.Features.SetFeatureValue("bprContentVsBoileplateVotes", heurResult.Second);
            }
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            DocumentCorpus corpus = (DocumentCorpus)data;            
            try
            {
                // split corpus according to document domain names
                Dictionary<string, ArrayList<Document>> domainDocCollections = new Dictionary<string, ArrayList<Document>>();
                foreach (Document document in corpus.Documents)
                {
                    try
                    {
                        string responseUrl = document.Features.GetFeatureValue("responseUrl");
                        if (responseUrl == null) { continue; }
                        bool blacklisted;
                        string urlKey = mUrlNormalizer.NormalizeUrl(responseUrl, document.Name, out blacklisted, UrlNormalizer.NormalizationMode.Heuristics);
                        document.Features.SetFeatureValue("blacklisted", blacklisted.ToString());
                        document.Features.SetFeatureValue("urlKey", urlKey);
                        string domainName = GetDomainName(urlKey);
                        document.Features.SetFeatureValue("domainName", domainName);
                        ArrayList<Document> domainDocs;
                        if (!domainDocCollections.TryGetValue(domainName, out domainDocs))
                        {
                            domainDocCollections.Add(domainName, domainDocs = new ArrayList<Document>());
                        }
                        domainDocs.Add(document);
                    }
                    catch (Exception exception)
                    {
                        mLogger.Error("ProcessDocument", exception);
                    }
                }
                // lock and process each domain separately 
                foreach (KeyValuePair<string, ArrayList<Document>> domainInfo in domainDocCollections)
                {
                    string domainName = domainInfo.Key;
                    Pair<Dictionary<string, RevInfo>, Queue<UrlHistoryEntry>> urlInfo = GetUrlInfo(domainName);
                    Pair<UrlTree, Queue<TextBlockHistoryEntry>> textBlockInfo = GetTextBlockInfo(domainName);
                    lock (AcquireLock(domainName)) // domain lock acquired
                    { 
                        // detect duplicates
                        foreach (Document document in domainInfo.Value)
                        {
                            try
                            {                                
                                string urlKey = document.Features.GetFeatureValue("urlKey");
                                bool cached = urlInfo.First.ContainsKey(urlKey);
                                document.Features.SetFeatureValue("rev", "1");
                                if (cached) 
                                {
                                    RevInfo revInfo = urlInfo.First[urlKey];
                                    revInfo.mRev++;
                                    document.Features.SetFeatureValue("rev", revInfo.mRev.ToString());
                                    continue; 
                                }
                                AddUrlToCache(urlKey, DateTime.Parse(document.Features.GetFeatureValue("time")), urlInfo);
                            }
                            catch (Exception exception)
                            {
                                mLogger.Error("ProcessDocument", exception);
                            }
                        }
                        // populate URL tree
                        ArrayList<ArrayList<ulong>> corpusHashCodes = new ArrayList<ArrayList<ulong>>();
                        foreach (Document document in domainInfo.Value)
                        {
                            try
                            {
                                string contentType = document.Features.GetFeatureValue("contentType");
                                if (contentType != "Text") { continue; }
                                string docUrl = document.Features.GetFeatureValue("responseUrl");
                                string urlKey = document.Features.GetFeatureValue("urlKey");
                                TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                                ArrayList<ulong> hashCodes = new ArrayList<ulong>();
                                for (int i = 0; i < blocks.Length; i++)
                                {
                                    TextBlock block = blocks[i];
                                    hashCodes.Add(UrlTree.ComputeHashCode(block.Text, /*alphaOnly=*/true));
                                }
                                if (document.Features.GetFeatureValue("rev") == "1")
                                {
                                    bool fullPath = urlKey.Contains("?");
                                    string documentId = document.Features.GetFeatureValue("guid").Replace("-", "");
                                    string corpusId = corpus.Features.GetFeatureValue("guid").Replace("-", "");
                                    AddToUrlTree(textBlockInfo, docUrl, hashCodes, fullPath, corpusId, documentId, domainName, DateTime.Parse(document.Features.GetFeatureValue("time")));
                                }
                                corpusHashCodes.Add(hashCodes);
                            }
                            catch (Exception exception)
                            {
                                mLogger.Error("ProcessDocument", exception);
                            }
                        }
                        // annotate boilerplate
                        int docIdx = 0;
                        foreach (Document document in domainInfo.Value)
                        {
                            try
                            {
                                string contentType = document.Features.GetFeatureValue("contentType");
                                if (contentType != "Text") { continue; }
                                string docUrl = document.Features.GetFeatureValue("responseUrl");
                                string urlKey = document.Features.GetFeatureValue("urlKey");                                
                                RevInfo revInfo = urlInfo.First[urlKey]; 
                                TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                                ArrayList<ulong> hashCodes = corpusHashCodes[docIdx++]; // document's hash codes
                                UrlTree urlTree = GetTextBlockInfo(domainName).First;
                                UrlTree.NodeInfo[] result = urlTree.Query(docUrl, hashCodes, mMinNodeDocCount, /*fullPath=*/urlKey.Contains("?"));
                                int bpCharCount = 0, contentCharCount = 0, unseenContentCharCount = 0;
                                ArrayList<ulong> unseenContentHashCodes = new ArrayList<ulong>();
                                for (int i = 0; i < blocks.Length; i++)
                                {
                                    TextBlock block = blocks[i];
                                    string pathInfo = GetPathInfo(result, i);
                                    SetBlockAnnotation(document, result, mHeuristicsType, i, pathInfo, block);
                                    if (block.Annotation.Type == "TextBlock/Boilerplate") { bpCharCount += block.Text.Length; }
                                    else { contentCharCount += block.Text.Length; }
                                    if (block.Annotation.Type == "TextBlock/Content/Unseen")
                                    {
                                        unseenContentCharCount += block.Text.Length;
                                        unseenContentHashCodes.Add(hashCodes[i]);
                                    }
                                }
                                document.Features.SetFeatureValue("bprBoilerplateCharCount", bpCharCount.ToString());
                                document.Features.SetFeatureValue("bprContentCharCount", contentCharCount.ToString());
                                if (document.Features.GetFeatureValue("rev") != "1")
                                {
                                    document.Features.SetFeatureValue("bprUnseenContentCharCount", unseenContentCharCount.ToString());
                                    if (unseenContentCharCount > mExactDuplicateThreshold)
                                    {
                                        document.Features.SetFeatureValue("unseenContent", "Yes");
                                        string documentId = document.Features.GetFeatureValue("guid").Replace("-", "");
                                        string corpusId = corpus.Features.GetFeatureValue("guid").Replace("-", "");
                                        AddToUrlTree(textBlockInfo, docUrl, unseenContentHashCodes, /*fullPath=*/urlKey.Contains("?"), corpusId, documentId, domainName, DateTime.Parse(document.Features.GetFeatureValue("time")));
                                    }
                                    else
                                    {
                                        document.Features.SetFeatureValue("unseenContent", "No");
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                mLogger.Error("ProcessDocument", exception);
                            }
                        }
                    } // domain lock released
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessData", exception);
            }
            return corpus;
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