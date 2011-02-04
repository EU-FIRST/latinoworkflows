/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RssHistory.cs
 *  Desc:    Shared history for RSS components
 *  Created: Feb-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class RssHistory
       |
       '-----------------------------------------------------------------------
    */
    public class RssHistory : ISerializable
    {
        private Pair<Set<Guid>, Queue<Guid>> mHistory
            = new Pair<Set<Guid>, Queue<Guid>>(new Set<Guid>(), new Queue<Guid>());
        private int mHistorySize
            = 1000;
        private object mLock
            = new object();

        public RssHistory()
        { 
        }

        public RssHistory(BinarySerializer reader)
        {
            Load(reader); // throws ArgumentNullException, serialization-related exceptions
        }

        public RssHistory(int size)
        {
            Utils.ThrowException(size < 0 ? new ArgumentOutOfRangeException("size") : null);
            mHistorySize = size;
        }

        public RssHistory(int size, IEnumerable<Guid> history) : this(size) // throws ArgumentOutOfRangeException
        {
            Utils.ThrowException(history == null ? new ArgumentNullException("history") : null);
            if (size > 0)
            {
                foreach (Guid item in history)
                {
                    AddToHistoryNoLock(item);
                }
            }
        }

        public bool Contains(Guid item)
        {
            lock (mLock)
            {
                return mHistory.First.Contains(item);
            }
        }

        private void AddToHistoryNoLock(Guid item)
        {
            if (!mHistory.First.Contains(item) && mHistorySize > 0)
            {
                if (mHistory.First.Count + 1 > mHistorySize)
                {
                    mHistory.First.Remove(mHistory.Second.Dequeue());
                }
                mHistory.First.Add(item);
                mHistory.Second.Enqueue(item);
            }
        }

        public void AddToHistory(Guid item)
        {
            lock (mLock)
            {
                AddToHistoryNoLock(item);
            }
        }

        public void Forget()
        {
            lock (mLock)
            {
                mHistory.First.Clear();
                mHistory.Second.Clear();
            }
        }

        //public override string ToString()
        //{
        //    string str = string.Format("Topmost: {0}", mHistory.Second.Peek());
        //    ArrayList<Guid> al = new ArrayList<Guid>(mHistory.Second.ToArray());
        //    str += "\r\n" + al.ToString();
        //    return str;
        //}

        // *** ISerializable interface implementation ***

        public void Save(BinarySerializer writer)
        {
            Utils.ThrowException(writer == null ? new ArgumentNullException("writer") : null);
            // the following statements throw serialization-related exception  
            writer.WriteInt(mHistorySize);
            new ArrayList<Guid>(mHistory.Second.ToArray()).Save(writer);
        }

        public void Load(BinarySerializer reader)
        {
            Utils.ThrowException(reader == null ? new ArgumentNullException("reader") : null);
            mHistory.First.Clear();
            mHistory.Second.Clear();
            // the following statements throw serialization-related exception
            mHistorySize = reader.ReadInt();
            ArrayList<Guid> hist = new ArrayList<Guid>(reader);
            foreach (Guid item in hist)
            {
                AddToHistoryNoLock(item);
            }
        }
    }
}
