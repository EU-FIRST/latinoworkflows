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
        private bool mStopped
            = false;
        private Thread mThread;

        public StreamDataConsumer()
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
        }

        public void Stop()
        {
            Utils.ThrowException(!IsRunning ? new InvalidOperationException() : null);
            lock (mQueue)
            {
                mStopped = true;
                if (!mThreadAlive)
                {
                    while ((mThread.ThreadState & ThreadState.Suspended) == 0) { Thread.Sleep(1); }
                    mThread.Resume();
                }
            }
        }

        public void Resume()
        {
            Utils.ThrowException(IsRunning ? new InvalidOperationException() : null);
            lock (mQueue)
            {
                mStopped = false;
                mThread = new Thread(new ThreadStart(ProcessQueue));
                mThreadAlive = mQueue.Count > 0;
                if (mThreadAlive) { mThread.Start(); }
            }
        }

        public bool IsRunning
        {
            get { return mThread.IsAlive; }
        }

        private void ProcessQueue()
        {
            while (!mStopped)
            {
                while (!mStopped)
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
                mQueue.Enqueue(new Pair<IDataProducer, object>(sender, data));
                if (!mThreadAlive && !mStopped)
                {
                    mThreadAlive = true;
                    if (!mThread.IsAlive)
                    {
                        mThread.Start();
                    }
                    else
                    {
                        while ((mThread.ThreadState & ThreadState.Suspended) == 0) { Thread.Sleep(1); }
                        mThread.Resume();
                    }
                }
            }
        }

        // *** IDisposable interface implementation ***

        public void Dispose()
        {
            Stop();
            while (IsRunning) { Thread.Sleep(100); }
        }
    }
}