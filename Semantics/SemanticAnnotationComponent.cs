/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    SemanticAnnotationComponent.cs
 *  Desc:    Semantic annotation component
 *  Created: Nov-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Semantics
{
    /* .-----------------------------------------------------------------------
       |
       |  Class SemanticAnnotationComponent
       |
       '-----------------------------------------------------------------------
    */
    public class SemanticAnnotationComponent : DocumentProcessor
    {
        private SemanticAnnotator mAnnotator
            = new SemanticAnnotator();

        public SemanticAnnotationComponent(TextReader ontologyReader) : base(typeof(SemanticAnnotationComponent))
        {
            mBlockSelector = "TextBlock";
            mAnnotator.LoadOntologyN3(ontologyReader);
            mAnnotator.ReadGazetteers();
        }

        public SemanticAnnotationComponent(IEnumerable<string> ontologyUrls) : base(typeof(SemanticAnnotationComponent))
        {
            mBlockSelector = "TextBlock";
            foreach (string url in ontologyUrls)
            {
                mAnnotator.LoadOntologyN3(url);
            }
            mAnnotator.ReadGazetteers();
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("_contentType");
            if (contentType != "Text") { return; }
            try
            {
                TextBlock[] textBlocks = document.GetAnnotatedBlocks(mBlockSelector);
                foreach (TextBlock textBlock in textBlocks)
                {
                    ArrayList<Annotation> annotations = mAnnotator.ExtractEntities(textBlock.Text, /*offset=*/textBlock.SpanStart);
                    document.AddAnnotations(annotations); 
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}