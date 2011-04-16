/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProducer.cs
 *  Desc:    Stream data producer base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Threading;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class StreamDataProducer
       |
       '-----------------------------------------------------------------------
    */
    public abstract class StreamDataProducer : IDataProducer
    {
        private Set<IDataConsumer> mDataConsumers
            = new Set<IDataConsumer>();
        private int mTimeBetweenPolls
            = 1;
        protected bool mStopped
            = false;
        private Thread mThread
            = null;
        private bool mCloneDataOnFork
            = true;
        private string mName
            = null;
        private string mLoggerBaseName;
        protected Logger mLogger;

        public StreamDataProducer(string loggerBaseName)
        {
            mLoggerBaseName = loggerBaseName;
            mLogger = WorkflowUtils.CreateLogger(mLoggerBaseName, mName);
        }

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        public void Start()
        {
            if (!IsRunning)
            {
                mLogger.Debug("Start", "Starting ...");
                mThread = new Thread(new ThreadStart(ProduceDataLoop));
                mStopped = false;
                mThread.Start();
                mLogger.Debug("Start", "Started.");
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                mLogger.Debug("Stop", "Stopping ...");
                mStopped = true;
            }
        }

        public void Resume()
        {
            Start(); // throws InvalidOperationException
        }

        public bool IsRunning
        {
            get { return mThread != null && mThread.IsAlive; }
        }

        public int TimeBetweenPolls
        {
            get { return mTimeBetweenPolls; }
            set 
            {
                Utils.ThrowException(value < 0 ? new ArgumentOutOfRangeException("TimeBetweenPolls") : null);
                mTimeBetweenPolls = value; 
            }
        }

        public Set<IDataConsumer>.ReadOnly SubscribedConsumers
        {
            get { return mDataConsumers; }
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

        protected void DispatchData(object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            mLogger.Debug("DispatchData", "Dispatching data of type {0} ...", data.GetType());
            if (mDataConsumers.Count > 1 && mCloneDataOnFork)
            {
                foreach (IDataConsumer dataConsumer in mDataConsumers)
                {
                    dataConsumer.ReceiveData(this, Utils.Clone(data, /*deepClone=*/true));
                }
            }
            else
            {
                foreach (IDataConsumer dataConsumer in mDataConsumers)
                {
                    dataConsumer.ReceiveData(this, data);
                }
            }
            mLogger.Debug("DispatchData", "Data dispatched.");
        }

        private void ProduceDataLoop()
        {
            while (!mStopped)
            {
                try
                {
                    // produce and dispatch data
                    object data = ProduceData();                    
                    if (data != null)
                    {
                        DispatchData(data);
                    }
                }
                catch (Exception exc)
                {
                    mLogger.Error("ProduceDataLoop", exc);
                }
                int sleepTime = Math.Min(500, mTimeBetweenPolls);
                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < mTimeBetweenPolls)
                {
                    if (mStopped) { mLogger.Info("ProduceDataLoop", "Stopped."); return; }    
                    Thread.Sleep(sleepTime);
                }
            }
            mLogger.Info("ProduceDataLoop", "Stopped.");
        }

        protected abstract object ProduceData();

        // *** IDataProducer interface implementation ***

        public void Subscribe(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            mDataConsumers.Add(dataConsumer);
        }

        public void Unsubscribe(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            if (mDataConsumers.Contains(dataConsumer))
            {
                mDataConsumers.Remove(dataConsumer);
            }
        }

        // *** IDisposable interface implementation ***

        public void Dispose()
        {
            mLogger.Debug("Dispose", "Disposing ...");
            if (IsRunning)
            {
                Stop();
                while (IsRunning) { Thread.Sleep(100); }
            }
            mLogger.Debug("Dispose", "Disposed.");
        }
    }
}

