/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataConsumer.cs
 *  Desc:    Stream data consumer base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class StreamDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public abstract class StreamDataConsumer : IDataConsumer
    {
        private Queue<Pair<IDataProducer, object>> mQueue
            = new Queue<Pair<IDataProducer, object>>();
        private bool mThreadAlive
            = false;
        private Thread mThread;

        public StreamDataConsumer()
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
        }

        private void ProcessQueue()
        {
            while (true)
            {
                while (true)
                {
                    // get data
                    Pair<IDataProducer, object> data;
                    lock (mQueue)
                    {
                        data = mQueue.Dequeue();
                    }
                    // consume data
                    try
                    {
                        ConsumeData(data.First, data.Second);
                    }
                    catch (Exception exc)
                    {
                        Log.Critical(exc);
                    }
                    // check if more data available
                    lock (mQueue)
                    {
                        mThreadAlive = mQueue.Count > 0;
                        if (!mThreadAlive) { break; }
                    }
                } 
                Thread.CurrentThread.Suspend();
            } 
        }

        protected abstract void ConsumeData(IDataProducer sender, object data);

        // *** IDataConsumer interface implementation ***

        public void ReceiveData(IDataProducer sender, object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            // *** note that setting sender to null is allowed
            lock (mQueue)
            {
                mQueue.Enqueue(new Pair<IDataProducer, object>(sender, data));
                if (!mThreadAlive)
                {
                    mThreadAlive = true;
                    if (!mThread.IsAlive)
                    {
                        mThread.Start();
                    }
                    else
                    {
                        while (mThread.ThreadState != ThreadState.Suspended) { Thread.Sleep(1); }                    
                        mThread.Resume();
                    }
                }
            }
        }
    }
}
