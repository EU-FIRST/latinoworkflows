/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    TextBlock.cs
 *  Desc:    Annotated text block data structure
 *  Created: Nov-2010
 *
 *  Author:  Miha Grcar
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
        private Annotation mAnnotation;

        internal TextBlock(int spanStart, int spanEnd, string type, string text, Annotation annotation)
        {
            mSpanStart = spanStart;
            mSpanEnd = spanEnd;
            mType = type;
            mText = text;
            mAnnotation = annotation;
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

        public Annotation Annotation
        {
            get { return mAnnotation; }
        }
    }
}
