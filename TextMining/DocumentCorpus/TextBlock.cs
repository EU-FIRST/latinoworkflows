/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    TextBlock.cs
 *  Desc:    Annotated text block data structure
 *  Created: Nov-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class TextBlock
       |
       '-----------------------------------------------------------------------
    */
    public class TextBlock
    {        
        private int mSpanStart;
        private int mSpanEnd;
        private string mType;
        private string mText;
        private Features mFeatures;

        internal TextBlock(int spanStart, int spanEnd, string type, string text, Dictionary<string, string> features)
        {
            mSpanStart = spanStart;
            mSpanEnd = spanEnd;
            mType = type;
            mText = text;
            mFeatures = new Features(features);
        }

        public int SpanStart
        {
            get { return mSpanStart; }
        }

        public int SpanEnd
        {
            get { return mSpanEnd; }
        }

        public string Type
        {
            get { return mType; }
        }

        public string Text
        {
            get { return mText; }
        }

        public Features Features
        {
            get { return mFeatures; }
        }

        public TextBlock[] GetAnnotatedBlocks(string regexStr, Document document)
        {
            Utils.ThrowException(regexStr == null ? new ArgumentNullException("regexStr") : null);
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            return GetAnnotatedBlocks(new Regex(regexStr, RegexOptions.Compiled), document);
        }

        public TextBlock[] GetAnnotatedBlocks(Regex regex, Document document)
        {            
            Utils.ThrowException(regex == null ? new ArgumentNullException("regex") : null);
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            ArrayList<TextBlock> blocks = new ArrayList<TextBlock>();
            foreach (Annotation annotation in document.Annotations)
            {
                if (regex.Match(annotation.Type).Success)
                {
                    if (annotation.SpanStart >= mSpanStart && annotation.SpanEnd <= mSpanEnd)
                    {
                        blocks.Add(annotation.GetAnnotatedBlock(document.Text));
                    }
                }
            }
            return blocks.ToArray();
        }
    }
}
