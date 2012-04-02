/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    UrlTreeBoilerplateRemoverComponent.cs
 *  Desc:    Boilerplate remover component
 *  Created: Mar-2012
 *
 *  Authors: Borut Sluban, Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
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
           |  Class HistoryEntry
           |
           '-----------------------------------------------------------------------
        */
        private class HistoryEntry
        {
            public string mDocumentId;
            public string mResponseUrl;
            public ArrayList<ulong> mHashCodes;
            public bool mFullPath;

            public HistoryEntry(string responseUrl, ArrayList<ulong> hashCodes, bool fullPath, string documentId)
            {
                mResponseUrl = responseUrl;
                mHashCodes = hashCodes;
                mFullPath = fullPath;
                mDocumentId = documentId;
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

        private UrlTree mUrlTree
            = new UrlTree();
        private int mMinDocCount // TODO: make configurable
            = 5;
        private HeuristicsType mHeuristicsType // TODO: make configurable
            = HeuristicsType.Slow;
        private DatabaseConnection mDbConnection
            = null;

        private ArrayList<Queue<HistoryEntry>> mHistory
            = new ArrayList<Queue<HistoryEntry>>();
        private int mHistoryCount // TODO: make configurable
            = 5;//40000;
        private int mMinDomainDocCount // TODO: make configurable
            = 5;//10;

        // debugging code
        //private static object wl = new object();
        //private static StreamWriter w;

        //private static void write(string msg)
        //{
        //    lock (wl)
        //    {
        //        w = new StreamWriter("c:\\bpreport.txt", true);
        //        w.WriteLine(msg);
        //        w.Close();
        //    }
        //}
        // end of debugging code

        public UrlTreeBoilerplateRemoverComponent() : base(typeof(UrlTreeBoilerplateRemoverComponent))
        {
            mBlockSelector = "TextBlock";
        }

        public UrlTreeBoilerplateRemoverComponent(string dbConnectionString) : base(typeof(UrlTreeBoilerplateRemoverComponent))
        {
            Utils.ThrowException(dbConnectionString == null ? new ArgumentNullException("dbConnectionString") : null);
            mDbConnection = new DatabaseConnection();
            mDbConnection.ConnectionString = dbConnectionString;
            mDbConnection.Connect();
            mBlockSelector = "TextBlock";
            InitializeHistory();
        }

        private void InitializeHistory()
        {
            int i = 0;
            int c = 0;
            while (true)
            {
                DataTable table = mDbConnection.ExecuteQuery(string.Format("select top {0} tb.id, tb.hashCodes, d.responseUrl, d.urlKey from TextBlocks tb, Documents d where tb.id = d.id and queue = ? order by tb.time desc", mHistoryCount), i);
                if (table.Rows.Count == 0) { break; }
                c += table.Rows.Count;
                mHistory.Add(new Queue<HistoryEntry>());
                for (int j = table.Rows.Count - 1; j >= 0; j--)
                {
                    string documentId = (string)table.Rows[j]["id"];
                    string hashCodesBase64 = (string)table.Rows[j]["hashCodes"];
                    string responseUrl = (string)table.Rows[j]["responseUrl"];
                    string urlKey = (string)table.Rows[j]["urlKey"];
                    bool fullPath = urlKey.Contains("?");
                    byte[] buffer = Convert.FromBase64String(hashCodesBase64);
                    BinarySerializer memSer = new BinarySerializer(new MemoryStream(buffer));
                    ArrayList<ulong> hashCodes = new ArrayList<ulong>(memSer);
                    HistoryEntry entry = new HistoryEntry(responseUrl, hashCodes, fullPath, documentId);
                    mHistory.Last.Enqueue(entry);
                    mUrlTree.Insert(responseUrl, hashCodes, mMinDocCount, fullPath, /*insertUnique=*/true);
                }
                i++;
            }
            mLogger.Info("InitializeHistory", "Loaded {0} history queues, {1} entries altogether.", i, c);
        }

        private static void SetBlockAnnotation(Document doc, UrlTree.NodeInfo[] result, HeuristicsType hType, int i, string pathInfo, TextBlock textBlock)
        {
            UrlTree.NodeInfo firstNode = result[0];
            Pair<bool, string> heurResult = BpHeuristics(result, i, hType);
            if (heurResult.First)
            {
                textBlock.Annotation.Type = "TextBlock/Boilerplate";
            }
            else
            {
                textBlock.Annotation.Type = "TextBlock/Content";
            }
            textBlock.Annotation.Features.SetFeatureValue("BPR_nodeBlockCount", firstNode.TextBlockCounts[i].ToString());
            textBlock.Annotation.Features.SetFeatureValue("BPR_nodeLocation", firstNode.NodeLocation.ToString());
            textBlock.Annotation.Features.SetFeatureValue("BPR_nodeDocumentCount", firstNode.NodeDocumentCount.ToString());
            textBlock.Annotation.Features.SetFeatureValue("BPR_urlPart", firstNode.UrlPart);
            textBlock.Annotation.Features.SetFeatureValue("BPR_pathInfo", pathInfo);
            if (hType != HeuristicsType.Simple)
            {
                textBlock.Annotation.Features.SetFeatureValue("BPR_contentVsBoileplateVotes", heurResult.Second);
            }
        }

        private static Pair<bool, string> BpHeuristics(UrlTree.NodeInfo[] result, int i, HeuristicsType type)
        {
            if (type == HeuristicsType.Simple)
            {
                return result[0].TextBlockCounts[i] > 1 ? new Pair<bool, string>(true, null) : new Pair<bool, string>(false, null);
            }
            else
            {
                int voters = result.Length > 3 ? result.Length - 2 : 1;
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

        private static string GetPathInfo(UrlTree.NodeInfo[] result, int i)
        {
            string pathInfo = "";
            foreach (UrlTree.NodeInfo nodeInfo in result)
            {
                pathInfo += nodeInfo.UrlPart + ": " + nodeInfo.TextBlockCounts[i] + "/" + nodeInfo.NodeDocumentCount + ", ";
            }
            return pathInfo.TrimEnd(' ', ',');
        }

        private void AddToHistory(int i, HistoryEntry historyEntry)
        {                       
            while (mHistory.Count < i + 1) { mHistory.Add(new Queue<HistoryEntry>()); }
            Queue<HistoryEntry> queue = mHistory[i];
            queue.Enqueue(historyEntry);
            // write to database
            if (mDbConnection != null)
            {
                if (i == 0)
                {
                    BinarySerializer memSer = new BinarySerializer();
                    historyEntry.mHashCodes.Save(memSer);
                    byte[] buffer = ((MemoryStream)memSer.Stream).GetBuffer();
                    string hashCodesBase64 = Convert.ToBase64String(buffer);
                    mDbConnection.ExecuteNonQuery("insert into TextBlocks (id, hashCodes, queue, time) values (?, ?, 0, ?)", historyEntry.mDocumentId, hashCodesBase64, DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));
                }
                else
                {
                    mDbConnection.ExecuteNonQuery("update TextBlocks set queue = ? where id = ?", i, historyEntry.mDocumentId);
                }
            }
            // end of write to database
            //write(i + "\r\n" + historyEntry.mResponseUrl + "\r\n" + mUrlTree.ToString() + "\r\n");
            if (queue.Count > mHistoryCount)
            {
                //write("queue.Count > mHistoryCount");
                historyEntry = queue.Dequeue();
                string debugInfo;
                int docCount = mUrlTree.GetDomainDocCount(historyEntry.mResponseUrl, historyEntry.mFullPath, out debugInfo);
                //write(docCount.ToString());
                if (docCount < mMinDomainDocCount)
                {
                    //write("not enough!!");                
                    AddToHistory(i + 1, historyEntry);
                }
                else
                {
                    mUrlTree.Remove(historyEntry.mResponseUrl, historyEntry.mHashCodes, historyEntry.mFullPath, /*unique=*/true);
                    //write("removed!");                
                }
            }
        }

        private void AddToHistory(string responseUrl, ArrayList<ulong> hashCodes, bool fullPath, string documentId)
        {
            HistoryEntry historyEntry = new HistoryEntry(responseUrl, hashCodes, fullPath, documentId);
            AddToHistory(0, historyEntry);
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;            
            try
            {
                ArrayList<ArrayList<ulong>> corpusHashCodes = new ArrayList<ArrayList<ulong>>();
                foreach (Document document in corpus.Documents)
                {
                    string contentType = document.Features.GetFeatureValue("_contentType");
                    if (contentType != "Text") { continue; }
                    string docUrl = document.Features.GetFeatureValue("_responseUrl");
                    string nUrl = document.Features.GetFeatureValue("_urlKey");
                    TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                    ArrayList<ulong> hashCodes = new ArrayList<ulong>();
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        TextBlock block = blocks[i];
                        hashCodes.Add(UrlTree.ComputeHashCode(block.Text, /*alphaOnly=*/true));
                    }
                    bool fullPath = nUrl.Contains("?");
                    mUrlTree.Insert(docUrl, hashCodes, mMinDocCount, fullPath, /*insertUnique=*/true);
                    string documentId = new Guid(document.Features.GetFeatureValue("_guid")).ToString("N");
                    AddToHistory(docUrl, hashCodes, fullPath, documentId);
                    corpusHashCodes.Add(hashCodes);
                }
                int docIdx = 0;
                foreach (Document document in corpus.Documents)
                {
                    string contentType = document.Features.GetFeatureValue("_contentType");
                    if (contentType != "Text") { continue; }
                    string docUrl = document.Features.GetFeatureValue("_responseUrl");
                    string nUrl = document.Features.GetFeatureValue("_urlKey");
                    TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                    ArrayList<ulong> hashCodes = corpusHashCodes[docIdx++];
                    UrlTree.NodeInfo[] result = mUrlTree.Query(docUrl, hashCodes, mMinDocCount, /*fullPath=*/nUrl.Contains("?"));
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        TextBlock block = blocks[i];
                        string pathInfo = GetPathInfo(result, i);
                        SetBlockAnnotation(document, result, mHeuristicsType, i, pathInfo, block);
                    }
                    document.Features.SetFeatureValue("BPR_heuristicsType", mHeuristicsType.ToString());
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
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