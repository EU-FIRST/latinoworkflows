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
        protected Log mLog;

        public StreamDataConsumer()
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
            mLog = new Log(GetType().ToString());
        }

        public Log Logging
        {
            get { return mLog; }
        }

        public void Stop()
        {            
            Utils.ThrowException(!IsRunning ? new InvalidOperationException() : null);
            mLog.Debug("Stop", "Stopping ...");
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
            mLog.Debug("Resume", "Resuming ...");
            lock (mQueue)
            {
                mStopped = false;
                mThread = new Thread(new ThreadStart(ProcessQueue));
                mThreadAlive = mQueue.Count > 0;
                if (mThreadAlive) { mThread.Start(); }
            }
            mLog.Debug("Resume", "Resumed.");
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
                        if (mStopped) { mLog.Debug("Stop", "Stopped."); return; }
                        data = mQueue.Dequeue();
                    }
                    // consume data
                    try
                    {
                        ConsumeData(data.First, data.Second);
                    }
                    catch (Exception exc)
                    {
                        mLog.Critical("ProcessQueue", exc);
                    }
                    // check if more data available
                    lock (mQueue)
                    {
                        if (mStopped) { mLog.Debug("Stop", "Stopped."); return; }
                        mThreadAlive = mQueue.Count > 0;
                        if (!mThreadAlive) { break; }
                    }
                }
                Thread.CurrentThread.Suspend();
            }
            mLog.Debug("Stop", "Stopped.");
        }

        protected abstract void ConsumeData(IDataProducer sender, object data);

        // *** IDataConsumer interface implementation ***

        public void ReceiveData(IDataProducer sender, object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            // *** note that setting sender to null is allowed
            mLog.Debug("ReceiveData", "Received data of type {0}.", data.GetType());
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

        public int QueueSize
        {
            get { return mQueue.Count; }
        }

        // *** IDisposable interface implementation ***

        public void Dispose()
        {
            mLog.Debug("Dispose", "Disposing ...");
            Stop();
            while (IsRunning) { Thread.Sleep(100); }
            mLog.Debug("Dispose", "Disposed.");
        }
    }
}