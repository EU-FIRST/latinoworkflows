/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    PosTaggerComponent.cs
 *  Desc:    English part-of-speech tagger component
 *  Created: Jul-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using OpenNLP.Tools.PosTagger;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class EnglishPosTaggerComponent
       |
       '-----------------------------------------------------------------------
    */
    // TODO: [X] reduce beam size, [ ] add lexicon
    public class EnglishPosTaggerComponent : DocumentProcessor
    {
        private EnglishMaximumEntropyPosTagger mPosTagger
            = new EnglishMaximumEntropyPosTagger(Utils.GetManifestResourceStream(typeof(EnglishTokenizerComponent), "EnglishPOS.nbin"), /*beamSize=*/1);

        private string mTokenGroupSelector 
            = "Sentence";
            
        public EnglishPosTaggerComponent() : base(typeof(EnglishPosTaggerComponent))
        {
            mBlockSelector = "Token";
        }

        public string TokenGroupSelector
        {
            get { return mTokenGroupSelector; }
            set { mTokenGroupSelector = value; }
        }

        private void ProcessTokens(TextBlock[] textBlocks)
        {
            ArrayList<string> tokens = new ArrayList<string>();
            foreach (TextBlock textBlock in textBlocks)
            {
                tokens.Add(textBlock.Text);
            }
            string[] posTags = mPosTagger.Tag(tokens.ToArray());
            int i = 0;
            foreach (TextBlock textBlock in textBlocks)
            {
                textBlock.Features.SetFeatureValue("posTag", posTags[i++]);
            }
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("_contentType");
            if (contentType != "Text") { return; }
            try
            {
                if (mTokenGroupSelector == null)
                {
                    TextBlock[] textBlocks = document.GetAnnotatedBlocks(mBlockSelector);
                    ProcessTokens(textBlocks);
                }
                else
                {
                    document.CreateAnnotationIndex();
                    TextBlock[] tokenGroups = document.GetAnnotatedBlocks(mTokenGroupSelector);
                    foreach (TextBlock tokenGroup in tokenGroups)
                    {
                        TextBlock[] textBlocks = document.GetAnnotatedBlocks(mBlockSelector, tokenGroup.SpanStart, tokenGroup.SpanEnd);
                        ProcessTokens(textBlocks);
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