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
using System.Web;
using System.Text;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentCorpus
       |
       '-----------------------------------------------------------------------
    */
    [XmlSchemaProvider("ProvideSchema")]
    public class DocumentCorpus : ICloneable<DocumentCorpus>, System.Xml.Serialization.IXmlSerializable, ISerializable
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
        
        public DocumentCorpus(BinarySerializer reader) {
            Load(reader);
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

        public void CopyFeaturesFrom(DocumentCorpus corpus)
        {
            foreach (KeyValuePair<string, string> item in corpus.mFeatures)
            {
                Features.SetFeatureValue(item.Key, item.Value);
            }        
        }

        // *** ICloneable<DocumentCorpus> interface implementation ***

        public DocumentCorpus Clone()
        {
            DocumentCorpus clone = new DocumentCorpus();
            clone.mDocuments = mDocuments.DeepClone();
            foreach (KeyValuePair<string, string> item in mFeatures)
            {
                clone.mFeatures.Add(item.Key, item.Value);
            }
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

        public void ReadXml(string xml)
        {
            Utils.ThrowException(xml == null ? new ArgumentNullException("xml") : null);
            XmlTextReader xmlReader = new XmlTextReader(new StringReader(xml));
            ReadXml(xmlReader);
            xmlReader.Close();
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
                writer.WriteElementString("Name", ns, Utils.ReplaceSurrogates(keyVal.Key));
                writer.WriteElementString("Value", ns, Utils.ReplaceSurrogates(keyVal.Value));
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

        public void WriteXml(string fileName, bool writeTopElement)
        {
            Utils.ThrowException(!Utils.VerifyFileNameCreate(fileName) ? new ArgumentValueException("fileName") : null);
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            StreamWriter fileWriter = new StreamWriter(fileName, /*append=*/false, Encoding.UTF8);
            XmlWriter xmlWriter = XmlWriter.Create(fileWriter, xmlSettings);
            WriteXml(xmlWriter, writeTopElement);
            xmlWriter.Close();
            fileWriter.Close(); 
        }

        // *** output HTML ***

        public void MakeHtmlPage(string path, bool inlineCss)
        {
            string indexString = Utils.GetManifestResourceString(GetType(), "Resources.IndexTemplate.htm");

            TextWriter index = new StreamWriter(string.Format("{0}\\Index.html", path));

            TextWriter document;
            string documentList = String.Empty;

            int i = 1;

            foreach (Document d in Documents)
            {
                ArrayList<TreeNode<string>> annotationTreeList;
                annotationTreeList = d.MakeAnnotationTree();

                document = new StreamWriter(string.Format("{0}\\Document{1}.html", path, i));
                documentList += "<p class='documentTitle'><a href=Document" + i + ".html>" + HttpUtility.HtmlEncode(d.Name) + "</a></br>";
                documentList += "<p class='documentText'>" + HttpUtility.HtmlEncode(Utils.Truncate(d.Text, 400)) + "...</p>";

                string annotationsString = d.Annotations.Count == 1 ? " annotation" : " annotations";
                string featuresString = d.Features.Names.Count == 1 ? " feature" : " features";
                int countBasicTypes = CountBasicTypes(annotationTreeList);
                string basicTypesString = countBasicTypes == 1 ? " basic type" : " basic types";

                documentList += "<p class='statistics'>Contains " + d.Annotations.Count + annotationsString + " of " + countBasicTypes + basicTypesString + ". Described with " + d.Features.Names.Count + featuresString + ".</p>";

                d.MakeHtmlPage(document, inlineCss);                              
                i++;               
            }

            string corpusFeatures = String.Empty;

            foreach (KeyValuePair<string, string> f in this.Features)
            {
                corpusFeatures += "<b>" + HttpUtility.HtmlEncode(f.Key) + "</b>" + " = " + HttpUtility.HtmlEncode(Utils.Truncate(f.Value, 100) + (f.Value.Length > 100 ? " ..." : "")) + " <br/><br/>";
            }

            indexString = indexString.Replace("{$document_list}", documentList);
            indexString = indexString.Replace("{$corpus_features}", corpusFeatures);
            indexString = indexString.Replace("{$inline_css}", inlineCss.ToString());

            index.Write(indexString);
            index.Close();
        }

        public int CountBasicTypes(ArrayList<TreeNode<string>> annotationTreeList)
        {
            int count = 0;

            foreach (TreeNode<string> tree in annotationTreeList)
            {
                count += tree.CountTreeLeaves();
            }

            return count;

        }

        public void Save(BinarySerializer writer) {
            mDocuments.Save(writer);
            Utils.SaveDictionary(mFeatures, writer);
        }
        
        public void Load(BinarySerializer reader) {
            mDocuments = new ArrayList<Document>(reader);
            mFeatures = Utils.LoadDictionary<string, string>(reader);
            mFeaturesInterface = new Features(mFeatures);
        }
    }
}