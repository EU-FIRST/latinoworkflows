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
using System.Text.RegularExpressions;
using System.Web;
using Latino.Web;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Data
{
    public class RssFeedComponent : StreamDataProducer
    {
        private string mUrl;
        private DateTime mLastUpdateTime
            = DateTime.MinValue;
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
            Sleep = 3600000; // poll every hour by default
        }

        //public RssFeedComponent(string url, DateTime lastUpdateTime) : this(url) // throws ArgumentNullException, ArgumentValueException
        //{
        //  ...
        //}

        private void Skip(XmlTextReader reader, string attrName)
        {
            if (reader.IsEmptyElement) { return; }
            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == attrName)) ;
        }

        private string ReadValue(XmlTextReader reader, string attrName)
        {
            if (reader.IsEmptyElement) { return ""; }
            string text = "";
            while (reader.Read() && reader.NodeType != XmlNodeType.Text && reader.NodeType != XmlNodeType.CDATA && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == attrName)) ;
            if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
            {
                text = HttpUtility.HtmlDecode(reader.Value); // *** decode inside CDATA?
                Skip(reader, attrName);
            }
            return text;
        }

        private Document ProcessItem(Dictionary<string, string> itemAttr)
        {              
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
            string name;
            itemAttr.TryGetValue("title", out name);            
            //Console.WriteLine("name = \"{0}\"", name);
            //Console.WriteLine("html = \"{0}\"", content);
            if (itemAttr.ContainsKey("comments"))
            { 
                // handle comments 
                Console.WriteLine("!");
            }
            Document document = new Document(name, content);
            Console.WriteLine("Item attributes:");         
            foreach (KeyValuePair<string, string> attr in itemAttr)
            {
                Console.WriteLine("{0} = \"{1}\"", attr.Key, attr.Value);
                document.Features.SetFeatureValue(attr.Key, attr.Value);
            }
            return document;
        }

        protected override object ProduceData()
        {
            // get RSS XML
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
                                string value = ReadValue(reader, attrName);
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
                                Skip(reader, reader.Name);
                            }
                        }
                    }
                    corpus.Add(ProcessItem(itemAttr));
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
                                Skip(reader, "item");
                            }
                            else 
                            {
                                // handle channel attributes                               
                                if (mChannelElements.Contains(reader.Name))
                                {
                                    string attrName = reader.Name;
                                    string value = ReadValue(reader, attrName);
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
                                    Skip(reader, reader.Name);
                                }
                            }
                        }
                    }
                }
            }
            reader.Close();
            //Console.WriteLine("Channel attributes:");
            foreach (KeyValuePair<string, string> attr in channelAttr)
            {
                //Console.WriteLine("{0} = \"{1}\"", attr.Key, attr.Value);
                corpus.Features.SetFeatureValue(attr.Key, attr.Value);
            }
            return corpus;
        }
    }
}
