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
using Latino.Workflows.TextMining;

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

        private Ref<int> mLoad
            = 0;
        private int mMaxLoad
            = 0;
        private Ref<TimeSpan> mProcessingTime
            = TimeSpan.Zero;
        private DateTime mStartTime
            = DateTime.MinValue;
        private Ref<int> mNumItemsProcessed
            = 0;
        private Ref<int> mNumDocumentsProcessed
            = 0;
        private bool mThreadAlive
            = false;
        private bool mStopped
            = false;
        private string mName
            = null;

        private Thread mThread;

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

        public int Load
        {
            get { return mLoad.Val; }
        }

        public int GetMaxLoad()
        {
            lock (mLoad)
            {
                int maxLoad = mMaxLoad;
                mMaxLoad = mLoad.Val;
                return maxLoad;
            }
        }

        public double GetProcessingTimeSec(DateTime currentTime)
        {
            lock (mProcessingTime)
            {
                if (mStartTime != DateTime.MinValue)
                {
                    mProcessingTime.Val += currentTime - mStartTime;
                    mStartTime = currentTime;
                }
                double timeSec = mProcessingTime.Val.TotalSeconds;
                mProcessingTime.Val = TimeSpan.Zero;
                return timeSec;
            }
        }

        public int GetNumItemsProcessed()
        {
            lock (mNumItemsProcessed)
            {
                int val = mNumItemsProcessed.Val;
                mNumItemsProcessed.Val = 0;
                return val;
            }
        }

        public int GetNumDocumentsProcessed()
        {
            lock (mNumDocumentsProcessed)
            {
                int val = mNumDocumentsProcessed.Val;
                mNumDocumentsProcessed.Val = 0;
                return val;
            }
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
                        mStartTime = DateTime.Now;
                        ConsumeData(data.First, data.Second);
                        lock (mProcessingTime) { mProcessingTime.Val += DateTime.Now - mStartTime; mStartTime = DateTime.MinValue; }
                        lock (mNumItemsProcessed) { mNumItemsProcessed.Val++; }
                        if (data.Second is DocumentCorpus)
                        {
                            lock (mNumDocumentsProcessed)
                            {
                                mNumDocumentsProcessed.Val += ((DocumentCorpus)data.Second).Documents.Count;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        mLogger.Error("ProcessQueue", exc);
                    }
                    // check if more data available
                    lock (mQueue)
                    {
                        lock (mLoad) { mLoad.Val--; }
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
                lock (mLoad) 
                { 
                    mLoad.Val++;
                    if (mLoad.Val >= mMaxLoad) { mMaxLoad = mLoad.Val; }
                }
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