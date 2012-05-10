/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentCorpusWriterComponent.cs
 *  Desc:    Document-corpus database writer
 *  Created: Jan-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text;
using System.Xml;
using System.IO;
using Latino.Persistance;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Persistance
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentCorpusWriterComponent
       |
       '-----------------------------------------------------------------------
    */
    public class DocumentCorpusWriterComponent : StreamDataConsumer
    {
        private DatabaseConnection mConnection
            = new DatabaseConnection();
        private static object mLock
            = new object();
        private bool mWriteToDatabase;
        private string mXmlDataRoot;
        private string mHtmlDataRoot;
        private bool mIsDumpWriter
            = false;

        public DocumentCorpusWriterComponent(string dbConnectionString, string xmlDataRoot, string htmlDataRoot) : base(typeof(DocumentCorpusWriterComponent))
        {
            mWriteToDatabase = dbConnectionString != null;
            mXmlDataRoot = xmlDataRoot;
            mHtmlDataRoot = htmlDataRoot;
            if (mWriteToDatabase)
            {
                mConnection.ConnectionString = dbConnectionString; // throws ArgumentNullException
                mConnection.Connect(); // throws OleDbException           
            }
        }

        public bool IsDumpWriter
        {
            get { return mIsDumpWriter; }
            set { mIsDumpWriter = value; }
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;
            string corpusId = corpus.Features.GetFeatureValue("guid").Replace("-", "");
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings); 
            corpus.WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            //DateTime now = DateTime.Now;
            //string recordId = now.ToString("HH_mm_ss_") + corpusId;
            DateTime timeEnd = DateTime.Parse(corpus.Features.GetFeatureValue("timeEnd"));
            string recordId = timeEnd.ToString("HH_mm_ss_") + corpusId;
            // write to file
            if (mXmlDataRoot != null)
            {
                string path = string.Format(@"{3}\{0}\{1}\{2}\", timeEnd.Year, timeEnd.Month, timeEnd.Day, mXmlDataRoot.TrimEnd('\\'));                
                if (!Directory.Exists(path))
                {
                    lock (mLock)
                    {
                        if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                    }
                }
                StreamWriter w = new StreamWriter(path + recordId + ".xml", /*append=*/false, Encoding.UTF8);
                w.Write(stringWriter.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
                w.Close();
            }
            if (mHtmlDataRoot != null)
            {
                string pathHtml = string.Format(@"{4}\{0}\{1}\{2}\{3}\", timeEnd.Year, timeEnd.Month, timeEnd.Day, recordId, mHtmlDataRoot.TrimEnd('\\'));
                if (!Directory.Exists(pathHtml))
                {
                    lock (mLock)
                    {
                        if (!Directory.Exists(pathHtml)) { Directory.CreateDirectory(pathHtml); }
                    }
                }
                corpus.MakeHtmlPage(pathHtml, /*inlineCss=*/true);
            }
            // write to database
            if (mWriteToDatabase)
            {
                bool success = mConnection.ExecuteNonQuery("insert into Corpora (id, title, language, sourceUrl, timeStart, timeEnd, siteId, rejected) values (?, ?, ?, ?, ?, ?, ?, ?)",
                    corpusId,
                    Utils.Truncate(corpus.Features.GetFeatureValue("title"), 400),
                    Utils.Truncate(corpus.Features.GetFeatureValue("language"), 100),
                    Utils.Truncate(corpus.Features.GetFeatureValue("sourceUrl"), 400),
                    Utils.Truncate(corpus.Features.GetFeatureValue("timeStart"), 26),
                    Utils.Truncate(corpus.Features.GetFeatureValue("timeEnd"), 26),
                    Utils.Truncate(corpus.Features.GetFeatureValue("siteId"), 100),
                    mIsDumpWriter 
                );
                if (!success) { mLogger.Warn("ConsumeData", "Unable to write to database."); }
                foreach (Document document in corpus.Documents)
                {
                    string documentId = new Guid(document.Features.GetFeatureValue("guid")).ToString("N");
                    string bpCharCountStr = document.Features.GetFeatureValue("bprBoilerplateCharCount");
                    string contentCharCountStr = document.Features.GetFeatureValue("bprContentCharCount");
                    string unseenContentCharCountStr = document.Features.GetFeatureValue("bprUnseenContentCharCount");
                    string unseenContent = document.Features.GetFeatureValue("unseenContent");
                    success = mConnection.ExecuteNonQuery("insert into Documents (id, corpusId, name, description, category, link, responseUrl, urlKey, time, pubDate, mimeType, contentType, charSet, contentLength, detectedLanguage, detectedCharRange, domain, bpCharCount, contentCharCount, rejected, unseenContent, unseenContentCharCount, rev) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        documentId,
                        corpusId,
                        Utils.Truncate(document.Name, 400),
                        Utils.Truncate(document.Features.GetFeatureValue("description"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("category"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("link"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("responseUrl"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("urlKey"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("time"), 26),
                        Utils.Truncate(document.Features.GetFeatureValue("pubDate"), 26),
                        Utils.Truncate(document.Features.GetFeatureValue("mimeType"), 80),
                        Utils.Truncate(document.Features.GetFeatureValue("contentType"), 40),
                        Utils.Truncate(document.Features.GetFeatureValue("charSet"), 40),
                        Convert.ToInt32(document.Features.GetFeatureValue("contentLength")),
                        Utils.Truncate(document.Features.GetFeatureValue("detectedLanguage"), 100),
                        Utils.Truncate(document.Features.GetFeatureValue("detectedCharRange"), 100),                        
                        Utils.Truncate(document.Features.GetFeatureValue("domainName"), 100),
                        bpCharCountStr == null ? null : (object)Convert.ToInt32(bpCharCountStr),
                        contentCharCountStr == null ? null : (object)Convert.ToInt32(contentCharCountStr),                        
                        mIsDumpWriter,
                        Utils.Truncate(unseenContent, 20),
                        unseenContentCharCountStr == null ? null : (object)Convert.ToInt32(unseenContentCharCountStr),
                        Convert.ToInt32(document.Features.GetFeatureValue("rev"))
                    );
                    if (!success) { mLogger.Warn("ConsumeData", "Unable to write to database."); }
                }
            }
        }

        // *** IDisposable interface implementation ***

        public new void Dispose()
        {
            base.Dispose();
            if (mWriteToDatabase)
            {
                try { mConnection.Disconnect(); }
                catch { }
            }
        }
    }
}