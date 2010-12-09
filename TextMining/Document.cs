/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Document.cs
 *  Desc:    Annotated document data structure
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
       |  Class Document
       |
       '-----------------------------------------------------------------------
    */
    public class Document //ICloneable<Document>
    {
        private string mText
            = "";
        private ArrayList<Annotation> mAnnotations
            = new ArrayList<Annotation>();
        private static AnnotationComparer mAnnotationComparer
            = new AnnotationComparer();

        private class AnnotationComparer : IComparer<Annotation>
        {
            // *** IComparer<Annotation> interface implementation ***

            public int Compare(Annotation x, Annotation y)
            {
                return x.Id.CompareTo(y.Id);
            }
        }

        public Document(string text)
        {
            Utils.ThrowException(text == null ? new ArgumentNullException("text") : null);
            mText = text;
        }

        public string Text
        {
            get { return mText; }
        }

        public int AnnotationCount
        {
            get { return mAnnotations.Count; }
        }

        public ArrayList<Annotation>.ReadOnly Annotations
        {
            get { return mAnnotations; }
        }

        public void AddAnnotation(Annotation annotation)
        {
            Utils.ThrowException(annotation == null ? new ArgumentNullException("annotation") : null);
            Utils.ThrowException(mAnnotations.Contains(annotation) ? new ArgumentValueException("annotation") : null);
            Utils.ThrowException((annotation.Id != -1 && mAnnotations.Count != 0 && mAnnotations.Last.Id >= annotation.Id) ?
                new ArgumentValueException("annotation") : null);
            if (annotation.Id == -1)
            {
                int id = -1;
                if (mAnnotations.Count != 0)
                {
                    id = mAnnotations.Last.Id;
                }
                annotation.SetId(id + 1);
            }
            mAnnotations.Add(annotation);
        }

        private int GetIdx(int id)
        {
            Annotation key = new Annotation(0, 0, "");
            key.SetId(id);
            return mAnnotations.BinarySearch(key, mAnnotationComparer);
        }

        public bool RemoveAnnotation(int id)
        {
            Utils.ThrowException(id < 0 ? new ArgumentOutOfRangeException("id") : null);
            int idx = GetIdx(id);
            if (idx > 0)
            {
                mAnnotations.RemoveAt(idx);
                return true;
            }
            return false;
        }

        public void RemoveAnnotationAt(int idx)
        {
            mAnnotations.RemoveAt(idx); // throws ArgumentOutOfRangeException
        }

        public Annotation GetAnnotation(int id)
        {
            Utils.ThrowException(id < 0 ? new ArgumentOutOfRangeException("id") : null);
            int idx = GetIdx(id);
            if (idx > 0)
            {
                return mAnnotations[idx];
            }
            return null;
        }

        public Annotation GetAnnotationAt(int idx)
        {
            return mAnnotations[idx]; // throws ArgumentOutOfRangeException
        }
    }
}
