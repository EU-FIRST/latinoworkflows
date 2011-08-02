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
        private static object mLock
            = new object();

        public DocumentCorpusWriterComponent() : base(typeof(DocumentCorpusWriterComponent))
        {
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
            string path = string.Format(@"C:\Work\DacqPipe\Data\{0}\{1}\{2}\", now.Year, now.Month, now.Day);
            //string path = string.Format(@"E:\Users\miha\Work\DacqPipeBig_6\Data\{0}\{1}\{2}\", now.Year, now.Month, now.Day);
            string pathHtml = string.Format(@"C:\Work\DacqPipe\DataHtml\{0}\{1}\{2}\{3}\", now.Year, now.Month, now.Day, recordId);
            //string pathHtml = string.Format(@"E:\Users\miha\Work\DacqPipeBig_6\DataHtml\{0}\{1}\{2}\{3}\", now.Year, now.Month, now.Day, recordId);
            foreach (string p in new string[] { path, pathHtml })
            {
                if (!Directory.Exists(p))
                {
                    lock (mLock)
                    {
                        if (!Directory.Exists(p))
                        {
                            Directory.CreateDirectory(p);
                        }
                    }
                }
            }
            StreamWriter w = new StreamWriter(path + recordId + ".xml", /*append=*/false, Encoding.UTF8);
            w.Write(stringWriter.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
            w.Close();
            corpus.MakeHtmlPage(pathHtml, /*inlineCss=*/true);
        }
    }
}