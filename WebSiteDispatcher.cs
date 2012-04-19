/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    WebSiteDispatcher.cs
 *  Desc:    Sends data to the FIRST Web site
 *  Created: Apr-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Net;
using System.Text;
using System.Web;
using System.IO;
using Latino.Web;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Persistance
{
    /* .-----------------------------------------------------------------------
       |
       |  Class WebSiteDispatcher
       |
       '-----------------------------------------------------------------------
    */
    public class WebSiteDispatcher : StreamDataConsumer
    {
        private string mCsrftoken
            = null;
        private CookieContainer mCookies
            = null;

        public WebSiteDispatcher() : base(typeof(WebSiteDispatcher))
        {
        }

        private void GetDjangoCookie()
        {
            mCookies = new CookieContainer();
            WebUtils.GetWebPage("http://first-vm4.ijs.si/feed-form/", /*refUrl=*/null, ref mCookies);
            foreach (Cookie cookie in mCookies.GetCookies(new Uri("http://first-vm4.ijs.si/feed-form/")))
            {
                if (cookie.Name == "csrftoken") 
                { 
                    mCsrftoken = cookie.Value; 
                    break; 
                }
            }
        }

        private static string Normalize(string str)
        {
            StringBuilder result = new StringBuilder();
            foreach (char ch in str) { if (char.IsLetterOrDigit(ch)) { result.Append(ch); } }
            return result.ToString().ToLower();
        }

        private double IsSubstring(string text, string substr)
        {
            text = Normalize(text);
            substr = Normalize(substr);
            if (text.Contains(substr))
            {
                return (double)substr.Length / (double)text.Length;
            }
            return 0;
        }

        private bool SendDocumentCorpusInfo(DocumentCorpus corpus)
        {
            // taken from Latino.Web WebUtils.cs
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://first-vm4.ijs.si/feed-form/");            
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.2; en-US; rv:1.8.0.6) Gecko/20060728 Firefox/1.5.0.6";
            request.Accept = "text/xml,application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,*/*;q=0.5";
            request.Headers.Add("Accept-Language", "en-us,en;q=0.5");
            request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            // configure POST request
            request.CookieContainer = mCookies;
            request.Method = "POST";
            StringBuilder postData = new StringBuilder(string.Format("csrfmiddlewaretoken={0}&form-TOTAL_FORMS={1}&form-INITIAL_FORMS=0", mCsrftoken, corpus.Documents.Count));
            int i = 0;
            foreach (Document document in corpus.Documents)
            {
                string title = Utils.ToOneLine(document.Name, /*compact=*/true);
                TextBlock[] textBlocks = document.GetAnnotatedBlocks("TextBlock/Content");
                StringBuilder text = new StringBuilder();
                foreach (TextBlock textBlock in textBlocks)
                {
                    if (IsSubstring(textBlock.Text, title) < 0.2)
                    {
                        text.AppendLine(textBlock.Text);
                        if (text.Length > 600) { break; }
                    }
                }
                //&form-0-url=...&form-0-title=...&form-0-source=...&form-0-snippet=...&form-0-timestamp=...
                string docData = string.Format("&form-{5}-url={0}&form-{5}-title={1}&form-{5}-source={2}&form-{5}-snippet={3}&form-{5}-timestamp={4}",
                    HttpUtility.UrlEncode(document.Features.GetFeatureValue("responseUrl")),
                    HttpUtility.UrlEncode(HttpUtility.HtmlEncode(title)),
                    HttpUtility.UrlEncode(corpus.Features.GetFeatureValue("siteId")),
                    HttpUtility.UrlEncode(HttpUtility.HtmlEncode(Utils.ToOneLine(Utils.Truncate(text.ToString(), 500), /*compact=*/true)) + " ..."),
                    "2012-04-13+16%3A47%3A38",
                    i++);
                postData.Append(docData);
            }
            //Console.WriteLine(postData.ToString());
            byte[] buffer = Encoding.ASCII.GetBytes(postData.ToString());
            request.ContentLength = buffer.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(buffer, 0, buffer.Length);
            dataStream.Close();
            // send request
            try
            {
                request.GetResponse().Close();
                return true;
            }
            catch { return false; }
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;
            if (mCsrftoken == null || !SendDocumentCorpusInfo(corpus))
            {
                GetDjangoCookie();
                SendDocumentCorpusInfo(corpus);
            }
        }
    }
}