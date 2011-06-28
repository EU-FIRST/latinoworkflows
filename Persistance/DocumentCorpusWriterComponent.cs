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
using System.Xml;
using System.IO;
using Latino.Workflows.TextMining;
using Latino.Persistance;
using System.Text;

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

        public DocumentCorpusWriterComponent(string dbConnectionString) : base(typeof(DocumentCorpusWriterComponent).ToString())
        {
            mConnection.ConnectionString = dbConnectionString; // throws ArgumentNullException
            mConnection.Connect(); // throws OleDbException            
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;
            //
            // debug code
            //
            // Jasmina
            string id = Guid.NewGuid().ToString("N");
            string path = @"C:\Users\Administrator\Desktop\htmls";// + "\\" + id;
            Directory.CreateDirectory(path);
            //corpus.MakeHtmlPage(path, true);
            //
            // end of debug code
            //
            string corpusId = Guid.NewGuid().ToString("N");
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings); 
            corpus.WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            //
            // debug code
            //
            // Jasmina
            StreamWriter w = new StreamWriter(path + "\\" + id + ".xml");
            w.Write(stringWriter.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", 
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
            w.Close();
            // Marko
            //string siteId = corpus.Features.GetFeatureValue("siteId");
            //if (siteId == null) { siteId = "(NULL)"; }
            //string __path = string.Format(@"E:\Users\miha\Work\DacqpipeMarko\Data\{0}\", siteId);
            //if (!Directory.Exists(__path)) { Directory.CreateDirectory(__path); }
            //foreach (Document document in corpus.Documents)
            //{
            //    if (document.Features.GetFeatureValue("_contentType") == "Html")
            //    {
            //        string fileName = __path + Guid.NewGuid().ToString("N") + ".html";
            //        byte[] bytes = Convert.FromBase64String(document.Features.GetFeatureValue("raw"));
            //        string html = Encoding.GetEncoding(document.Features.GetFeatureValue("_charSet")).GetString(bytes);
            //        StreamWriter w = new StreamWriter(fileName, /*append=*/false, Encoding.UTF8);
            //        w.WriteLine("<!--");
            //        w.WriteLine("pubDate: {0}", document.Features.GetFeatureValue("pubDate"));
            //        w.WriteLine("acqDate: {0}", document.Features.GetFeatureValue("_time"));
            //        w.WriteLine("link: {0}", document.Features.GetFeatureValue("link"));                    
            //        w.WriteLine("-->");
            //        w.Write(html);
            //        w.Close();
            //    }
            //}
            //
            // end of debug code
            //
            bool success = mConnection.ExecuteNonQuery("insert into Corpora (id, xml, title, provider, language, sourceUrl, source, timeStart, timeEnd) values (?, ?, ?, ?, ?, ?, ?, ?, ?)", 
                corpusId, 
                stringWriter.ToString(),
                Utils.Truncate(corpus.Features.GetFeatureValue("title"), 400),
                Utils.Truncate(corpus.Features.GetFeatureValue("_provider"), 400),
                Utils.Truncate(corpus.Features.GetFeatureValue("language"), 400),
                Utils.Truncate(corpus.Features.GetFeatureValue("_sourceUrl"), 400),
                corpus.Features.GetFeatureValue("_source"),
                Utils.Truncate(corpus.Features.GetFeatureValue("_timeStart"), 26),
                Utils.Truncate(corpus.Features.GetFeatureValue("_timeEnd"), 26));
            if (!success) { mLogger.Warn("ConsumeData", "Unable to write to database."); }
            foreach (Document document in corpus.Documents)
            {
                string documentId = Guid.NewGuid().ToString("N");
                success = mConnection.ExecuteNonQuery("insert into Documents (id, corpusId, name, description, text, category, link, time, pubDate, mimeType, contentType, charSet, contentLength) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    documentId,
                    corpusId,
                    Utils.Truncate(document.Name, 400),
                    Utils.Truncate(document.Features.GetFeatureValue("description"), 400),
                    document.Text,
                    Utils.Truncate(document.Features.GetFeatureValue("category"), 400),
                    Utils.Truncate(document.Features.GetFeatureValue("link"), 400),
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

        // *** IDisposable interface implementation ***

        public new void Dispose()
        {
            base.Dispose();
            try { mConnection.Disconnect(); } catch { }
        }
    }
}