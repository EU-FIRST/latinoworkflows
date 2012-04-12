/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    EnglishTokenizerComponent.cs
 *  Desc:    English tokenizer component
 *  Created: Jul-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using OpenNLP.Tools.Tokenize;
using OpenNLP.Tools.Util;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class EnglishTokenizerComponent
       |
       '-----------------------------------------------------------------------
    */
    public class EnglishTokenizerComponent : DocumentProcessor
    {
        private EnglishMaximumEntropyTokenizer mTokenizer
            = new EnglishMaximumEntropyTokenizer(Utils.GetManifestResourceStream(typeof(EnglishTokenizerComponent), "EnglishTok.nbin"));

        public EnglishTokenizerComponent() : base(typeof(EnglishTokenizerComponent))
        {
            mTokenizer.AlphaNumericOptimization = true;
            mBlockSelector = "Sentence";
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Text") { return; }
            try
            {
                TextBlock[] textBlocks = document.GetAnnotatedBlocks(mBlockSelector);
                foreach (TextBlock textBlock in textBlocks)
                {
                    Span[] positions = mTokenizer.TokenizePositions(textBlock.Text);
                    foreach (Span position in positions)
                    {
                        document.AddAnnotation(new Annotation(textBlock.SpanStart + position.Start, textBlock.SpanStart + (position.End - 1), "Token"));
                    }
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}