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
    public class WebSiteDispatcher : DocumentConsumer
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
            byte[] buffer = Encoding.ASCII.GetBytes(string.Format("csrfmiddlewaretoken={0}&form-TOTAL_FORMS=4&form-INITIAL_FORMS=0&form-0-url=http%3A%2F%2Fwww.example.com%2F&form-0-title=assumenda+tenetur+est+nostrum+suscipit&form-0-source=ipsam&form-0-snippet=Delectus+error+labore%2C+iure+est+eligendi+eos+perferendis+natus+exercitationem+ducimus+nam+ipsum+rem%2C+dolorum+cum+officiis+est+iusto+voluptatem+cumque+reprehenderit+aliquam+et+amet+similique%2C+consequuntur+ea+excepturi%3F&form-0-timestamp=2012-04-10+16%3A39%3A40&form-1-url=http%3A%2F%2Fwww.example.com%2F&form-1-title=quam+ad+nam+quaerat&form-1-source=itaque&form-1-snippet=Possimus+unde+amet+facilis+dolore+quod+nostrum+iste+doloremque+dolorem+asperiores+delectus.&form-1-timestamp=2012-04-10+16%3A39%3A40&form-2-url=http%3A%2F%2Fwww.example.com%2F&form-2-title=optio+nihil+labore+illum+odio+commodi+odit&form-2-source=illum&form-2-snippet=Quibusdam+repudiandae+architecto+odio+blanditiis+tempore%2C+pariatur+accusantium+facilis+qui%3F+Ipsam+itaque+quia+nulla+cupiditate%2C+quis+debitis+cupiditate+ullam+ipsum+et+beatae+placeat+dicta+laborum%3F+Sunt+sequi+eum+quasi+necessitatibus+placeat%2C+earum+tempore+beatae+nobis+saepe+sunt+explicabo+sint%2C+culpa+in+incidunt+quisquam+voluptates+beatae+cupiditate+dolor+suscipit+magnam+eos%2C+impedit+possimus+magni+quod+rerum+quibusdam+enim+sit+adipisci.+Tempora+eius+ratione+repudiandae+magni+rerum+facere+explicabo%2C+cumque+inventore+quo+quaerat+quia+vero+exercitationem+libero+eius%3F&form-2-timestamp=2012-04-10+16%3A39%3A40&form-3-url=http%3A%2F%2Fwww.example.com%2F&form-3-title=quod+esse+ipsum+unde+non+amet+ipsam+officia+dolor&form-3-source=culpa&form-3-snippet=Laboriosam+modi+impedit+dicta%2C+expedita+ipsam+reiciendis+officia+rerum+qui+animi%3F+Accusamus+animi+fugit+optio+iusto+veniam+porro+impedit%2C+corrupti+similique+inventore+fuga+repudiandae+quo+libero+repellendus+voluptates+obcaecati+veniam%2C+aspernatur+nesciunt+reiciendis+quidem+alias%2C+voluptatum+dolore+est+explicabo+nulla+quaerat+ratione+porro+cupiditate+facilis+mollitia%2C+fuga+autem+reiciendis+necessitatibus+delectus%3F&form-3-timestamp=2012-04-10+16%3A39%3A40", mCsrftoken));
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

        protected override void ConsumeDocument(Document document)
        {
            if (mCsrftoken == null || !SendDocumentCorpusInfo(null))
            {
                GetDjangoCookie();
                SendDocumentCorpusInfo(null);
            }
        }
    }
}