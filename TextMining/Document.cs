/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Document.cs
 *  Desc:    Annotated document data structure
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
using System.IO;
using System.Reflection;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Document
       |
       '-----------------------------------------------------------------------
    */
    [XmlSchemaProvider("ProvideSchema")]
    public class Document : ICloneable<Document>, System.Xml.Serialization.IXmlSerializable
    {
        private Ref<string> mText;
        private string mName;
        private ArrayList<Annotation> mAnnotations
            = new ArrayList<Annotation>();
        private static AnnotationComparer mAnnotationComparer
            = new AnnotationComparer();
        private Dictionary<string, string> mFeatures
            = new Dictionary<string, string>();
        private Features mFeaturesInterface;

        private class AnnotationComparer : IComparer<Annotation>
        {
            // *** IComparer<Annotation> interface implementation ***

            public int Compare(Annotation x, Annotation y)
            {
                return x.Id.CompareTo(y.Id);
            }
        }

        public Document(string name, string text)
        {
            Utils.ThrowException(text == null ? new ArgumentNullException("text") : null);
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            mName = name;
            mText = text;            
            mFeaturesInterface = new Features(mFeatures);
        }

        private Document() : this("", "") // required for serialization
        { 
        }

        public Features Features
        {
            get { return mFeaturesInterface; }
        }

        public string Name
        {
            get { return mName; }
        }

        public string Text
        {
            get { return mText; }
        }

        public int AnnotationCount
        {
            get { return mAnnotations.Count; }
        }

        public ArrayList<Annotation>.ReadOnly Annotations
        {
            get { return mAnnotations; }
        }

        public void AddAnnotation(Annotation annotation)
        {
            Utils.ThrowException(annotation == null ? new ArgumentNullException("annotation") : null);
            Utils.ThrowException(mAnnotations.Contains(annotation) ? new ArgumentValueException("annotation") : null);
            Utils.ThrowException((annotation.Id != -1 && mAnnotations.Count != 0 && mAnnotations.Last.Id >= annotation.Id) ?
                new ArgumentValueException("annotation") : null);
            if (annotation.Id == -1)
            {
                int id = -1;
                if (mAnnotations.Count != 0)
                {
                    id = mAnnotations.Last.Id;
                }
                annotation.SetId(id + 1);
            }
            mAnnotations.Add(annotation);
        }

        private int GetIdx(int id)
        {
            Annotation key = new Annotation(0, 0, "");
            key.SetId(id);
            return mAnnotations.BinarySearch(key, mAnnotationComparer);
        }

        public bool RemoveAnnotation(int id)
        {
            Utils.ThrowException(id < 0 ? new ArgumentOutOfRangeException("id") : null);
            int idx = GetIdx(id);
            if (idx > 0)
            {
                mAnnotations.RemoveAt(idx);
                return true;
            }
            return false;
        }

        public void RemoveAnnotationAt(int idx)
        {
            mAnnotations.RemoveAt(idx); // throws ArgumentOutOfRangeException
        }

        public Annotation GetAnnotation(int id)
        {
            Utils.ThrowException(id < 0 ? new ArgumentOutOfRangeException("id") : null);
            int idx = GetIdx(id);
            if (idx > 0)
            {
                return mAnnotations[idx];
            }
            return null;
        }

        public Annotation GetAnnotationAt(int idx)
        {
            return mAnnotations[idx]; // throws ArgumentOutOfRangeException
        }

        public TextBlock[] GetAnnotatedBlocks(string query) // TODO: more powerful query language for retrieving text blocks
        {
            Utils.ThrowException(query == null ? new ArgumentNullException("query") : null);
            Utils.ThrowException(query == "" ? new ArgumentValueException("query") : null);
            string[] tmp = query.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            ArrayList<string> annotTypes = new ArrayList<string>(tmp.Length);
            foreach (string annotType in tmp) { if (annotType.Trim() != "") { annotTypes.Add(annotType.Trim().ToLower()); } }
            Set<string> availTypes = new Set<string>();
            foreach (Annotation annot in mAnnotations) { availTypes.Add(annot.Type); }
            foreach (string annotType in annotTypes)
            {
                if (availTypes.Contains(annotType))
                {
                    return GetAnnotatedBlocksByType(annotType).ToArray();
                }
                else if (annotType == "*")
                {
                    return new TextBlock[] { new TextBlock(0, mText.Val.Length - 1, "*", mText.Val, /*features=*/new Dictionary<string, string>()) };
                }
            }
            return null;
        }

        private ArrayList<TextBlock> GetAnnotatedBlocksByType(string annotType) 
        {
            //Utils.ThrowException(annotType == null ? new ArgumentNullException("annotType") : null);
            //annotType = annotType.Trim().ToLower();
            //Utils.ThrowException(annotType == "" ? new ArgumentValueException("annotType") : null);
            ArrayList<TextBlock> blocks = new ArrayList<TextBlock>();
            foreach (Annotation annot in mAnnotations)
            {
                if (annot.Type == annotType)
                { 
                    // extract text block
                    blocks.Add(annot.GetAnnotatedBlock(mText));
                }
            }
            return blocks;
        }

        // *** ICloneable<Document> interface implementation

        public Document Clone()
        {
            Document clone = new Document(mName, "");
            clone.mText = mText; // *** text is not cloned, just referenced
            clone.mAnnotations = mAnnotations.DeepClone();
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
            return new XmlQualifiedName("Document", "http://freekoders.org/latino");
        }

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer, bool writeTopElement)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);
            string ns = "http://freekoders.org/latino";
            if (writeTopElement) { writer.WriteStartElement("Document", ns); }
            writer.WriteElementString("Text", ns, mText);
            writer.WriteElementString("Name", ns, mName);
            writer.WriteStartElement("Annotations", ns);
            foreach (Annotation annot in mAnnotations)
            {
                writer.WriteStartElement("Annotation", ns);
                writer.WriteElementString("Id", ns, annot.Id.ToString());
                writer.WriteElementString("SpanStart", ns, annot.SpanStart.ToString());
                writer.WriteElementString("SpanEnd", ns, annot.SpanEnd.ToString());
                writer.WriteElementString("Type", ns, annot.Type);
                writer.WriteStartElement("Features", ns);
                foreach (KeyValuePair<string, string> keyVal in annot.Features)
                {
                    writer.WriteStartElement("Feature", ns);
                    writer.WriteElementString("Name", ns, keyVal.Key);
                    writer.WriteElementString("Value", ns, keyVal.Value);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();      
            }
            writer.WriteEndElement();
            writer.WriteStartElement("Features", ns);
            foreach (KeyValuePair<string, string> keyVal in mFeatures)
            {
                writer.WriteStartElement("Feature", ns);
                writer.WriteElementString("Name", ns, keyVal.Key);
                writer.WriteElementString("Value", ns, keyVal.Value);
                writer.WriteEndElement();
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
