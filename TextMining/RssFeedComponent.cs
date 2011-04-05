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
        private DatabaseConnection mDatabase
            = null;
        private bool mIncludeRawData
            = false;
        private bool mIncludeRssXml
            = false;
        private int mPolitenessSleep
            = 1000;
        private string mSiteId
            = null;
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
            get { return mDatabase; }
            set { mDatabase = value; }
        }

        public string SiteId
        {
            get { return mSiteId; }
        }

        private static Guid MakeGuid(string title, string desc, string pubDate)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0} {1} {2}", title, desc, pubDate))));
        }

        // TODO: allow mSiteId == null
        private bool CheckHistory(Guid guid, string link)
        {
            if (mDatabase == null || mSiteId == null) { return false; } 
            string itemId = guid.ToString("N");
            OleDbDataReader reader = mDatabase.ExecuteReader("select * from History where SiteId=? and ItemId=?", mSiteId, itemId);
            bool retVal = reader.HasRows;
            reader.Close();
            if (retVal)
            {
                reader = mDatabase.ExecuteReader("select * from History where SiteId=? and ItemId=? and Source=?", mSiteId, itemId, link);
                bool hasRows = reader.HasRows;
                reader.Close();
                if (!hasRows)
                {
                    mDatabase.ExecuteNonQuery("insert into History (SiteId, ItemId, Source) values (?, ?, ?)", mSiteId, itemId, link);
                }
            }
            else
            {
                mDatabase.ExecuteNonQuery("insert into History (SiteId, ItemId, Source) values (?, ?, ?)", mSiteId, itemId, link);
            }
            return retVal;
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
            if (!CheckHistory(guid, rssXmlUrl))
            {            
                DateTime time = DateTime.Now;
                string content = "";
                if (itemAttr.ContainsKey("link"))
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
    }
}
