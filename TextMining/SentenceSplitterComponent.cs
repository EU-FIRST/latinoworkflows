/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    SentenceSplitterComponent.cs
 *  Desc:    English sentence splitter component
 *  Created: Jul-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text.RegularExpressions;
using OpenNLP.Tools.SentenceDetect;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class SentenceSplitterComponent
       |
       '-----------------------------------------------------------------------
    */
    public class SentenceSplitterComponent : DocumentProcessor
    {
        private EnglishMaximumEntropySentenceDetector mSentenceDetector
            = new EnglishMaximumEntropySentenceDetector(Utils.GetManifestResourceStream(typeof(SentenceSplitterComponent), "EnglishSD.nbin"));

        public SentenceSplitterComponent() : base(typeof(SentenceSplitterComponent))
        {
            mBlockSelector = "TextBlock";
        }

        private void GetTrimOffsets(string str, out int startOffset, out int endOffset)
        {
            startOffset = endOffset = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsSeparator(str[i])) { startOffset++; }
                else { break; }
            }
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (Char.IsSeparator(str[i])) { endOffset++; }
                else { break; }                
            }
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Text") { return; }
            try
            {
                TextBlock[] blocks = document.GetAnnotatedBlocks(mBlockSelector);
                foreach (TextBlock block in blocks)
                {
                    OpenNLP.Tools.Util.Pair<int, int>[] positions;
                    string[] sentences = mSentenceDetector.SentenceDetect(block.Text, out positions);
                    int i = 0;
                    foreach (OpenNLP.Tools.Util.Pair<int, int> pos in positions)
                    {
                        int startTrimOffset, endTrimOffset;
                        GetTrimOffsets(sentences[i], out startTrimOffset, out endTrimOffset);
                        int startIdx = block.SpanStart + pos.FirstValue + startTrimOffset;
                        int endIdx = block.SpanStart + pos.FirstValue + (pos.SecondValue - 1) - endTrimOffset;
                        if (endIdx >= startIdx)
                        {
                            document.AddAnnotation(new Annotation(startIdx, endIdx, "Sentence"));
                        }
                        i++;
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
