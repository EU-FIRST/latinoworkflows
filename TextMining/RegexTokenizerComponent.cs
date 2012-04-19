/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RegexTokenizerComponent.cs
 *  Desc:    Regex-based tokenizer component
 *  Created: Dec-2010
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text.RegularExpressions;
using Latino.TextMining;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class RegexTokenizerComponent
       |
       '-----------------------------------------------------------------------
    */
    public class RegexTokenizerComponent : DocumentProcessor
    {
        private RegexTokenizer mTokenizer
            = new RegexTokenizer();

        public RegexTokenizerComponent() : base(typeof(RegexTokenizerComponent))
        {
            mBlockSelector = "Sentence";
        }

        public string TokenRegex
        {
            get { return mTokenizer.TokenRegex; }
            set { mTokenizer.TokenRegex = value; } // throws ArgumentNullException, ArgumentException
        }

        public bool IgnoreUnknownTokens
        {
            get { return mTokenizer.IgnoreUnknownTokens; }
            set { mTokenizer.IgnoreUnknownTokens = value; }
        }

        public RegexOptions TokenRegexOptions
        {
            get { return mTokenizer.TokenRegexOptions; }
            set { mTokenizer.TokenRegexOptions = value; }
        }

        public/*protected*/ override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Text") { return; }
            try
            {
                TextBlock[] textBlocks = document.GetAnnotatedBlocks(mBlockSelector);
                foreach (TextBlock textBlock in textBlocks)
                {
                    mTokenizer.Text = textBlock.Text;
                    for (RegexTokenizer.Enumerator e = (RegexTokenizer.Enumerator)mTokenizer.GetEnumerator(); e.MoveNext();)
                    {
                        document.AddAnnotation(new Annotation(textBlock.SpanStart + e.CurrentTokenIdx, textBlock.SpanStart + e.CurrentTokenIdx + e.Current.Length - 1, "Token"));
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