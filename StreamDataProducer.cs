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
        private int mSleep
            = 1;
        protected bool mStopped
            = false;
        private Thread mThread
            = null;
        private bool mCloneDataOnFork
            = true;

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        public void Start()
        {
            if (mThread == null || !mThread.IsAlive)
            {
                mThread = new Thread(new ThreadStart(ProduceDataLoop));
                mStopped = false;
                mThread.Start();                
            }
        }

        public void Stop()
        {
            mStopped = true;
            while (mThread.IsAlive) { Thread.Sleep(1); }
        }

        public int SleepBetweenPolls
        {
            get { return mSleep; }
            set 
            {
                Utils.ThrowException(value < 0 ? new ArgumentOutOfRangeException("Sleep") : null);
                mSleep = value; 
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
                        if (mStopped) { return; }
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
                    Log.Critical(exc);
                }
                if (mStopped) { return; }
                Thread.Sleep(mSleep);
            }
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
    }
}
