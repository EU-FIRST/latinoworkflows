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
using System.Drawing;
using System.Text.RegularExpressions;
using System.Web;
using System.Text;

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
        private Dictionary<string, string> mFeatures
            = new Dictionary<string, string>();
        private Features mFeaturesInterface;
        
        private ArrayList<KeyDat<int, Annotation>> mAnnotationIndex
            = null;

        public Document(string name, string text)
        {
            Utils.ThrowException(text == null ? new ArgumentNullException("text") : null);
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            mName = name;
            mText = text;            
            mFeaturesInterface = new Features(mFeatures);
        }

        internal Document() : this("", "") // required for serialization
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
            set 
            {
                Utils.ThrowException(value == null ? new ArgumentNullException("Text") : null);
                //Utils.ThrowException(mAnnotations.Count > 0 ? new InvalidOperationException() : null);
                mText = value; 
            }
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
            mAnnotations.Add(annotation); 
        }

        public void AddAnnotations(IEnumerable<Annotation> annotationList)
        {
            Utils.ThrowException(annotationList == null ? new ArgumentNullException("annotationList") : null);
            foreach (Annotation annotation in annotationList)
            {
                AddAnnotation(annotation); // throws ArgumentNullException, ArgumentValueException
            }
        }

        public void RemoveAnnotationAt(int idx)
        {
            mAnnotations.RemoveAt(idx); // throws ArgumentOutOfRangeException
        }

        public void ClearAnnotations()
        {
            mAnnotations.Clear();
        }

        public Annotation GetAnnotationAt(int idx)
        {
            return mAnnotations[idx]; // throws ArgumentOutOfRangeException
        }

        public TextBlock[] GetAnnotatedBlocks(string selector) 
        {
            Utils.ThrowException(selector == null ? new ArgumentNullException("selector") : null);
            ArrayList<TextBlock> blocks = new ArrayList<TextBlock>();
            foreach (Annotation annotation in mAnnotations)
            {
                if (annotation.Type.StartsWith(selector))
                {
                    blocks.Add(annotation.GetAnnotatedBlock(mText));
                }
            }
            return blocks.ToArray();
        }

        public void CreateAnnotationIndex()
        {
            mAnnotationIndex = new ArrayList<KeyDat<int, Annotation>>(mAnnotations.Count);
            foreach (Annotation annotation in mAnnotations)
            {
                mAnnotationIndex.Add(new KeyDat<int, Annotation>(annotation.SpanStart, annotation));
            }
            mAnnotationIndex.Sort();
        }

        public TextBlock[] GetAnnotatedBlocks(string selector, int spanStart, int spanEnd)
        {
            // TODO: set mAnnotationIndex to null if annotation array changes
            Utils.ThrowException(mAnnotationIndex == null ? new InvalidOperationException() : null);
            Utils.ThrowException(selector == null ? new ArgumentNullException("selector") : null);
            Utils.ThrowException(spanStart < 0 ? new ArgumentOutOfRangeException("spanStart") : null);
            Utils.ThrowException(spanEnd < spanStart ? new ArgumentOutOfRangeException("SpanEnd") : null);
            KeyDat<int, Annotation> key = new KeyDat<int, Annotation>(spanStart, null);
            ArrayList<TextBlock> blocks = new ArrayList<TextBlock>();
            int idx = mAnnotationIndex.BinarySearch(key);
            if (idx < 0) { idx = ~idx; }
            else { while (idx >= 0 && mAnnotationIndex[idx].Key == key.Key) { idx--; } idx++; }
            for (int i = idx; i < mAnnotationIndex.Count; i++)
            {
                Annotation annotation = mAnnotationIndex[i].Dat;                
                if (annotation.SpanStart > spanEnd) { break; }
                if (annotation.SpanEnd <= spanEnd) 
                {
                    if (annotation.Type.StartsWith(selector))
                    {
                        blocks.Add(annotation.GetAnnotatedBlock(mText));
                    }
                }
            }
            return blocks.ToArray();
        }

        // *** ICloneable<Document> interface implementation

        public Document Clone()
        {
            Document clone = new Document(mName, "");
            clone.mText = mText; // *** text is not cloned, just referenced
            clone.mAnnotations = mAnnotations.DeepClone();
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
            return new XmlQualifiedName("Document", "http://freekoders.org/latino");
        }

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            Utils.ThrowException(reader == null ? new ArgumentNullException("reader") : null);
            mAnnotations.Clear();
            mFeatures.Clear();
            mText = mName = "";
            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Document"))
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name")
                {
                    mName = Utils.XmlReadValue(reader, "Name");
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Text")
                {
                    mText = Utils.XmlReadValue(reader, "Text");
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Annotation" && !reader.IsEmptyElement)
                {
                    int spanStart = 0;
                    int spanEnd = 0;
                    string annotType = "not set";
                    int annotId = -1;
                    ArrayList<Pair<string, string>> features = new ArrayList<Pair<string, string>>();
                    while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "Annotation"))
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "SpanStart")
                        {
                            spanStart = Convert.ToInt32(Utils.XmlReadValue(reader, "SpanStart")); 
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "SpanEnd")
                        {
                            spanEnd = Convert.ToInt32(Utils.XmlReadValue(reader, "SpanEnd")); 
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Type")
                        {
                            annotType = Utils.XmlReadValue(reader, "Type");
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Type")
                        {
                            annotType = Utils.XmlReadValue(reader, "Type");
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Feature" && !reader.IsEmptyElement)
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
                            features.Add(new Pair<string, string>(featName, featVal));
                        }
                    }
                    Annotation annot = new Annotation(spanStart, spanEnd, annotType);
                    AddAnnotation(annot);
                    foreach (Pair<string, string> feature in features)
                    {
                        annot.Features.SetFeatureValue(feature.First, feature.Second);
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Feature" && !reader.IsEmptyElement)
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
            }
        }

        public void WriteXml(XmlWriter writer, bool writeTopElement)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);
            string ns = "http://freekoders.org/latino";
            if (writeTopElement) { writer.WriteStartElement("Document", ns); }
            writer.WriteElementString("Name", ns, mName);
            writer.WriteElementString("Text", ns, mText);            
            writer.WriteStartElement("Annotations", ns);
            foreach (Annotation annot in mAnnotations)
            {
                writer.WriteStartElement("Annotation", ns);
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

        public void WriteGateXml(XmlWriter writer, bool writeTopElement)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);

            writer.WriteProcessingInstruction("xml", "version='1.0'");

            if (writeTopElement) { writer.WriteStartElement("GateDocument"); }

            //DOCUMENT FEATURES
            writer.WriteStartElement("GateDocumentFeatures");

            foreach (KeyValuePair<string, string> keyVal in mFeatures)
            {
                writer.WriteStartElement("Feature");

                writer.WriteStartElement("Name");
                writer.WriteAttributeString("className", "java.lang.String");
                writer.WriteString(keyVal.Key);
                writer.WriteEndElement(); //</Name>

                writer.WriteStartElement("Value");
                writer.WriteAttributeString("className", "java.lang.String");
                writer.WriteString(keyVal.Value);
                writer.WriteEndElement(); //</Value>

                writer.WriteEndElement(); //</Feature>
            }
            writer.WriteEndElement();//</GateDocumentFeatures>

            //TEXT WITH NODES
            writer.WriteStartElement("TextWithNodes");           

            List<int> spans = new List<int>();

            foreach (Annotation annot in mAnnotations)
            {
                if(!spans.Contains(annot.SpanStart))
                    spans.Add(annot.SpanStart);
                if (!spans.Contains(annot.SpanEnd + 1))
                    spans.Add(annot.SpanEnd + 1);
            }

            spans.Sort(); 

            string textWithNodes="";
            int k = 0;

            for (int j = 0; j < mText.Val.Length;)
            {
                if (spans.Count>0 && j == spans[k])
                {
                    textWithNodes +=  "<Node id=\"" + spans[k] + "\" />";
                    k++;

                }

                if (k < spans.Count)
                {
                    while (j != spans[k])
                    {
                        textWithNodes += HttpUtility.HtmlEncode(mText.Val[j].ToString());
                        j++;
                    }
                }
                else
                {
                    while (j < mText.Val.Length)
                    {
                        textWithNodes += HttpUtility.HtmlEncode(mText.Val[j].ToString());
                        j++;
                    }
                }
            }

            writer.WriteRaw(textWithNodes);

            writer.WriteEndElement();//</TextWithNodes>
            
            //ANNOTATIONS
            writer.WriteStartElement("AnnotationSet");
            int i = 1;
            foreach (Annotation annot in mAnnotations)
            {
                string annotType = annot.Type;
                if (annot.Type.StartsWith("Sentiment object/"))
                    annotType = "SO";

                writer.WriteStartElement("Annotation");
                writer.WriteAttributeString("Id", i.ToString());
                writer.WriteAttributeString("Type", annotType);
                writer.WriteAttributeString("StartNode", annot.SpanStart.ToString());
                writer.WriteAttributeString("EndNode", (annot.SpanEnd+1).ToString());

                if (annot.Type == "Token")
                {
                    string annotText;
                    annotText = (annot.GetAnnotatedBlock(mText)).Text;
                    annot.Features.SetFeatureValue("string", annotText);
                }
                foreach (KeyValuePair<string, string> keyVal in annot.Features)
                {
                    writer.WriteStartElement("Feature");

                    string replacement = keyVal.Key;
                    bool writeInstanceName = false;

                    if (annot.Type.StartsWith("Sentiment object/") && keyVal.Key == "objUri")
                    {
                        replacement = "Uri";
                        writeInstanceName = true;         
                    }

                    if (annot.Type == "Token" && keyVal.Key == "posTag")
                    {
                        replacement = "category";
                    }
                    else if(annot.Type == "Token" && (keyVal.Key == "word" || keyVal.Key == "punctuation"))
                    {
                        replacement = "kind";
                    }
                    else if (annot.Type == "Token" && keyVal.Key == "lemma")
                    {
                        replacement = "root";
                    }
                    
                    writer.WriteStartElement("Name");
                    writer.WriteAttributeString("className", "java.lang.String");
                    writer.WriteString(replacement);
                    writer.WriteEndElement(); //</Name>
                    

                    writer.WriteStartElement("Value");
                    writer.WriteAttributeString("className", "java.lang.String");
                    writer.WriteString(keyVal.Value);
                    writer.WriteEndElement();//</Value>

                    writer.WriteEndElement(); //</Feature>

                    if (writeInstanceName)
                    {
                        writer.WriteStartElement("Feature");

                        writer.WriteStartElement("Name");
                        writer.WriteAttributeString("className", "java.lang.String");
                        writer.WriteString("instanceName");
                        writer.WriteEndElement(); //</Name>

                        writer.WriteStartElement("Value");
                        writer.WriteAttributeString("className", "java.lang.String");
                        writer.WriteString(keyVal.Value.Split('#')[1]);
                        writer.WriteEndElement();//</Value>   
  
                        writer.WriteEndElement(); //</Feature>
                    }
                }

               

                writer.WriteEndElement(); //</Annotation>

                i++;
            }

           
            writer.WriteEndElement(); //</AnnotationSet>

            if (writeTopElement) { writer.WriteEndElement(); } //</GateDocument>
        }

        public void WriteXml(XmlWriter writer)
        {
            WriteXml(writer, /*writeTopElement=*/false); // throws ArgumentNullException
        }

        public void WriteGateXml(XmlWriter writer)
        {
            WriteGateXml(writer, /*writeTopElement=*/false); // throws ArgumentNullException
        }

        // *** Output HTML ***

        private static Set<string> dontTruncFeatures
            = new Set<string>(new string[] { "link", "_responseUrl", "pubDate", "_mimeType", "_contentType", 
                "_charSet", "_contentLength", "_guid", "_time", "detectedLanguage", "normalizedResponseUrl" });

        internal void MakeHtmlPage(TextWriter document, bool inlineCss, ArrayList<TreeNode<string>> annotationTreeList)
        {
            string templateString = Utils.GetManifestResourceString(GetType(), "Resources.DocumentTemplate.htm");

            string annotationTypeList = MakeHTMLAnnotationList(annotationTreeList);

            string documentFeatures = String.Empty;

            foreach (KeyValuePair<string, string> f in this.Features)
            {
                string val = f.Value;
                if (!dontTruncFeatures.Contains(f.Key))
                {
                    val = Utils.Truncate(f.Value, 100) + (f.Value.Length > 100 ? " ..." : "");
                }                
                documentFeatures += "<b>" + HttpUtility.HtmlEncode(f.Key) + "</b>" + " = " + HttpUtility.HtmlEncode(val) + " <br/><br/>";
            }

            templateString = templateString.Replace("{$document_title}", HttpUtility.HtmlEncode(mName));
            templateString = templateString.Replace("{$var_document_text}", MakeVarText(mText.Val, "text"));
            templateString = templateString.Replace("{$document_features}", documentFeatures);
            templateString = templateString.Replace("{$annotation_type_list}", annotationTypeList);
            templateString = templateString.Replace("{$annotation_type_list_name}", "annotationTypeList");
            templateString = templateString.Replace("{$annotation_name}", "annotation");
            templateString = templateString.Replace("{$inline_css}", inlineCss.ToString());

            document.Write(templateString);
            document.Close();

        }

        private string MakeHTMLAnnotationList(ArrayList<TreeNode<string>> annotationTreeList)
        {
            string annotationTypeList = "<ul>";

            List<Color> colors = new List<Color>();
            colors.Add(Color.White);
            colors.Add(Color.Black);

            foreach (TreeNode<string> tree in annotationTreeList)
            {

                string colorHtml;

                colorHtml = GetNewColor(colors);

                annotationTypeList += "<li> <TABLE ><TR><TD name='{$annotation_name}' style='padding-right:10px' ><input type='checkbox' name='{$annotation_type_list_name}' class='" + HttpUtility.HtmlEncode(tree.Root.Value) + "' elements='" + HttpUtility.HtmlEncode(tree.Root.Elements) + "' >" + HttpUtility.HtmlEncode(tree.Root.Value) + " <TD bgcolor='" + colorHtml + "' style='border:solid black 1px'>&nbsp &nbsp &nbsp</TD></TR></TABLE>";

                annotationTypeList += "<ul>";
                annotationTypeList = WriteHtmlList(tree, annotationTypeList, colors);
                annotationTypeList += "</ul>";

                annotationTypeList += "</li>";
            }

            annotationTypeList += "</ul>";

            annotationTypeList = annotationTypeList.Replace("{$document_title}", HttpUtility.HtmlEncode(mName));
            annotationTypeList = annotationTypeList.Replace("{$var_document_text}", MakeVarText(mText, "text"));      
            annotationTypeList = annotationTypeList.Replace("{$annotation_type_list}", annotationTypeList);
            annotationTypeList = annotationTypeList.Replace("{$annotation_type_list_name}", "annotationTypeList");
            annotationTypeList = annotationTypeList.Replace("{$annotation_name}", "annotation");
            
            return annotationTypeList;
        }

        internal ArrayList<TreeNode<string>> MakeAnnotationTree()
        {
            ArrayList<TreeNode<string>> result = new ArrayList<TreeNode<string>>();

            foreach (Annotation a in mAnnotations)
            {

                if (a.Type.Contains("/"))
                {
                    String[] annSplit = a.Type.Split('/');
                    Tree<string> rootNode = new Tree<string>(annSplit[0]);
                    
                    bool newNode = true;

                    for (int s = 0; s < result.Count; s++)
                    {
                        if (((Tree<string>)result[s]).Value == annSplit[0])
                        {
                            newNode = false;
                            AddChildren((Tree<string>)result[s], annSplit, a);
                        }
                    }

                    if (newNode)
                    {
                        TreeNode<string> node = rootNode;
                        for (int k = 1; k < annSplit.Length; k++)
                        {
                            node.Children.Add(annSplit[k]);

                            node.Children[node.Children.Count - 1].Elements += a.SpanStart + "," + a.SpanEnd + ",";

                            foreach (KeyValuePair<string, string> f in a.Features)
                            {
                                node.Children[node.Children.Count - 1].Elements += HttpUtility.HtmlEncode(f.Key + " = " + f.Value).Replace("'", "&#39;").Replace(":", "&#58;").Replace(",", "&#44;") + " <br/>";
                            }

                            node.Children[node.Children.Count - 1].Elements += ':';
                            node = node.Children[node.Children.Count - 1];
                        }

                        result.Add(rootNode);
                    }

                }
                else
                {
                    bool proceed = true;

                    for (int t = 0; t < result.Count; t++)
                    {
                        

                        if (((Tree<string>)result[t]).Value == a.Type)
                        {
                            proceed = false;
                            ((Tree<string>)result[t]).Elements += a.SpanStart + "," + a.SpanEnd + ",";
                          
                            foreach (KeyValuePair<string, string> f in a.Features)
                            {
                                ((Tree<string>)result[t]).Elements += HttpUtility.HtmlEncode(f.Key + " = " + f.Value).Replace("'", "&#39;").Replace(":", "&#58;").Replace(",", "&#44;") + " <br/>";
                            }

                            ((Tree<string>)result[t]).Elements += ":";
                        }
                    }

                    if (proceed)
                    {
                        Tree<string> rootNode = new Tree<string>(a.Type);
                        result.Add(rootNode);

                        rootNode.Root.Elements += a.SpanStart + "," + a.SpanEnd + ",";
                      
                        foreach (KeyValuePair<string, string> f in a.Features)
                        {
                            rootNode.Root.Elements += HttpUtility.HtmlEncode(f.Key + " = " + f.Value).Replace("'", "&#39;").Replace(":", "&#58;").Replace(",", "&#44;") + " <br/>";
                        }

                        rootNode.Root.Elements += ":";

                    }
                }

                
            }

            return result;
        }

        private string GetNewColor(List<Color> colors)
        {
            Color newColor = new Color();
            double maxDistance = 0;
            Random r = new Random();

            for (int t = 0; t < 100; t++)
            {
                double minDistance = 1000;

                //make up new random color
                int red = r.Next(255);
                int green = r.Next(255);
                int blue = r.Next(255);

                foreach (Color color in colors)
                {
                    double dbl_test_red = Math.Pow(Convert.ToDouble(((Color)color).R) - Convert.ToDouble(red), 2.0);
                    double dbl_test_green = Math.Pow(Convert.ToDouble(((Color)color).G) - Convert.ToDouble(green), 2.0);
                    double dbl_test_blue = Math.Pow(Convert.ToDouble(((Color)color).B) - Convert.ToDouble(blue), 2.0);

                    // compute the Euclidean distance between the two colors
                    double distance = Math.Sqrt(dbl_test_blue + dbl_test_green + dbl_test_red);

                    // keep minimum distance
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                if (minDistance > maxDistance)
                {
                    maxDistance = minDistance;
                    newColor = Color.FromArgb(red, green, blue);
                }


            }

            colors.Add(newColor);

            string colorHtml = "#" + String.Format("{0:X2}", newColor.R) + String.Format("{0:X2}", newColor.G) + String.Format("{0:X2}", newColor.B);
            return colorHtml;
        }

        private string WriteHtmlList(TreeNode<string> tree, string html, List<Color> colors)
        {

            for (int k = 0; k < tree.Children.Count; k++)
            {

                string colorHtml;

                colorHtml = GetNewColor(colors);

                html += "<li> <TABLE ><TR><TD name='{$annotation_name}' style='padding-right:10px' ><input type='checkbox' name='{$annotation_type_list_name}' class='" + HttpUtility.HtmlEncode(tree.Children[k].Value) + "' elements='" + HttpUtility.HtmlEncode(tree.Children[k].Elements) + "' >" + HttpUtility.HtmlEncode(tree.Children[k].Value) + " <TD bgcolor='" + colorHtml + " ' style='border:solid black 1px'>&nbsp &nbsp &nbsp</TD></TR></TABLE>";

                html += "<ul>";
                html = WriteHtmlList(tree.Children[k], html, colors);
                html += "</ul>";
                html += "</li>";
            }


            return html;
        }

        private void AddChildren(Tree<string> node, String[] children, Annotation a)
        {
            TreeNode<string> newNode = node;

            for (int k = 1; k < children.Length; k++)
            {
                if (newNode != null)
                {
                    newNode = AddChild(newNode, children[k]);

                    if (k == children.Length - 1)
                    {
                        newNode.Elements += a.SpanStart + "," + a.SpanEnd + ",";

                        foreach (KeyValuePair<string, string> f in a.Features)
                        {
                            newNode.Elements += HttpUtility.HtmlEncode(f.Key + " = " + f.Value).Replace("'", "&#39;").Replace(":", "&#58;").Replace(",", "&#44;") + " <br/>";
                        }

                        newNode.Elements += ":";

                    }

                }
            }
        }

        private TreeNode<string> AddChild(Tree<string> node, String child)
        {
            if (!node.HasChild(child))
            {
                return node.Children.Add(child);
            }
            else
                return node.GetChild(child);

        }

        private TreeNode<string> AddChild(TreeNode<string> node, string child)
        {
            if (!node.HasChild(child))
            {
                return node.Children.Add(child);
            }
            else
                return node.GetChild(child);

        }

        public static string MakeVarText(string text, string varName)
        {
            StringBuilder str = new StringBuilder("var " + varName + " = \"");
            foreach (char ch in text)
            {
                if (ch >= 32 && ch <= 126 && ch != '"' && ch != '\\') { str.Append(ch); } // "readable" range
                else { str.Append("\\u" + ((int)ch).ToString("X").PadLeft(4, '0')); } // encoded as Unicode
            }
            return str.ToString() + "\"";
        }
    }
}
