/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RssFeedComponent.cs
 *  Desc:    RSS feed polling component
 *  Created: Dec-2010
 *
 *  Author:  Miha Grcar
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
using System.Text.RegularExpressions;
using Latino.Web;
using Latino.Persistance;
using Latino.Workflows.TextMining;
using System.Data.SqlClient;

namespace Latino.Workflows.WebMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class RssFeedComponent
       |
       '-----------------------------------------------------------------------
    */
    public class RssFeedComponent : StreamDataProducerPoll
    {
        /* .-----------------------------------------------------------------------
           |
           |  Enum ContentType
           |
           '-----------------------------------------------------------------------
        */
        [Flags]
        private enum ContentType
        {
            Xml = 1,
            Html = 2,
            Text = 4,
            Binary = 8
        }

        private ArrayList<string> mSources;
        private string mSiteId
            = null;

        private bool mIncludeRawData
            = false;
        private bool mIncludeRssXml
            = false;
        
        private int mPolitenessSleep
            = 1000;
        private static string mDbConnectionString
            = null;
                
        private RssHistory mHistory
            = new RssHistory();
        
        private static Set<string> mChannelElements
            = new Set<string>(new string[] { "title", "link", "description", "language", "copyright", "managingEditor", "pubDate", "category" });
        private static Set<string> mItemElements
            = new Set<string>(new string[] { "title", "link", "description", "author", "category", "comments", "pubDate", "source", "emm:entity" });

        private static Regex mHtmlMimeTypeRegex
            = new Regex(@"(text/html)|(application/xhtml\+xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex mXmlMimeTypeRegex
            = new Regex(@"(application/xml)|(application/[^+ ;]+\+xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex mTextMimeTypeRegex
            = new Regex(@"text/plain", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private int mSizeLimit // *** make this adjustable
            = 10485760;
        private ContentType mContentFilter // *** make this adjustable?
            = ContentType.Html | ContentType.Text;
        private int mMaxDocsPerCorpus
            = -1;

        private static ContentType GetContentType(string mimeType)
        {
            if (mHtmlMimeTypeRegex.Match(mimeType).Success)
            {
                return ContentType.Html;
            }
            else if (mXmlMimeTypeRegex.Match(mimeType).Success)
            {
                return ContentType.Xml;
            }
            else if (mTextMimeTypeRegex.Match(mimeType).Success)
            {
                return ContentType.Text;
            }
            else
            {
                return ContentType.Binary;
            }
        }

        public RssFeedComponent(string siteId) : base(typeof(RssFeedComponent))
        {
            mSiteId = siteId;
            mSources = new ArrayList<string>();
        }

        public RssFeedComponent(string rssUrl, string siteId) : base(typeof(RssFeedComponent))
        {            
            Utils.ThrowException(rssUrl == null ? new ArgumentNullException("rssUrl") : null);
            mSiteId = siteId;
            mSources = new ArrayList<string>(new string[] { rssUrl });
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public RssFeedComponent(IEnumerable<string> rssList, string siteId) : base(typeof(RssFeedComponent))
        {
            Utils.ThrowException(rssList == null ? new ArgumentNullException("rssList") : null);
            mSiteId = siteId;
            mSources = new ArrayList<string>();
            AddSources(rssList); // throws ArgumentNullException, ArgumentValueException
            //Utils.ThrowException(mSources.Count == 0 ? new ArgumentValueException("rssList") : null); // allow empty source list
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public static string DatabaseConnectionString
        {
            get { return mDbConnectionString; }
            set { mDbConnectionString = value; }
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

        public int MaxDocsPerCorpus
        {
            get { return mMaxDocsPerCorpus; }
            set { mMaxDocsPerCorpus = value; }
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

        public string SiteId
        {
            get { return mSiteId; }
        }

        public void Initialize(string dbConnectionString)
        {
            Utils.ThrowException(dbConnectionString == null ? new ArgumentNullException("dbConnectionString") : null);
            mDbConnectionString = dbConnectionString;
            Initialize();
        }

        public void Initialize()
        {
            mHistory.Load(mSiteId, mDbConnectionString);
        }

        private static Guid MakeGuid(string title, string desc, string pubDate)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0} {1} {2}", title, desc, pubDate))));
        }

        private Encoding GetEncoding(string charSet)
        {
            charSet = Regex.Replace(charSet, @"^utf(\d+)$", "utf-$1", RegexOptions.IgnoreCase);
            return Encoding.GetEncoding(charSet);
        }

        private void ProcessItem(Dictionary<string, string> itemAttr, DocumentCorpus corpus, string rssXmlUrl, string xml)
        {
            try
            {
                string name = "";
                itemAttr.TryGetValue("title", out name);
                string desc = "";
                itemAttr.TryGetValue("description", out desc);
                string pubDate = "";
                itemAttr.TryGetValue("pubDate", out pubDate);
                Guid guid = MakeGuid(name, desc, pubDate);
                mLogger.Info("ProcessItem", "Found item \"{0}\".", Utils.ToOneLine(name, /*compact=*/true));
                if (mDbConnectionString != null)
                {
                    string xmlHash = Utils.GetHashCode128(xml).ToString("N");
                    string category = null;
                    itemAttr.TryGetValue("category", out category);
                    string entities = null;
                    itemAttr.TryGetValue("emm:entity", out entities);
                    using (SqlConnection connection = new SqlConnection(mDbConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand cmd = new SqlCommand("insert into Sources (siteId, docId, sourceUrl, category, entities, xmlHash) values (@siteId, @docId, @sourceUrl, @category, @entities, @xmlHash)", connection))
                        {
                            WorkflowUtils.AssignParamsToCommand(cmd,
                                "siteId", Utils.Truncate(mSiteId, 100),
                                "docId", guid.ToString("N"),
                                "sourceUrl", Utils.Truncate(rssXmlUrl, 400),
                                "category", category,
                                "entities", entities,
                                "xmlHash", xmlHash);
                            cmd.ExecuteNonQuery();
                        }
                        using (SqlCommand cmd = new SqlCommand("insert into RssXml (hash, xml) values (@hash, @xml)", connection))
                        {
                            WorkflowUtils.AssignParamsToCommand(cmd,
                                "hash", xmlHash,
                                "xml", xml);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }                
                if (!mHistory.CheckHistory(guid))
                {
                    DateTime time = DateTime.Now;
                    string content = "";
                    if (itemAttr.ContainsKey("link") && itemAttr["link"].Trim() != "")
                    {
                        // get referenced Web page
                        mLogger.Info("ProcessItem", "Getting HTML from {0} ...", Utils.ToOneLine(itemAttr["link"], /*compact=*/true));
                        string mimeType, charSet;
                        string responseUrl;
                        CookieContainer cookies = null;
                        byte[] bytes = WebUtils.GetWebResource(itemAttr["link"], /*refUrl=*/null, ref cookies, WebUtils.DefaultTimeout, out mimeType, out charSet, mSizeLimit, out responseUrl);
                        if (bytes == null) 
                        {
                            mLogger.Info("ProcessItem", "Item rejected because of its size.");
                            mHistory.AddToHistory(guid, mSiteId);
                            return;                        
                        }
                        ContentType contentType = GetContentType(mimeType);
                        if ((contentType & mContentFilter) == 0) 
                        {
                            mLogger.Info("ProcessItem", "Item rejected because of its content type.");
                            mHistory.AddToHistory(guid, mSiteId);
                            return;
                        }
                        itemAttr.Add("responseUrl", responseUrl);
                        itemAttr.Add("mimeType", mimeType);
                        itemAttr.Add("contentType", contentType.ToString());
                        if (charSet == null) { charSet = "ISO-8859-1"; }
                        itemAttr.Add("charSet", charSet);
                        itemAttr.Add("contentLength", bytes.Length.ToString());
                        if (contentType == ContentType.Binary)
                        {
                            // save as base64-encoded binary data
                            content = Convert.ToBase64String(bytes);
                        }
                        else
                        { 
                            // save as text                                
                            content = GetEncoding(charSet).GetString(bytes);
                            if (mIncludeRawData)
                            {
                                itemAttr.Add("raw", Convert.ToBase64String(bytes));
                            }
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
                    itemAttr.Add("guid", guid.ToString());
                    itemAttr.Add("time", time.ToString(Utils.DATE_TIME_SIMPLE));
                    Document document = new Document(name, content);
                    foreach (KeyValuePair<string, string> attr in itemAttr)
                    {
                        document.Features.SetFeatureValue(attr.Key, attr.Value);
                    }
                    corpus.AddDocument(document);
                    mHistory.AddToHistory(guid, mSiteId);
                }
            }
            catch (Exception e)
            {
                mLogger.Warn("ProcessItem", e);
            }
        }

        private string FixXml(string xml)
        {
            int i = xml.IndexOf("<");
            if (i < 0) { return xml; }
            return xml.Substring(i); 
        }

        private void ReadChannelAttributes(string url, string xml, DateTime timeStart, Dictionary<string, string> channelAttr)
        { 
            XmlTextReader reader = new XmlTextReader(new StringReader(xml));
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
            channelAttr.Add("siteId", mSiteId);
            channelAttr.Add("provider", GetType().ToString());
            channelAttr.Add("sourceUrl", url);
            if (mIncludeRssXml) { channelAttr.Add("source", xml); }
            channelAttr.Add("timeBetweenPolls", TimeBetweenPolls.ToString());
            channelAttr.Add("timeStart", timeStart.ToString(Utils.DATE_TIME_SIMPLE));            
        }

        protected override object ProduceData()
        {
            for (int i = 0; i < mSources.Count; i++)
            {
                string url = mSources[i];
                int numNewItems = 0;
                try
                {                    
                    DateTime timeStart = DateTime.Now;
                    // get RSS XML
                    string xml;
                    try
                    {
                        mLogger.Info("ProduceData", "Getting RSS XML from {0} ...", url);
                        xml = WebUtils.GetWebPageDetectEncoding(url);
                        xml = FixXml(xml);
                    }
                    catch (Exception e)
                    {
                        mLogger.Error("ProduceData", e);
                        return null;
                    }
                    Dictionary<string, string> channelAttr = new Dictionary<string, string>();
                    DocumentCorpus corpus = new DocumentCorpus();
                    corpus.Features.SetFeatureValue("guid", Guid.NewGuid().ToString());
                    XmlTextReader reader = new XmlTextReader(new StringReader(xml));
                    // first pass: channel attributes
                    ReadChannelAttributes(url, xml, timeStart, channelAttr);
                    // second pass: items
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
                                        string emmTrigger = attrName == "category" ? reader.GetAttribute("emm:trigger") : null;
                                        string emmEntityId = attrName == "emm:entity" ? reader.GetAttribute("id") : null;
                                        string emmEntityName = attrName == "emm:entity" ? reader.GetAttribute("name") : null;
                                        string value = Utils.XmlReadValue(reader, attrName);
                                        if (value.Trim() != "")
                                        {
                                            string oldValue;
                                            if (attrName == "pubDate") { string tmp = Utils.NormalizeDateTimeStr(value); if (tmp != null) { value = tmp; } }
                                            if (emmTrigger != null)
                                            {
                                                value += " ; " + emmTrigger.Replace(';', ',').TrimEnd(' ', ',');
                                            }
                                            if (emmEntityId != null && emmEntityName != null)
                                            {
                                                value = string.Format("{0} ; {1} ; {2}", emmEntityName, value, emmEntityId);
                                            }
                                            if (itemAttr.TryGetValue(attrName, out oldValue))
                                            {
                                                itemAttr[attrName] = oldValue + " ;; " + value;
                                            }
                                            else
                                            {
                                                itemAttr.Add(attrName, value);
                                            }
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
                            ProcessItem(itemAttr, corpus, url, xml);
                            if (mMaxDocsPerCorpus > 0 && corpus.Documents.Count == mMaxDocsPerCorpus)
                            {
                                numNewItems += corpus.Documents.Count;
                                foreach (KeyValuePair<string, string> attr in channelAttr)
                                {
                                    corpus.Features.SetFeatureValue(attr.Key, attr.Value);
                                }
                                corpus.Features.SetFeatureValue("timeEnd", DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));
                                DispatchData(corpus);
                                corpus = new DocumentCorpus();
                                corpus.Features.SetFeatureValue("guid", Guid.NewGuid().ToString());
                            }
                        }
                    }
                    reader.Close();
                    if (corpus.Documents.Count > 0)
                    {
                        numNewItems += corpus.Documents.Count;
                        foreach (KeyValuePair<string, string> attr in channelAttr)
                        {
                            corpus.Features.SetFeatureValue(attr.Key, attr.Value);
                        }
                        corpus.Features.SetFeatureValue("timeEnd", DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));
                        DispatchData(corpus);                        
                    }
                    mLogger.Info("ProduceData", "{0} new items.", numNewItems);
                    // stopped?
                    if (mStopped) { return null; }
                }
                catch (Exception e)
                {
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
            private Pair<Set<Guid>, Queue<Guid>> mHistory
                = new Pair<Set<Guid>, Queue<Guid>>(new Set<Guid>(), new Queue<Guid>());
            private int mHistorySize
                = 30000; // TODO: make this configurable

            public void AddToHistory(Guid id, string siteId)
            {
                if (mHistorySize == 0) { return; }
                if (mHistory.First.Count + 1 > mHistorySize)
                {
                    mHistory.First.Remove(mHistory.Second.Dequeue());
                }
                mHistory.First.Add(id);
                mHistory.Second.Enqueue(id);
            }

            public bool CheckHistory(Guid id)
            {
                return mHistory.First.Contains(id);
            }

            public void Load(string siteId, string dbConnectionString)
            {
                if (mHistorySize > 0)
                {
                    using (SqlConnection connection = new SqlConnection(dbConnectionString))
                    {
                        connection.Open();
                        DataTable table = new DataTable();
                        if (siteId == null)
                        {
                            using (SqlCommand cmd = new SqlCommand(string.Format("select top {0} d.id from Documents d, Corpora c where d.corpusId = c.id and c.siteId is null order by time desc", mHistorySize), connection))
                            { 
                                using (SqlDataReader reader = cmd.ExecuteReader()) 
                                { 
                                    table.Load(reader); 
                                }
                            }
                        }
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand(string.Format("select top {0} d.id from Documents d, Corpora c where d.corpusId = c.id and c.siteId = @siteId order by time desc", mHistorySize), connection))
                            {
                                WorkflowUtils.AssignParamsToCommand(cmd, "siteId", Utils.Truncate(siteId, 400));
                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    table.Load(reader);
                                }
                            }
                        }
                        mHistory.First.Clear();
                        mHistory.Second.Clear();
                        for (int i = table.Rows.Count - 1; i >= 0; i--)
                        {
                            DataRow row = table.Rows[i];
                            Guid itemId = new Guid((string)row["id"]);
                            mHistory.First.Add(itemId);
                            mHistory.Second.Enqueue(itemId);
                        }
                    }
                }
            }
        }
    }
}
