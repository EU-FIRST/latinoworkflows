/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentCorpusWriterComponent.cs
 *  Desc:    Document-corpus database writer
 *  Created: Jan-2011
 *
 *  Authors: Miha Grcar
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

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;
            string corpusId = Guid.NewGuid().ToString("N");
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings); 
            corpus.WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            DateTime now = DateTime.Now;
            string recordId = now.ToString("HH_mm_ss_") + corpusId;
            // write to file
            if (mXmlDataRoot != null)
            {                
                string path = string.Format(@"{3}\{0}\{1}\{2}\", now.Year, now.Month, now.Day, mXmlDataRoot.TrimEnd('\\'));                
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
                string pathHtml = string.Format(@"{4}\{0}\{1}\{2}\{3}\", now.Year, now.Month, now.Day, recordId, mHtmlDataRoot.TrimEnd('\\'));
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
                bool success = mConnection.ExecuteNonQuery("insert into Corpora (id, title, language, sourceUrl, timeStart, timeEnd) values (?, ?, ?, ?, ?, ?)",
                    corpusId,
                    Utils.Truncate(corpus.Features.GetFeatureValue("title"), 400),
                    Utils.Truncate(corpus.Features.GetFeatureValue("language"), 400),
                    Utils.Truncate(corpus.Features.GetFeatureValue("_sourceUrl"), 400),
                    Utils.Truncate(corpus.Features.GetFeatureValue("_timeStart"), 26),
                    Utils.Truncate(corpus.Features.GetFeatureValue("_timeEnd"), 26));
                if (!success) { mLogger.Warn("ConsumeData", "Unable to write to database."); }
                foreach (Document document in corpus.Documents)
                {
                    string documentId = new Guid(document.Features.GetFeatureValue("_guid")).ToString("N");
                    success = mConnection.ExecuteNonQuery("insert into Documents (id, corpusId, name, description, category, link, responseUrl, time, pubDate, mimeType, contentType, charSet, contentLength) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        documentId,
                        corpusId,
                        Utils.Truncate(document.Name, 400),
                        Utils.Truncate(document.Features.GetFeatureValue("description"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("category"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("link"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("_responseUrl"), 400),
                        Utils.Truncate(document.Features.GetFeatureValue("_time"), 26),
                        Utils.Truncate(document.Features.GetFeatureValue("pubDate"), 26),
                        Utils.Truncate(document.Features.GetFeatureValue("_mimeType"), 80),
                        Utils.Truncate(document.Features.GetFeatureValue("_contentType"), 40),
                        Utils.Truncate(document.Features.GetFeatureValue("_charSet"), 40),
                        Convert.ToInt64(document.Features.GetFeatureValue("_contentLength"))
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