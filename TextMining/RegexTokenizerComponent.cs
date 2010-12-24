/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RegexTokenizerComponent.cs
 *  Desc:    Regex-based tokenizer component
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
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

        private const string DEST_ANNOT_TYPE 
            = "token";
        private const string SRC_ANNOT_TYPE
            = "content_block,*";

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

        protected override void ProcessDocument(Document document)
        {
            TextBlock[] textBlocks = document.GetAnnotatedBlocks(SRC_ANNOT_TYPE);
            foreach (TextBlock textBlock in textBlocks)
            {                
                // do tokenization, add annotations to document
                mTokenizer.Text = textBlock.Text;
                for (RegexTokenizer.Enumerator e = (RegexTokenizer.Enumerator)mTokenizer.GetEnumerator(); e.MoveNext(); )
                {
                    //Console.WriteLine("{0} {1} {2}", textBlock.SpanStart + e.CurrentTokenIdx, textBlock.SpanStart + e.CurrentTokenIdx + e.Current.Length - 1, e.Current);
                    Annotation annot = new Annotation(textBlock.SpanStart + e.CurrentTokenIdx, textBlock.SpanStart + e.CurrentTokenIdx + e.Current.Length - 1, DEST_ANNOT_TYPE);
                    document.AddAnnotation(annot);
                }
            }
        }
    }
}