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
        private bool mStopped
            = false;
        private Thread mThread
            = null;
        private bool mCloneDataOnFork
            = true;
        protected Log mLog;

        public StreamDataProducer()
        {
            mLog = new Log(GetType().ToString());
        }

        public Log Logging
        {
            get { return mLog; }
        }

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        public void Start()
        {
            Utils.ThrowException(IsRunning ? new InvalidOperationException() : null);
            mLog.Debug("Start", "Starting ...");
            mThread = new Thread(new ThreadStart(ProduceDataLoop));
            mStopped = false;
            mThread.Start();
            mLog.Debug("Start", "Started.");
        }

        public void Stop()
        {            
            Utils.ThrowException(!IsRunning ? new InvalidOperationException() : null);
            mLog.Debug("Stop", "Stopping ...");
            mStopped = true;
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

        private void ProduceDataLoop()
        {
            while (!mStopped)
            {
                try
                {
                    // produce data
                    object data = ProduceData();                    
                    if (data != null)
                    {
                        mLog.Debug("ProduceDataLoop", "Produced data of type {0}.", data.GetType());
                        // dispatch data
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
                    }
                }
                catch (Exception exc)
                {
                    mLog.Critical("ProduceDataLoop", exc);
                }
                int sleepTime = Math.Min(500, mTimeBetweenPolls);
                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < mTimeBetweenPolls)
                {
                    if (mStopped) { mLog.Info("ProduceDataLoop", "Stopped."); return; }    
                    Thread.Sleep(sleepTime);
                }
            }
            mLog.Info("ProduceDataLoop", "Stopped.");
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
            mLog.Debug("Dispose", "Disposing ...");
            if (IsRunning)
            {
                Stop();
                while (IsRunning) { Thread.Sleep(100); }
            }
            mLog.Debug("Dispose", "Disposed.");
        }
    }
}

