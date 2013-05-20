/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentWriterComponent.cs
 *  Desc:    Document writer component
 *  Created: May-2013
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Security.Cryptography;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.IO.Compression;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Persistance
{
    public class DocumentWriterComponent : StreamDataConsumer
    {
        private string mConnectionString;
        private string mXmlDataRoot;
        private string mHtmlDataRoot;

        private object mLock
            = new object();

        public DocumentWriterComponent(string connectionString, string xmlDataRoot, string htmlDataRoot) : base(typeof(DocumentWriterComponent))
        {
            mConnectionString = connectionString;
            mXmlDataRoot = xmlDataRoot == null ? null : xmlDataRoot.TrimEnd('\\');
            mHtmlDataRoot = htmlDataRoot == null ? null : htmlDataRoot.TrimEnd('\\');       
        }

        private static DataTable CreateTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("id", typeof(Guid));
            t.Columns.Add("name", typeof(string));
            t.Columns.Add("description", typeof(string));
            t.Columns.Add("category", typeof(string));
            t.Columns.Add("link", typeof(string));
            t.Columns.Add("responseUrl", typeof(string));
            t.Columns.Add("urlkey", typeof(string));
            t.Columns.Add("acqTime", typeof(DateTime));
            t.Columns.Add("pubTimeStr", typeof(string));
            t.Columns.Add("mimeType", typeof(string));
            t.Columns.Add("charSet", typeof(string));
            t.Columns.Add("contentLength", typeof(int));
            t.Columns.Add("domain", typeof(string));
            t.Columns.Add("bpCharCount", typeof(int));
            t.Columns.Add("contentCharCount", typeof(int));
            t.Columns.Add("unseenContentCharCount", typeof(int));
            t.Columns.Add("rev", typeof(int));
            t.Columns.Add("fileName", typeof(string));
            t.Columns.Add("oldIdCorpus", typeof(Guid));
            t.Columns.Add("oldIdDocument", typeof(Guid));
            return t;
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            DocumentCorpus c = (DocumentCorpus)data;
            DataTable dt = CreateTable();
            foreach (Document doc in c.Documents)
            {
                Document d = doc.Clone();
                string rawHtml = d.Features.GetFeatureValue("raw");
                DateTime time = DateTime.Parse(d.Features.GetFeatureValue("time"));
                Guid cGuid = new Guid(c.Features.GetFeatureValue("guid"));
                Guid dGuid = new Guid(d.Features.GetFeatureValue("guid"));
                ArrayList<byte> buffer = new ArrayList<byte>();
                buffer.AddRange(cGuid.ToByteArray());
                buffer.AddRange(dGuid.ToByteArray());
                Guid docId = new Guid(MD5.Create().ComputeHash(buffer.ToArray()));
                d.Features.RemoveFeature("raw");
                DateTime timeEnd = DateTime.Parse(c.Features.GetFeatureValue("timeEnd"));
                d.Features.SetFeatureValue("oldId", string.Format("{0:HH}_{0:mm}_{0:ss}_{1:N}_{2:N}", timeEnd, cGuid, dGuid));
                d.Features.SetFeatureValue("guid", docId.ToString("N"));
                d.Features.SetFeatureValue("rssUrl", c.Features.GetFeatureValue("sourceUrl"));
                d.Features.SetFeatureValue("siteId", c.Features.GetFeatureValue("siteId"));
                // remove boilerplate removal features
                foreach (Annotation annot in d.Annotations)
                {
                    if (annot.Type.StartsWith("TextBlock")) { annot.Features.Clear(); }
                }
                // write doc XML
                if (mXmlDataRoot != null)
                {
                    string outFileName = string.Format("{0}\\{1:yyyy}\\{1:MM}\\{1:dd}\\{1:HH}_{1:mm}_{1:ss}_{2:N}.xml.gz", mXmlDataRoot, time, docId);
                    string path = new FileInfo(outFileName).DirectoryName;
                    if (!Directory.Exists(path))
                    {
                        lock (mLock) { if (!Directory.Exists(path)) { Directory.CreateDirectory(path); } }
                    }
                    d.WriteXmlCompressed(outFileName);
                }
                // write raw HTML
                if (mHtmlDataRoot != null)
                {
                    string outFileName = string.Format("{0}\\{1:yyyy}\\{1:MM}\\{1:dd}\\{1:HH}_{1:mm}_{1:ss}_{2:N}.html.gz", mHtmlDataRoot, time, docId);
                    string path = new FileInfo(outFileName).DirectoryName;
                    if (!Directory.Exists(path))
                    {
                        lock (mLock) { if (!Directory.Exists(path)) { Directory.CreateDirectory(path); } }
                    }
                    using (FileStream stream = new FileStream(outFileName, FileMode.Create))
                    {
                        using (GZipStream gzStream = new GZipStream(stream, CompressionMode.Compress))
                        {
                            using (BinaryWriter w = new BinaryWriter(gzStream))
                            {
                                w.Write(Convert.FromBase64String(rawHtml));
                            }
                        }
                    }
                }
                // prepare for bulk write
                if (mConnectionString != null) 
                {
                    string fileName = string.Format("{0:yyyy}\\{0:MM}\\{0:dd}\\{0:HH}_{0:mm}_{0:ss}_{1:N}.html.gz", time, docId);
                    dt.Rows.Add(
                        new Guid(d.Features.GetFeatureValue("guid")),
                        Utils.Truncate(d.Name, 400),
                        Utils.Truncate(d.Features.GetFeatureValue("description"), 400),
                        Utils.Truncate(d.Features.GetFeatureValue("category"), 400),
                        Utils.Truncate(d.Features.GetFeatureValue("link"), 400),
                        Utils.Truncate(d.Features.GetFeatureValue("responseUrl"), 400),
                        Utils.Truncate(d.Features.GetFeatureValue("urlKey"), 400),
                        DateTime.Parse(d.Features.GetFeatureValue("time")),
                        Utils.Truncate(d.Features.GetFeatureValue("pubDate"), 100),
                        Utils.Truncate(d.Features.GetFeatureValue("mimeType"), 80),
                        Utils.Truncate(d.Features.GetFeatureValue("charSet"), 40),
                        Convert.ToInt32(d.Features.GetFeatureValue("contentLength")),
                        Utils.Truncate(d.Features.GetFeatureValue("domainName"), 100),
                        Convert.ToInt32(d.Features.GetFeatureValue("bprBoilerplateCharCount")),
                        Convert.ToInt32(d.Features.GetFeatureValue("bprContentCharCount")),
                        Convert.ToInt32(d.Features.GetFeatureValue("unseenContentCharCount")),
                        Convert.ToInt32(d.Features.GetFeatureValue("rev")),
                        Utils.Truncate(fileName, 100),
                        cGuid,
                        dGuid 
                        );
                }
            }
            // bulk write to database
            if (mConnectionString != null && dt.Rows.Count > 0)
            {
                using (SqlConnection connection = new SqlConnection(mConnectionString))
                {
                    connection.Open();
                    using (SqlBulkCopy bulkWriter = new SqlBulkCopy(connection))
                    {
                        bulkWriter.BulkCopyTimeout = 0; // *** no timeout
                        bulkWriter.DestinationTableName = "Documents";
                        bulkWriter.WriteToServer(dt);
                    }
                }
            }
        }
    }
}
