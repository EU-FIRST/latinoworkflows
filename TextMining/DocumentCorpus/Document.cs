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
using System.Web;

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

        /* .-----------------------------------------------------------------------
           |
           |  Class AnnotationComparer
           |
           '-----------------------------------------------------------------------
        */
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
                Utils.ThrowException(mAnnotations.Count > 0 ? new InvalidOperationException() : null);
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
                    return new TextBlock[] { new TextBlock(0, mText.Val.Length - 1, "*", mText.Val, mFeatures) };
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
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Id")
                        {
                            annotId = Convert.ToInt32(Utils.XmlReadValue(reader, "Id")); 
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "SpanStart")
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
                    annot.SetId(annotId);
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

        // *** Output HTML ***

        public void MakeHtmlPage(TextWriter document, bool inlineCss, ArrayList<TreeNode<string>> annotationTreeList)
        {
            string templateString = Utils.GetManifestResourceString(GetType(), "Resources.DocumentTemplate.htm");

            string annotationTypeList = MakeHTMLAnnotationList(annotationTreeList);

            string documentFeatures = String.Empty;

            foreach (KeyValuePair<string, string> f in this.Features)
            {
                documentFeatures += "<b>" + f.Key + "</b>" + " = " + Utils.Truncate(HttpUtility.HtmlEncode(f.Value), 100) + (f.Value.Length > 100?" ...":"") + " <br/><br/>";
            }

            templateString = templateString.Replace("{$document_title}", mName);
            templateString = templateString.Replace("{$document_text}", mText);
            templateString = templateString.Replace("{$document_features}", documentFeatures);
            templateString = templateString.Replace("{$annotation_type_list}", annotationTypeList);
            templateString = templateString.Replace("{$annotation_type_list_name}", "annotationTypeList");
            templateString = templateString.Replace("{$annotation_name}", "annotation");
            templateString = templateString.Replace("{$inline_css}", inlineCss.ToString());

            document.Write(templateString);
            document.Close();

        }

        public string MakeHTMLAnnotationList(ArrayList<TreeNode<string>> annotationTreeList)
        {
            string annotationTypeList = "<ul>";

            List<Color> colors = new List<Color>();
            colors.Add(Color.White);
            colors.Add(Color.Black);

            foreach (TreeNode<string> tree in annotationTreeList)
            {

                string colorHtml;

                if (tree.Root.Value == "positive")
                {
                    colorHtml = "#" + String.Format("{0:X2}", Color.GreenYellow.R) + String.Format("{0:X2}", Color.GreenYellow.G) + String.Format("{0:X2}", Color.GreenYellow.B);
                    colors.Add(Color.GreenYellow);
                }
                else if (tree.Root.Value == "negative")
                {
                    colorHtml = "#" + String.Format("{0:X2}", Color.Tomato.R) + String.Format("{0:X2}", Color.Tomato.G) + String.Format("{0:X2}", Color.Tomato.B);
                    colors.Add(Color.Tomato);
                }
                else
                    colorHtml = GetNewColor(colors);

                annotationTypeList += "<li> <TABLE ><TR><TD name='{$annotation_name}' style='padding-right:10px' ><input type='checkbox' name='{$annotation_type_list_name}' class='" + tree.Root.Value + "'elements='" + tree.Root.Elements + "' >" + tree.Root.Value + " <TD bgcolor='" + colorHtml + "' style='border:solid black 1px'>&nbsp &nbsp &nbsp</TD></TR></TABLE>";

                annotationTypeList += "<ul>";
                annotationTypeList = WriteHtmlList(tree, annotationTypeList, colors);
                annotationTypeList += "</ul>";

                annotationTypeList += "</li>";
            }

            annotationTypeList += "</ul>";

            annotationTypeList = annotationTypeList.Replace("{$document_title}", mName);
            annotationTypeList = annotationTypeList.Replace("{$document_text}", mText);           
            annotationTypeList = annotationTypeList.Replace("{$annotation_type_list}", annotationTypeList);
            annotationTypeList = annotationTypeList.Replace("{$annotation_type_list_name}", "annotationTypeList");
            annotationTypeList = annotationTypeList.Replace("{$annotation_name}", "annotation");
            
            return annotationTypeList;
        }

        public ArrayList<TreeNode<string>> MakeAnnotationTree()
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
                        for (int k = 1; k < annSplit.Length; k++)
                        {
                            rootNode.Children.Add(annSplit[k]);

                            rootNode.Children[rootNode.Children.Count - 1].Elements += a.SpanStart + "," + a.SpanEnd + ",";
                            
                            foreach (KeyValuePair<string, string> f in a.Features)
                            {                              
                                rootNode.Children[rootNode.Children.Count - 1].Elements += f.Key + " = " + f.Value + " <br/>";
                            }
                        }

                        rootNode.Children[rootNode.Children.Count - 1].Elements += ';';

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
                                ((Tree<string>)result[t]).Elements += f.Key + " = " + f.Value + " <br/>";
                            }

                            ((Tree<string>)result[t]).Elements += ";";
                        }
                    }

                    if (proceed)
                    {
                        Tree<string> rootNode = new Tree<string>(a.Type);
                        result.Add(rootNode);

                        rootNode.Root.Elements += a.SpanStart + "," + a.SpanEnd + ",";
                      
                        foreach (KeyValuePair<string, string> f in a.Features)
                        {                           
                            rootNode.Root.Elements += f.Key + " = " + f.Value + " <br/>";
                        }

                        rootNode.Root.Elements += ";";

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

                if (tree.Children[k].Value == "positive")
                {
                    colorHtml = "#" + String.Format("{0:X2}", Color.GreenYellow.R) + String.Format("{0:X2}", Color.GreenYellow.G) + String.Format("{0:X2}", Color.GreenYellow.B);
                    colors.Add(Color.GreenYellow);
                }
                else if (tree.Children[k].Value == "negative")
                {
                    colorHtml = "#" + String.Format("{0:X2}", Color.Tomato.R) + String.Format("{0:X2}", Color.Tomato.G) + String.Format("{0:X2}", Color.Tomato.B);
                    colors.Add(Color.Tomato);
                }
                else
                    colorHtml = GetNewColor(colors);

                html += "<li> <TABLE ><TR><TD name='{$annotation_name}' style='padding-right:10px' ><input type='checkbox' name='{$annotation_type_list_name}' class='" + tree.Children[k].Value + "'elements='" + tree.Children[k].Elements + "' >" + tree.Children[k].Value + " <TD bgcolor='" + colorHtml + " ' style='border:solid black 1px'>&nbsp &nbsp &nbsp</TD></TR></TABLE>";

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
                            newNode.Elements += f.Key + " = " + f.Value + " <br/>";
                        }

                        newNode.Elements += ";";

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
    }
}
