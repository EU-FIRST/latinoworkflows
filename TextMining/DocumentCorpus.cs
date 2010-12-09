/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentCorpus.cs
 *  Desc:    Annotated document corpus data structure
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
       |  Class DocumentCorpus
       |
       '-----------------------------------------------------------------------
    */
    public class DocumentCorpus //ICloneable<DocumentCorpus>
    {
        private ArrayList<Document> mDocuments
            = new ArrayList<Document>();

        public void Add(Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            Utils.ThrowException(mDocuments.Contains(document) ? new ArgumentValueException("document") : null);
            mDocuments.Add(document);
        }

        public void AddRange(IEnumerable<Document> documents)
        {
            Utils.ThrowException(documents == null ? new ArgumentNullException("documents") : null);
            foreach (Document document in documents)
            {
                Add(document); // throws ArgumentNullException, ArgumentValueException
            }
        }

        public void Clear()
        {
            mDocuments.Clear();
        }

        public void Insert(int index, Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            Utils.ThrowException(mDocuments.Contains(document) ? new ArgumentValueException("document") : null);
            mDocuments.Insert(index, document); // throws ArgumentOutOfRangeException
        }

        public void InsertRange(int index, IEnumerable<Document> documents)
        {
            Utils.ThrowException(documents == null ? new ArgumentNullException("documents") : null);
#if THROW_EXCEPTIONS
            foreach (Document document in documents)
            {
                if (document == null || mDocuments.Contains(document)) { throw new ArgumentValueException("documents"); }
            }
#endif
            mDocuments.InsertRange(index, documents); // throws ArgumentOutOfRangeException
        }

        public bool Remove(Document document)
        {
            Utils.ThrowException(document == null ? new ArgumentNullException("document") : null);
            return mDocuments.Remove(document);
        }

        public void RemoveAt(int index)
        {
            mDocuments.RemoveAt(index); // throws ArgumentOutOfRangeException
        }

        public void RemoveRange(int index, int count)
        {
            mDocuments.RemoveRange(index, count); // throws ArgumentOutOfRangeException, ArgumentException
        }

        public ArrayList<Document>.ReadOnly Documents
        {
            get { return mDocuments; }
        }
    }
}