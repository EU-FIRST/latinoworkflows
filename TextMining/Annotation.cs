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

        public Annotation(int spanStart, int spanEnd, string type)
        {
            Utils.ThrowException(spanStart < 0 ? new ArgumentOutOfRangeException("spanStart") : null);
            Utils.ThrowException(SpanEnd < spanStart ? new ArgumentOutOfRangeException("SpanEnd") : null);
            Utils.ThrowException(type == null ? new ArgumentNullException("type") : null);
            mSpanStart = spanStart;
            mSpanEnd = spanEnd;
            mType = type;
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

        public Dictionary<string, string>.KeyCollection Features
        {
            get { return mFeatures.Keys; }
        }

        public void SetFeatureValue(string name, string val)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            if (mFeatures.ContainsKey(name))
            {
                mFeatures[name] = val;
            }
            else
            {
                mFeatures.Add(name, val);
            }
        }

        public string GetFeatureValue(string name)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            string value;
            if (mFeatures.TryGetValue(name, out value))
            {
                return value;
            }
            return null;
        }

        public bool RemoveFeature(string name)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            if (mFeatures.ContainsKey(name))
            {
                mFeatures.Remove(name);
                return true;
            }
            return false;
        }

        // *** ICloneable<Annotation> interface implementation ***

        public Annotation Clone()
        {
            Annotation clone = new Annotation(mSpanStart, mSpanEnd, mType);
            clone.SetId(mId);
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
