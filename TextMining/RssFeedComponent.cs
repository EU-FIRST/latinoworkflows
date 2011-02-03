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
        private string mUrl;
        private bool mIncludeRawFormat
            = false;
        private RssHistory mHistory
            = new RssHistory();
        private static Set<string> mChannelElements
            = new Set<string>(new string[] { "title", "link", "description", "language", "copyright", "managingEditor", "pubDate", "category" });
        private static Set<string> mItemElements
            = new Set<string>(new string[] { "title", "link", "description", "author", "category", "comments", "pubDate", "source" });

        public RssFeedComponent(string url)
        {
            Utils.ThrowException(url == null ? new ArgumentNullException("url") : null);
            Utils.ThrowException(!Uri.IsWellFormedUriString(url, UriKind.Absolute) ? new ArgumentValueException("url") : null);
            Utils.ThrowException(Array.IndexOf(new string[] { "http", "https" }, new Uri(url).Scheme) < 0 ? new ArgumentValueException("url") : null);
            mUrl = url;
            TimeBetweenPolls = 300000; // poll every 5 minutes by default
        }

        public string Url
        {
            get { return mUrl; }
        }

        public bool IncludeRawFormat
        {
            get { return mIncludeRawFormat; }
            set { mIncludeRawFormat = value; }
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
                        if (mIncludeRawFormat)
                        {
                            CookieContainer cookies = null;
                            string ascii = WebUtils.GetWebPage(itemAttr["link"], /*refUrl=*/null, ref cookies, Encoding.GetEncoding("ISO-8859-1"));
                            itemAttr.Add("raw", ascii);
                        }
                    }
                    catch (Exception e)
                    {
                        mLog.Warning("ProcessItem", e);
                    }
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
            // get RSS XML
            DateTime timeStart = DateTime.Now;
            string xml;
            try
            {
                mLog.Info("ProduceData", "Getting RSS XML from {0} ...", mUrl);
                xml = WebUtils.GetWebPageDetectEncoding(mUrl);
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
                channelAttr.Add("_source", mUrl);
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
                return corpus;
            }
            else
            {
                mLog.Info("ProduceData", "No new items.");
                return null;
            }
        }
    }
}
