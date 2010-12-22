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
        protected bool mStopped
            = false;
        private Thread mThread;

        public StreamDataConsumer()
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
        }

        public void Stop()
        {
            lock (mQueue)
            {
                mStopped = true;
                mQueue.Clear();
                if ((mThread.ThreadState | ThreadState.Suspended) != 0) 
                {
                    mThread.Resume();
                }
                mThreadAlive = false;
            }
            while (mThread.IsAlive) { Thread.Sleep(1); }
        }

        public void Resume()
        {
            //lock (mQueue)
            //{
                if (mStopped)
                {
                    mThread = new Thread(new ThreadStart(ProcessQueue));
                    mStopped = false;
                }
            //}
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
                        if (mStopped) { return; }
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
                        if (mStopped) { return; }
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
                if (!mStopped)
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
                            while ((mThread.ThreadState | ThreadState.Suspended) == 0) { Thread.Sleep(1); }
                            mThread.Resume();
                        }
                    }
                }
            }
        }
    }
}
