/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataConsumer.cs
 *  Desc:    Stream data consumer base class
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
        private int mMaxQueueSize
            = 0;
        private DateTime mMaxQueueSizeTime
            = DateTime.MinValue;
        private string mName
            = null;
        private string mLoggerBaseName;
        protected Logger mLogger;

        public StreamDataConsumer(string loggerBaseName)
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
            mLogger = WorkflowUtils.CreateLogger(mLoggerBaseName = loggerBaseName, mName);
        }

        public StreamDataConsumer(Type loggerType) : this(loggerType.ToString())
        { 
        }

        public bool IsSuspended
        {
            get { return (mThread.ThreadState & ThreadState.Suspended) != 0; }
        }

        public int QueueSize
        {
            get { return mQueue.Count; }
        }

        public int MaxQueueSize
        {
            get { return mMaxQueueSize; }
        }

        public DateTime MaxQueueSizeTime
        {
            get { return mMaxQueueSizeTime; }
        }

        public string Name
        {
            get { return mName; }
            set
            {
                mName = value;
                mLogger = WorkflowUtils.CreateLogger(mLoggerBaseName, mName);
            }
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
                        if (mStopped) { mLogger.Debug("Stop", "Stopped."); return; }
                        data = mQueue.Dequeue();
                    }
                    // consume data
                    try
                    {
                        ConsumeData(data.First, data.Second);
                    }
                    catch (Exception exc)
                    {
                        mLogger.Error("ProcessQueue", exc);
                    }
                    // check if more data available
                    lock (mQueue)
                    {
                        if (mStopped) { mLogger.Debug("Stop", "Stopped."); return; }
                        mThreadAlive = mQueue.Count > 0;
                        if (!mThreadAlive) { break; }
                    }
                }
                Thread.CurrentThread.Suspend();
            }
            mLogger.Debug("Stop", "Stopped.");
        }

        protected abstract void ConsumeData(IDataProducer sender, object data);

        // *** IDataConsumer interface implementation ***

        public void Start()
        {
            if (!IsRunning)
            {
                mLogger.Debug("Start", "Resuming ...");
                lock (mQueue)
                {
                    mStopped = false;
                    mThread = new Thread(new ThreadStart(ProcessQueue));
                    mThreadAlive = mQueue.Count > 0;
                    if (mThreadAlive) { mThread.Start(); }
                }
                mLogger.Debug("Start", "Resumed.");
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                mLogger.Debug("Stop", "Stopping ...");
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
        }

        public bool IsRunning
        {
            get { return mThread.IsAlive; }
        }

        public void ReceiveData(IDataProducer sender, object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            // *** note that setting sender to null is allowed
            mLogger.Debug("ReceiveData", "Received data of type {0}.", data.GetType());
            lock (mQueue)
            {
                mQueue.Enqueue(new Pair<IDataProducer, object>(sender, data));
                if (mQueue.Count >= mMaxQueueSize) { mMaxQueueSize = mQueue.Count; mMaxQueueSizeTime = DateTime.Now; }
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
            mLogger.Debug("Dispose", "Disposing ...");
            Stop();
            while (IsRunning) { Thread.Sleep(100); }
            mLogger.Debug("Dispose", "Disposed.");
        }
    }
}