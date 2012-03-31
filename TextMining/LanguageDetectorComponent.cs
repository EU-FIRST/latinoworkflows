/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    LanguageDetectorComponent.cs
 *  Desc:    Language detector component 
 *  Created: Oct-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text;
using Latino.TextMining;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class LanguageDetectorComponent
       |
       '-----------------------------------------------------------------------
    */
    public class LanguageDetectorComponent : DocumentProcessor
    {
        private static LanguageDetector mLanguageDetector
            = LanguageDetector.GetLanguageDetectorPrebuilt();
        private int mMinTextLen // TODO: make this configurable
            = 100;

        public LanguageDetectorComponent() : base(typeof(LanguageDetectorComponent))
        {
            mBlockSelector = "TextBlock";
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("_contentType");
            if (contentType != "Text") { return; }
            StringBuilder strBulder = new StringBuilder();
            try
            {
                TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                foreach (TextBlock block in blocks)
                {
                    strBulder.AppendLine(block.Text);
                }
                if (strBulder.Length >= mMinTextLen) 
                {
                    LanguageProfile langProfile = mLanguageDetector.FindMatchingLanguage(strBulder.ToString());
                    document.Features.SetFeatureValue("detectedLanguage", langProfile.Language.ToString());
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}