/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    EntityRecognitionComponent.cs
 *  Desc:    Ontology-based entity recognition component
 *  Created: Jun-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Semantics
{
    /* .-----------------------------------------------------------------------
       |
       |  Class EntityRecognitionComponent
       |
       '-----------------------------------------------------------------------
    */
    public class EntityRecognitionComponent : DocumentProcessor
    {
        private EntityRecognitionEngine mEntityRecognitionEngine
            = new EntityRecognitionEngine();

        public EntityRecognitionComponent(string rdfFolderName) : base(typeof(EntityRecognitionComponent))
        {
            mBlockSelector = "TextBlock";
            mEntityRecognitionEngine.ImportRdfFromFolder(rdfFolderName);
            mEntityRecognitionEngine.LoadGazetteers();
        }

        public EntityRecognitionComponent(IEnumerable<string> ontologyUrls) : base(typeof(EntityRecognitionComponent))
        {
            mBlockSelector = "TextBlock";
            foreach (string url in ontologyUrls)
            {
                mEntityRecognitionEngine.ImportRdfFromUrl(url);
            }
            mEntityRecognitionEngine.LoadGazetteers();
        }

        private static string GetAnnotationName(ArrayList<string> classPath)
        {
            string name = "";
            for (int i = classPath.Count - 1; i >= 0; i--)
            {
                name += "/" + new ArrayList<string>(classPath[i].Split('#', '/')).Last;
            }
            return name.TrimStart('/');
        }

        public override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Text") { return; }
            try
            {
                document.CreateAnnotationIndex();
                EntityRecognitionEngine.Document erDoc = new EntityRecognitionEngine.Document();
                foreach (TextBlock tb in document.GetAnnotatedBlocks(mBlockSelector))
                {
                    erDoc.BeginNewTextBlock();
                    foreach (TextBlock s in document.GetAnnotatedBlocks("Sentence", tb.SpanStart, tb.SpanEnd)) // *** sentence selector hardcoded
                    {
                        ArrayList<string> tokens = new ArrayList<string>();
                        ArrayList<string> posTags = new ArrayList<string>();
                        ArrayList<int> spanInfo = new ArrayList<int>();
                        foreach (TextBlock token in document.GetAnnotatedBlocks("Token", s.SpanStart, s.SpanEnd)) // *** token selector hardcoded
                        {
                            tokens.Add(token.Text);
                            posTags.Add(token.Annotation.Features.GetFeatureValue("posTag")); // *** POS tag feature name hardcoded
                            spanInfo.Add(token.SpanStart);
                        }
                        erDoc.AddSentence(tokens, spanInfo, posTags);
                    }
                }
                ArrayList<Pair<int, int>> spans;
                ArrayList<string> entities = erDoc.DiscoverEntities(mEntityRecognitionEngine, out spans);
                int i = 0;
                foreach (string gazetteerUri in entities)
                {
                    string instanceUri = mEntityRecognitionEngine.GetIdentifiedInstance(gazetteerUri);
                    if (instanceUri != null)
                    {
                        string annotationName = GetAnnotationName(mEntityRecognitionEngine.GetInstanceClassPath(instanceUri));
                        Annotation annotation = new Annotation(spans[i].First, spans[i].Second, annotationName);
                        document.AddAnnotation(annotation);
                        annotation.Features.SetFeatureValue("gazetteerUri", gazetteerUri);
                        annotation.Features.SetFeatureValue("instanceUri", instanceUri);
                        annotation.Features.SetFeatureValue("instanceClassUri", mEntityRecognitionEngine.GetInstanceClass(instanceUri));
                        // TODO: instanceLabel, instanceClassLabel
                    }
                    i++;
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}