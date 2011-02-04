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

        public DocumentCorpusWriterComponent(string dbConnectionString)
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
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings); 
            corpus.WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            bool success = mConnection.ExecuteNonQuery("insert into Corpora (id, xml, title, provider, language, sourceUrl, source, timeStart, timeEnd) values (?, ?, ?, ?, ?, ?, ?, ?, ?)", 
                corpusId, 
                stringWriter.ToString(),
                Utils.Trunc(corpus.Features.GetFeatureValue("title"), 4000),
                Utils.Trunc(corpus.Features.GetFeatureValue("_provider"), 4000),
                Utils.Trunc(corpus.Features.GetFeatureValue("language"), 4000),
                Utils.Trunc(corpus.Features.GetFeatureValue("_sourceUrl"), 4000),
                corpus.Features.GetFeatureValue("_source"),
                Utils.Trunc(corpus.Features.GetFeatureValue("_timeStart"), 30),
                Utils.Trunc(corpus.Features.GetFeatureValue("_timeEnd"), 30));
            if (!success) { mLog.Warning("ConsumeData", "Unable to write to database."); }
            foreach (Document document in corpus.Documents)
            {
                string documentId = Guid.NewGuid().ToString("N");
                success = mConnection.ExecuteNonQuery("insert into Documents (id, corpusId, name, description, text, category, link, time, pubDate, raw) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    documentId, 
                    corpusId, 
                    Utils.Trunc(document.Name, 4000),
                    Utils.Trunc(document.Features.GetFeatureValue("description"), 4000),
                    document.Text,
                    Utils.Trunc(document.Features.GetFeatureValue("category"), 4000),
                    Utils.Trunc(document.Features.GetFeatureValue("link"), 4000),
                    Utils.Trunc(document.Features.GetFeatureValue("_time"), 30),
                    Utils.Trunc(document.Features.GetFeatureValue("pubDate"), 30),
                    document.Features.GetFeatureValue("raw"));
                if (!success) { mLog.Warning("ConsumeData", "Unable to write to database."); }
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