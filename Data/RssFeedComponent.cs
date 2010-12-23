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
using Latino.Web;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Data
{
    public class RssFeedComponent : StreamDataProducer
    {
        private string mUrl;
        private Set<Guid> mSetOld
            = new Set<Guid>();
        private Queue<Guid> mQueueOld
            = new Queue<Guid>();
        private const int mNumOld
            = 1000; // *** this is currently hardcoded
        private const string mTimeFmt
            = "ddd, dd MMM yyyy HH:mm:ss K"; // RFC 822 format (incl. time zone)
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
            SleepBetweenPolls = 300000; // poll every 5 minutes by default
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
            if (!mSetOld.Contains(guid))
            {                
                if (mSetOld.Count + 1 > mNumOld)
                {
                    mSetOld.Remove(mQueueOld.Dequeue());
                }
                mQueueOld.Enqueue(guid);
                mSetOld.Add(guid);
                DateTime time = DateTime.Now;
                string content = "";
                if (itemAttr.ContainsKey("link"))
                {
                    // get referenced Web page
                    try
                    {
                        content = WebUtils.GetWebPageDetectEncoding(itemAttr["link"]);
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e);
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
                itemAttr.Add("_time", time.ToString(mTimeFmt)); 
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
                xml = WebUtils.GetWebPageDetectEncoding(mUrl);
            }
            catch (Exception e)
            { 
                Log.Warning(e); 
                return null; 
            }
            Dictionary<string, string> channelAttr = new Dictionary<string, string>();
            DocumentCorpus corpus = new DocumentCorpus();
            XmlTextReader reader = new XmlTextReader(new StringReader(xml));
            // first pass: items
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
                    if (mStopped) { return null; }
                    ProcessItem(itemAttr, corpus);
                    if (mStopped) { return null; }
                }            
            }
            reader.Close();
            reader = new XmlTextReader(new StringReader(xml));
            // second pass: channel attributes
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "channel" && !reader.IsEmptyElement)
                {
                    // handle channel
                    while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "channel"))
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "item")
                            {
                                Utils.XmlSkip(reader, "item");
                            }
                            else 
                            {
                                // handle channel attributes                               
                                if (mChannelElements.Contains(reader.Name))
                                {
                                    string attrName = reader.Name;
                                    string value = Utils.XmlReadValue(reader, attrName);
                                    string oldValue;
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
            }
            reader.Close();
            if (corpus.Documents.Count > 0)
            {
                channelAttr.Add("_provider", GetType().ToString());
                channelAttr.Add("_source", mUrl);
                channelAttr.Add("_sleepBetweenPolls", SleepBetweenPolls.ToString());
                channelAttr.Add("_timeStart", timeStart.ToString(mTimeFmt));
                channelAttr.Add("_timeEnd", DateTime.Now.ToString(mTimeFmt));
                //Console.WriteLine("Channel attributes:");
                foreach (KeyValuePair<string, string> attr in channelAttr)
                {
                    //Console.WriteLine("{0} = \"{1}\"", attr.Key, attr.Value);
                    corpus.Features.SetFeatureValue(attr.Key, attr.Value);
                }
                Console.WriteLine("Got {0} news.", corpus.Documents.Count);
                return corpus;
            }
            else
            {
                return null;
            }
        }

        public void ResetHistory()
        {
            mSetOld.Clear();
            mQueueOld.Clear();
        }
    }
}
