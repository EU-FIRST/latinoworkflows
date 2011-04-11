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
            string corpusId = Guid.NewGuid().ToString("N");
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings); 
            corpus.WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            bool success = mConnection.ExecuteNonQuery("insert into Corpora (id, xml, title, provider, language, sourceUrl, source, timeStart, timeEnd) values (?, ?, ?, ?, ?, ?, ?, ?, ?)", 
                corpusId, 
                stringWriter.ToString(),
                Utils.Trunc(corpus.Features.GetFeatureValue("title"), 400),
                Utils.Trunc(corpus.Features.GetFeatureValue("_provider"), 400),
                Utils.Trunc(corpus.Features.GetFeatureValue("language"), 400),
                Utils.Trunc(corpus.Features.GetFeatureValue("_sourceUrl"), 400),
                corpus.Features.GetFeatureValue("_source"),
                Utils.Trunc(corpus.Features.GetFeatureValue("_timeStart"), 26),
                Utils.Trunc(corpus.Features.GetFeatureValue("_timeEnd"), 26));
            if (!success) { mLogger.Warn("ConsumeData", "Unable to write to database."); }
            foreach (Document document in corpus.Documents)
            {
                string documentId = Guid.NewGuid().ToString("N");
                success = mConnection.ExecuteNonQuery("insert into Documents (id, corpusId, name, description, text, category, link, time, pubDate, mimeType, contentType, charSet, contentLength) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    documentId,
                    corpusId,
                    Utils.Trunc(document.Name, 400),
                    Utils.Trunc(document.Features.GetFeatureValue("description"), 400),
                    document.Text,
                    Utils.Trunc(document.Features.GetFeatureValue("category"), 400),
                    Utils.Trunc(document.Features.GetFeatureValue("link"), 400),
                    Utils.Trunc(document.Features.GetFeatureValue("_time"), 26),
                    Utils.Trunc(document.Features.GetFeatureValue("pubDate"), 26),
                    Utils.Trunc(document.Features.GetFeatureValue("_mimeType"), 80),
                    Utils.Trunc(document.Features.GetFeatureValue("_contentType"), 40),
                    Utils.Trunc(document.Features.GetFeatureValue("_charSet"), 40),
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