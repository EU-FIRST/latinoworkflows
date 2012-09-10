/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProducerPoll.cs
 *  Desc:    Stream data producer base class (polling)
 *  Created: Dec-2010
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Threading;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class StreamDataProducerPoll
       |
       '-----------------------------------------------------------------------
    */
    public abstract class StreamDataProducerPoll : StreamDataProducer
    {
        private int mTimeBetweenPolls
            = 1;
        private bool mRandomDelayAtStart
            = true; // TODO: make this configurable
        private Random mRng
            = new Random();

        protected bool mStopped
            = false;
        private Thread mThread
            = null;
        
        public StreamDataProducerPoll(string loggerBaseName) : base(loggerBaseName)
        {
        }

        public StreamDataProducerPoll(Type loggerType) : base(loggerType)
        {
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
            if (mRandomDelayAtStart)
            {
                Thread.Sleep(mRng.Next(0, mTimeBetweenPolls));
            }            
            while (!mStopped)
            {
                try
                {
                    // produce and dispatch data
                    object data = ProduceData();                    
                    DispatchData(data);
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

        public override void Start()
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

        public override void Stop()
        {
            if (IsRunning)
            {
                mLogger.Debug("Stop", "Stopping ...");
                mStopped = true;
            }
        }

        public override bool IsRunning
        {
            get { return mThread != null && mThread.IsAlive; }
        }

        // *** IDisposable interface implementation ***

        public override void Dispose()
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

