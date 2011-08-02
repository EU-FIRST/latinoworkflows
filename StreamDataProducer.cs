/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProducer.cs
 *  Desc:    Stream data producer base class
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;

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
        private bool mCloneDataOnFork
            = true;
        private string mName
            = null;
        private DispatchPolicy mDispatchPolicy
            = DispatchPolicy.ToAll;
        private Random mRandom
            = new Random();
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

        public DispatchPolicy DispatchPolicy
        {
            get { return mDispatchPolicy; }
            set { mDispatchPolicy = value; }
        }

        protected void DispatchData(object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            if (mDispatchPolicy == DispatchPolicy.Random)
            {
                mLogger.Trace("DispatchData", "Dispatching data of type {0} (random policy) ...", data.GetType());
                ArrayList<IDataConsumer> tmp = new ArrayList<IDataConsumer>(mDataConsumers.Count);
                foreach (IDataConsumer dataConsumer in mDataConsumers) { tmp.Add(dataConsumer); }
                tmp[mRandom.Next(0, tmp.Count)].ReceiveData(this, data);               
            }
            else
            {
                mLogger.Trace("DispatchData", "Dispatching data of type {0} (to-all policy) ...", data.GetType());
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
            mLogger.Trace("DispatchData", "Data dispatched.");
        }

        // *** IDataProducer interface implementation ***

        public abstract void Start();

        public abstract void Stop();

        public abstract bool IsRunning { get; }

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

        public abstract void Dispose();
    }
}

