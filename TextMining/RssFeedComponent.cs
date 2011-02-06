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
using Latino.Web;
using System.Threading;

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
        private RssHistory mHistory
            = new RssHistory();
        private int mPolitenessSleep
            = 1000;
        private static Set<string> mChannelElements
            = new Set<string>(new string[] { "title", "link", "description", "language", "copyright", "managingEditor", "pubDate", "category" });
        private static Set<string> mItemElements
            = new Set<string>(new string[] { "title", "link", "description", "author", "category", "comments", "pubDate", "source" });

        public RssFeedComponent(string rssUrl)
        {
            Utils.ThrowException(rssUrl == null ? new ArgumentNullException("rssUrl") : null);
            Utils.ThrowException(!Uri.IsWellFormedUriString(rssUrl, UriKind.Absolute) ? new ArgumentValueException("rssUrl") : null);
            Utils.ThrowException(Array.IndexOf(new string[] { "http", "https" }, new Uri(rssUrl).Scheme) < 0 ? new ArgumentValueException("rssUrl") : null);
            mSources = new ArrayList<string>(new string[] { rssUrl });
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public RssFeedComponent(IEnumerable<string> rssList)
        {
            Utils.ThrowException(rssList == null ? new ArgumentNullException("rssList") : null);
            Utils.ThrowException(!rssList.GetEnumerator().MoveNext() ? new ArgumentValueException("rssList") : null);
            mSources = new ArrayList<string>();
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
            AddSources(rssList); // throws ArgumentValueException
        }

        public ArrayList<string>.ReadOnly Sources
        {
            get { return mSources; }
        }

        public void AddSource(string rssUrl)
        {
            Utils.ThrowException(rssUrl == null ? new ArgumentNullException("rssUrl") : null);
            Utils.ThrowException(!Uri.IsWellFormedUriString(rssUrl, UriKind.Absolute) ? new ArgumentValueException("rssUrl") : null);
            Utils.ThrowException(Array.IndexOf(new string[] { "http", "https" }, new Uri(rssUrl).Scheme) < 0 ? new ArgumentValueException("rssUrl") : null);
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
                Utils.ThrowException(value <= 0 ? new ArgumentOutOfRangeException("PoliteSleep") : null);
                mPolitenessSleep = value;
            }
        }

        public RssHistory History
        {
            get { return mHistory; }
            set 
            {
                Utils.ThrowException(value == null ? new ArgumentNullException("History") : null);
                mHistory = value; 
            }
        }

        private static Guid MakeGuid(string title, string desc)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0} {1}", title, desc))));
        }

        private void ProcessItem(Dictionary<string, string> itemAttr, DocumentCorpus corpus)
        {
            string name = "";
            itemAttr.TryGetValue("title", out name);
            string desc = "";
            itemAttr.TryGetValue("description", out desc);
            Guid guid = MakeGuid(name, desc);
            mLog.Info("ProcessItem", "Found item \"{0}\" [{1}].", Utils.MakeOneLine(name, /*compact=*/true), guid.ToString("N"));
            if (!mHistory.Contains(guid))
            {
                mHistory.AddToHistory(guid);              
                DateTime time = DateTime.Now;
                string content = "";
                if (itemAttr.ContainsKey("link"))
                {
                    // get referenced Web page
                    try
                    {
                        mLog.Info("ProcessItem", "Getting HTML from {0} ...", itemAttr["link"]);
                        content = WebUtils.GetWebPageJsint(itemAttr["link"]);
                        if (mIncludeRawData)
                        {
                            CookieContainer cookies = null;
                            string ascii = WebUtils.GetWebPage(itemAttr["link"], /*refUrl=*/null, ref cookies, Encoding.GetEncoding("ISO-8859-1"), WebUtils.Timeout);
                            itemAttr.Add("raw", ascii);
                        }
                    }
                    catch (Exception e)
                    {
                        mLog.Warning("ProcessItem", e);
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
                DateTime timeStart = DateTime.Now;
                // get RSS XML
                string xml;
                try
                {
                    mLog.Info("ProduceData", "Getting RSS XML from {0} ...", url);
                    xml = WebUtils.GetWebPageDetectEncoding(url);
                }
                catch (Exception e)
                {
                    mLog.Warning("ProduceData", e);
                    return null;
                }
                Dictionary<string, string> channelAttr = new Dictionary<string, string>();
                DocumentCorpus corpus = new DocumentCorpus();
                XmlTextReader reader = new XmlTextReader(new StringReader(xml));
                // first pass: items
                mLog.Info("ProduceData", "Reading items ...");
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
                        ProcessItem(itemAttr, corpus);
                    }
                }
                reader.Close();
                reader = new XmlTextReader(new StringReader(xml));
                if (corpus.Documents.Count > 0)
                {
                    // second pass: channel attributes
                    mLog.Info("ProduceData", "Reading channel attributes ...");
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
                    mLog.Info("ProduceData", "{0} new items.", corpus.Documents.Count);
                    // dispatch data 
                    DispatchData(corpus); 
                }
                else
                {
                    mLog.Info("ProduceData", "No new items.");
                }   
                // stopped?
                if (mStopped) { return null; }
            }
            return null;
        }
    }
}
