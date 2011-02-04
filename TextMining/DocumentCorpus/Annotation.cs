/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Annotation.cs
 *  Desc:    Document annotation data structure
 *  Created: Nov-2010
 *
 *  Authors: Jasmina Smailovic, Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Annotation
       |
       '-----------------------------------------------------------------------
    */
    public class Annotation : ICloneable<Annotation>
    {
        private int mId
            = -1;
        private string mType;
        private int mSpanStart;
        private int mSpanEnd;
        private Dictionary<string, string> mFeatures
            = new Dictionary<string, string>();
        private Features mFeaturesInterface;
        
        public Annotation(int spanStart, int spanEnd, string type)
        {
            Utils.ThrowException(spanStart < 0 ? new ArgumentOutOfRangeException("spanStart") : null);
            Utils.ThrowException(spanEnd < spanStart ? new ArgumentOutOfRangeException("SpanEnd") : null);
            Utils.ThrowException(type == null ? new ArgumentNullException("type") : null);
            Utils.ThrowException((type.Contains(",") || type.Contains("*")) ? new ArgumentValueException("type") : null);
            mSpanStart = spanStart;
            mSpanEnd = spanEnd;            
            mType = type.Trim().ToLower();
            mFeaturesInterface = new Features(mFeatures);
        }

        internal void SetId(int id)
        {
            mId = id;
        }

        public int Id
        {
            get { return mId; }
        }

        public string Type
        {
            get { return mType; }
        }

        public int SpanStart
        {
            get { return mSpanStart; }
        }

        public int SpanEnd
        {
            get { return mSpanEnd; }
        }

        public Features Features
        {
            get { return mFeaturesInterface; }
        }

        internal TextBlock GetAnnotatedBlock(Ref<string> text)
        {
            //Utils.ThrowException((text == null || text.Val == null) ? new ArgumentNullException("text") : null);
            //Utils.ThrowException(mSpanStart >= text.Val.Length ? new ArgumentOutOfRangeException("SpanStart") : null);
            //Utils.ThrowException(mSpanEnd >= text.Val.Length ? new ArgumentOutOfRangeException("SpanEnd") : null);
            TextBlock block = new TextBlock(mSpanStart, mSpanEnd, mType, text.Val.Substring(mSpanStart, mSpanEnd - mSpanStart + 1), mFeatures);
            return block;
        }

        // *** ICloneable<Annotation> interface implementation ***

        public Annotation Clone()
        {
            Annotation clone = new Annotation(mSpanStart, mSpanEnd, mType);
            clone.mId = mId;
            foreach (KeyValuePair<string, string> item in mFeatures)
            {
                clone.mFeatures.Add(item.Key, item.Value);
            }
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
