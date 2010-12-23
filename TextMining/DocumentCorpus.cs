/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentCorpus.cs
 *  Desc:    Annotated document corpus data structure
 *  Created: Nov-2010
 *
 *  Authors: Jasmina Smailovic, Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Reflection;
using System.IO;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentCorpus
       |
       '-----------------------------------------------------------------------
    */
    [XmlSchemaProvider("ProvideSchema")]
    public class DocumentCorpus : ICloneable<DocumentCorpus>, System.Xml.Serialization.IXmlSerializable
    {
        private ArrayList<Document> mDocuments
            = new ArrayList<Document>();
        private Dictionary<string, string> mFeatures
            = new Dictionary<string, string>();
        private Features mFeaturesInterface;

        public DocumentCorpus()
        {
            mFeaturesInterface = new Features(mFeatures);
        }

        public Features Features
        {
            get { return mFeaturesInterface; }
        }

        public void AddDocument(Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            Utils.ThrowException(mDocuments.Contains(document) ? new ArgumentValueException("document") : null);
            mDocuments.Add(document);
        }

        public void AddRange(IEnumerable<Document> documents)
        {
            Utils.ThrowException(documents == null ? new ArgumentNullException("documents") : null);
            foreach (Document document in documents)
            {
                AddDocument(document); // throws ArgumentNullException, ArgumentValueException
            }
        }

        public void Clear()
        {
            mDocuments.Clear();
        }

        public void Insert(int index, Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            Utils.ThrowException(mDocuments.Contains(document) ? new ArgumentValueException("document") : null);
            mDocuments.Insert(index, document); // throws ArgumentOutOfRangeException
        }

        public void InsertRange(int index, IEnumerable<Document> documents)
        {
            Utils.ThrowException(documents == null ? new ArgumentNullException("documents") : null);
#if THROW_EXCEPTIONS
            Set<Document> tmp = new Set<Document>();
            foreach (Document document in documents)
            {
                if (document == null || tmp.Contains(document)) { throw new ArgumentValueException("documents"); }
                tmp.Add(document);
            }
#endif
            mDocuments.InsertRange(index, documents); // throws ArgumentOutOfRangeException
        }

        public bool Remove(Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            return mDocuments.Remove(document);
        }

        public void RemoveAt(int index)
        {
            mDocuments.RemoveAt(index); // throws ArgumentOutOfRangeException
        }

        public void RemoveRange(int index, int count)
        {
            mDocuments.RemoveRange(index, count); // throws ArgumentOutOfRangeException, ArgumentException
        }

        public ArrayList<Document>.ReadOnly Documents
        {
            get { return mDocuments; }
        }

        // *** ICloneable<DocumentCorpus> interface implementation ***

        public DocumentCorpus Clone()
        {
            DocumentCorpus clone = new DocumentCorpus();
            clone.mDocuments = mDocuments.DeepClone();
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        // *** IXmlSerializable interface implementation ***

        public static XmlQualifiedName ProvideSchema(XmlSchemaSet schemaSet)
        {
            Utils.ThrowException(schemaSet == null ? new ArgumentNullException("schemaSet") : null);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = null;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("DocumentCorpusSchema.xsd"))
                {
                    stream = assembly.GetManifestResourceStream(name);
                    break;
                }
            }
            XmlSchema schema = XmlSchema.Read(stream, null);      
            schemaSet.Add(schema);
            stream.Close();
            return new XmlQualifiedName("DocumentCorpus", "http://freekoders.org/latino");
        }

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            Utils.ThrowException(reader == null ? new ArgumentNullException("reader") : null);
            mDocuments.Clear();
            mFeatures.Clear();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Feature" && !reader.IsEmptyElement)
                {
                    string featName = "not set";
                    string featVal = "";
                    while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Feature"))
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name")
                        {
                            featName = Utils.XmlReadValue(reader, "Name");
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Value")
                        {
                            featVal = Utils.XmlReadValue(reader, "Value");
                        }
                    }
                    Features.SetFeatureValue(featName, featVal);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Document" && !reader.IsEmptyElement)
                {
                    Document doc = new Document();
                    doc.ReadXml(reader);
                    AddDocument(doc);
                }
            }
        }

        public void WriteXml(XmlWriter writer, bool writeTopElement)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);
            string ns = "http://freekoders.org/latino";
            if (writeTopElement) { writer.WriteStartElement("DocumentCorpus", ns); }
            writer.WriteStartElement("Features", ns);
            foreach (KeyValuePair<string, string> keyVal in mFeatures)
            {
                writer.WriteStartElement("Feature", ns);
                writer.WriteElementString("Name", ns, keyVal.Key);
                writer.WriteElementString("Value", ns, keyVal.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("Documents", ns);
            foreach (Document doc in mDocuments)
            {
                doc.WriteXml(writer, /*writeTopElement=*/true);
            }
            writer.WriteEndElement();
            if (writeTopElement) { writer.WriteEndElement(); }
        }

        public void WriteXml(XmlWriter writer)
        {
            WriteXml(writer, /*writeTopElement=*/false); // throws ArgumentNullException
        }
    }
}