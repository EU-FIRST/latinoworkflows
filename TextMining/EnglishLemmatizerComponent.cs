/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    EnglishLemmatizerComponent.cs
 *  Desc:    English lemmatizer component
 *  Created: Feb-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using Latino.TextMining;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class EnglishLemmatizerComponent
       |
       '-----------------------------------------------------------------------
    */
    public class EnglishLemmatizerComponent : DocumentProcessor
    {
        public enum Type
        {
            RdrLemmatizer,
            PorterStemmer,
            Both
        }

        private IStemmer mStemmer;
        private IStemmer mLemmatizer;
        private Type mType;

        public EnglishLemmatizerComponent(Type type) : base(typeof(EnglishLemmatizerComponent))
        {
            mBlockSelector = "Token";
            mStemmer = new PorterStemmer();
            mLemmatizer = new Lemmatizer(Language.English);
            mType = type;
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
                    if (mType == Type.PorterStemmer || mType == Type.Both)
                    {
                        textBlock.Annotation.Features.SetFeatureValue("stem", mStemmer.GetStem(textBlock.Text));
                    }
                    if (mType == Type.RdrLemmatizer || mType == Type.Both)
                    {
                        textBlock.Annotation.Features.SetFeatureValue("lemma", mLemmatizer.GetStem(textBlock.Text));
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
