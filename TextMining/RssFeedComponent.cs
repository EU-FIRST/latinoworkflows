/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RssFeedComponent.cs
 *  Desc:    RSS feed polling component
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Net;
using System.Threading;
using System.Data.OleDb;
using System.Data;
using Latino.Web;
using Latino.Persistance;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class RssFeedComponent
       |
       '-----------------------------------------------------------------------
    */
    public class RssFeedComponent : StreamDataProducer
    {
        private ArrayList<string> mSources;
        private bool mIncludeRawData
            = false;
        private bool mIncludeRssXml
            = false;
        private int mPolitenessSleep
            = 1000;
        private string mSiteId
            = null;
        private DatabaseConnection mHistoryDatabase
            = null;
        private RssHistory mHistory
            = new RssHistory();
        private static Set<string> mChannelElements
            = new Set<string>(new string[] { "title", "link", "description", "language", "copyright", "managingEditor", "pubDate", "category" });
        private static Set<string> mItemElements
            = new Set<string>(new string[] { "title", "link", "description", "author", "category", "comments", "pubDate", "source" });

        private void CreateLogger(string siteId)
        {
            if (siteId == null) { mLogger = Logger.GetLogger(typeof(RssFeedComponent)); }
            else { mLogger = Logger.GetLogger(typeof(RssFeedComponent).ToString() + "." + siteId); }
        }

        public RssFeedComponent(string siteId) : base(null)
        {
            CreateLogger(mSiteId = siteId);
            mSources = new ArrayList<string>();
        }

        public RssFeedComponent(string rssUrl, string siteId) : base(null)
        {            
            Utils.ThrowException(rssUrl == null ? new ArgumentNullException("rssUrl") : null);
            CreateLogger(mSiteId = siteId);
            mSources = new ArrayList<string>(new string[] { rssUrl });
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public RssFeedComponent(IEnumerable<string> rssList, string siteId) : base(null)
        {
            Utils.ThrowException(rssList == null ? new ArgumentNullException("rssList") : null);
            CreateLogger(mSiteId = siteId);
            mSources = new ArrayList<string>();
            AddSources(rssList); // throws ArgumentNullException, ArgumentValueException
            //Utils.ThrowException(mSources.Count == 0 ? new ArgumentValueException("rssList") : null); // allow empty source list
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public ArrayList<string>.ReadOnly Sources
        {
            get { return mSources; }
        }

        public void AddSource(string rssUrl)
        {
            Utils.ThrowException(rssUrl == null ? new ArgumentNullException("rssUrl") : null);
            mSources.Add(rssUrl);
        }

        public void AddSources(IEnumerable<string> rssList)
        {
            Utils.ThrowException(rssList == null ? new ArgumentNullException("rssList") : null);
            foreach (string rssUrl in rssList)
            {
                AddSource(rssUrl); // throws ArgumentNullException, ArgumentValueException
            }
        }

        public bool IncludeRawData
        {
            get { return mIncludeRawData; }
            set { mIncludeRawData = value; }
        }

        public bool IncludeRssXml
        {
            get { return mIncludeRssXml; }
            set { mIncludeRssXml = value; }
        }

        public int PolitenessSleep
        {
            get { return mPolitenessSleep; }
            set
            {
                Utils.ThrowException(value <= 0 ? new ArgumentOutOfRangeException("PolitenessSleep") : null);
                mPolitenessSleep = value;
            }
        }

        public DatabaseConnection HistoryDatabase
        {
            get { return mHistoryDatabase; }
            set { mHistoryDatabase = value; }
        }

        public string SiteId
        {
            get { return mSiteId; }
        }

        public void LoadHistory()
        {
            Utils.ThrowException(mHistoryDatabase == null ? new InvalidOperationException() : null);
            mHistory.Load(mSiteId, mHistoryDatabase);
        }

        private static Guid MakeGuid(string title, string desc, string pubDate)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0} {1} {2}", title, desc, pubDate))));
        }

        private void ProcessItem(Dictionary<string, string> itemAttr, DocumentCorpus corpus, string rssXmlUrl)
        {
            string name = "";
            itemAttr.TryGetValue("title", out name);
            string desc = "";
            itemAttr.TryGetValue("description", out desc);
            string pubDate = "";
            itemAttr.TryGetValue("pubDate", out pubDate);
            Guid guid = MakeGuid(name, desc, pubDate);
            mLogger.Info("ProcessItem", "Found item \"{0}\" [{1}].", Utils.ToOneLine(name, /*compact=*/true), guid.ToString("N"));
            if (!mHistory.CheckHistory(guid, rssXmlUrl, mSiteId, mHistoryDatabase))
            {            
                DateTime time = DateTime.Now;
                string content = "";
                if (itemAttr.ContainsKey("link") && itemAttr["link"].Trim() != "")
                {
                    // get referenced Web page
                    try
                    {
                        mLogger.Info("ProcessItem", "Getting HTML from {0} ...", itemAttr["link"]);
                        content = WebUtils.GetWebPageDetectEncoding(itemAttr["link"]);
                        if (mIncludeRawData)
                        {
                            CookieContainer cookies = null;                            
                            Encoding extAsciiEnc = Encoding.GetEncoding("ISO-8859-1");
                            string ascii = WebUtils.GetWebPage(itemAttr["link"], /*refUrl=*/null, ref cookies, extAsciiEnc, WebUtils.DefaultTimeout);
                            ascii = Convert.ToBase64String(extAsciiEnc.GetBytes(ascii));
                            itemAttr.Add("raw", ascii);                            
                        }
                    }
                    catch (Exception e)
                    {
                        mLogger.Warn("ProcessItem", e);
                    }
                    Thread.Sleep(mPolitenessSleep); 
                }
                if (content == "")
                {
                    if (itemAttr.ContainsKey("description"))
                    {
                        content = itemAttr["description"];
                    }
                    else if (itemAttr.ContainsKey("title"))
                    {
                        content = itemAttr["title"];
                    }
                }
                //Console.WriteLine("name = \"{0}\"", name);
                //Console.WriteLine("html = \"{0}\"", content);
                if (itemAttr.ContainsKey("comments"))
                {
                    // TODO: handle comments 
                }
                itemAttr.Add("_guid", guid.ToString());
                itemAttr.Add("_time", time.ToString(Utils.DATE_TIME_SIMPLE));
                Document document = new Document(name, content);
                //Console.WriteLine("Item attributes:");
                foreach (KeyValuePair<string, string> attr in itemAttr)
                {
                    //Console.WriteLine("{0} = \"{1}\"", attr.Key, attr.Value);
                    document.Features.SetFeatureValue(attr.Key, attr.Value);
                }
                corpus.AddDocument(document);
            }
        }

        protected override object ProduceData()
        {
            for (int i = 0; i < mSources.Count; i++)
            {
                string url = mSources[i];
                try
                {
                    DateTime timeStart = DateTime.Now;
                    // get RSS XML
                    string xml;
                    try
                    {
                        mLogger.Info("ProduceData", "Getting RSS XML from {0} ...", url);
                        xml = WebUtils.GetWebPageDetectEncoding(url);
                    }
                    catch (Exception e)
                    {
                        mLogger.Warn("ProduceData", e);
                        return null;
                    }
                    Dictionary<string, string> channelAttr = new Dictionary<string, string>();
                    DocumentCorpus corpus = new DocumentCorpus();
                    XmlTextReader reader = new XmlTextReader(new StringReader(xml));
                    // first pass: items
                    mLogger.Info("ProduceData", "Reading items ...");
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "item" && !reader.IsEmptyElement)
                        {
                            Dictionary<string, string> itemAttr = new Dictionary<string, string>();
                            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "item"))
                            {
                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    // handle item attributes
                                    if (mItemElements.Contains(reader.Name))
                                    {
                                        string attrName = reader.Name;
                                        string value = Utils.XmlReadValue(reader, attrName);
                                        string oldValue;
                                        if (attrName == "pubDate") { string tmp = Utils.NormalizeDateTimeStr(value); if (tmp != null) { value = tmp; } }
                                        if (itemAttr.TryGetValue(attrName, out oldValue))
                                        {
                                            itemAttr[attrName] = oldValue + " ;; " + value;
                                        }
                                        else
                                        {
                                            itemAttr.Add(attrName, value);
                                        }
                                    }
                                    else
                                    {
                                        Utils.XmlSkip(reader, reader.Name);
                                    }
                                }
                            }
                            // stopped?
                            if (mStopped)
                            {
                                if (corpus.Documents.Count == 0) { return null; }
                                break;
                            }
                            ProcessItem(itemAttr, corpus, url);
                        }
                    }
                    reader.Close();
                    reader = new XmlTextReader(new StringReader(xml));
                    if (corpus.Documents.Count > 0)
                    {
                        // second pass: channel attributes
                        mLogger.Info("ProduceData", "Reading channel attributes ...");
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "channel" && !reader.IsEmptyElement)
                            {
                                // handle channel
                                while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "channel"))
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        // handle channel attributes                               
                                        if (mChannelElements.Contains(reader.Name))
                                        {
                                            string attrName = reader.Name;
                                            string value = Utils.XmlReadValue(reader, attrName);
                                            string oldValue;
                                            if (attrName == "pubDate") { string tmp = Utils.NormalizeDateTimeStr(value); if (tmp != null) { value = tmp; } }
                                            if (channelAttr.TryGetValue(attrName, out oldValue))
                                            {
                                                channelAttr[attrName] = oldValue + " ;; " + value;
                                            }
                                            else
                                            {
                                                channelAttr.Add(attrName, value);
                                            }
                                        }
                                        else
                                        {
                                            Utils.XmlSkip(reader, reader.Name);
                                        }
                                    }
                                }
                            }
                        }
                        reader.Close();
                        channelAttr.Add("_provider", GetType().ToString());
                        channelAttr.Add("_sourceUrl", url);
                        if (mIncludeRssXml) { channelAttr.Add("_source", xml); }
                        channelAttr.Add("_timeBetweenPolls", TimeBetweenPolls.ToString());
                        channelAttr.Add("_timeStart", timeStart.ToString(Utils.DATE_TIME_SIMPLE));
                        channelAttr.Add("_timeEnd", DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));
                        //Console.WriteLine("Channel attributes:");
                        foreach (KeyValuePair<string, string> attr in channelAttr)
                        {
                            //Console.WriteLine("{0} = \"{1}\"", attr.Key, attr.Value);
                            corpus.Features.SetFeatureValue(attr.Key, attr.Value);
                        }
                        mLogger.Info("ProduceData", "{0} new items.", corpus.Documents.Count);
                        // dispatch data 
                        DispatchData(corpus);
                    }
                    else
                    {
                        mLogger.Info("ProduceData", "No new items.");
                    }
                    // stopped?
                    if (mStopped) { return null; }
                }
                catch (Exception e)
                {
                    mLogger.Info("ProduceData", url);
                    mLogger.Error("ProduceData", e);
                }
            }
            return null;
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class RssHistory
           |
           '-----------------------------------------------------------------------
        */
        private class RssHistory 
        {
            private Pair<Dictionary<Guid, ArrayList<string>>, Queue<Guid>> mHistory
                = new Pair<Dictionary<Guid, ArrayList<string>>, Queue<Guid>>(new Dictionary<Guid, ArrayList<string>>(), new Queue<Guid>());
            private int mHistorySize
                = 30000; // TODO: make this adjustable

            private bool AddToHistory(Guid id, string source)
            {
                if (mHistorySize == 0) { return true; }
                if (!mHistory.First.ContainsKey(id))
                {
                    if (mHistory.First.Count + 1 > mHistorySize)
                    {
                        mHistory.First.Remove(mHistory.Second.Dequeue());
                    }
                    mHistory.First.Add(id, new ArrayList<string>(new string[] { source }));
                    mHistory.Second.Enqueue(id);
                    return true;
                }
                else
                {
                    ArrayList<string> links = mHistory.First[id];
                    if (!links.Contains(source)) 
                    { 
                        links.Add(source);
                        return true;
                    }
                }
                return false;
            }

            public bool CheckHistory(Guid id, string source, string siteId, DatabaseConnection historyDatabase)
            {
                bool retVal = mHistory.First.ContainsKey(id);
                bool historyChanged = AddToHistory(id, source);                
                if (historyChanged && historyDatabase != null) // write through
                {
                    string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    if (siteId != null)
                    {
                        historyDatabase.ExecuteNonQuery("insert into History (SiteId, ItemId, Source, Time) values (?, ?, ?, ?)", 
                            Utils.Trunc(siteId, 400), id.ToString("N"), Utils.Trunc(source, 900), timeStr);
                    }
                    else
                    {
                        historyDatabase.ExecuteNonQuery("insert into History (ItemId, Source, Time) values (?, ?, ?)", 
                            id.ToString("N"), Utils.Trunc(source, 900), timeStr);
                    }
                }
                return retVal;
            }

            public void Load(string siteId, DatabaseConnection historyDatabase)
            {
                if (mHistorySize > 0)
                {
                    DataTable t;
                    if (siteId == null)
                    {
                        t = historyDatabase.ExecuteQuery(string.Format("select top {0} * from History where SiteId is null order by Time desc", mHistorySize));
                    }
                    else
                    {
                        t = historyDatabase.ExecuteQuery(string.Format("select top {0} * from History where SiteId=? order by Time desc", mHistorySize), 
                            Utils.Trunc(siteId, 400));
                    }
                    mHistory.First.Clear();
                    mHistory.Second.Clear();
                    for (int i = t.Rows.Count - 1; i >= 0; i--)
                    {
                        DataRow row = t.Rows[i];
                        Guid itemId = new Guid((string)row["ItemId"]);
                        string source = (string)row["Source"];
                        if (!mHistory.First.ContainsKey(itemId))
                        {
                            mHistory.First.Add(itemId, new ArrayList<string>(new string[] { source }));
                            mHistory.Second.Enqueue(itemId);
                        }
                        else
                        {
                            mHistory.First[itemId].Add(source);
                        }
                    }
                }
            }
        }
    }
}
