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
        private static object mDispatchLock
            = new object();

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

        internal static int GetBranchLoadMax(IWorkflowComponent component)
        {
            if (component is StreamDataProcessor)
            {
                StreamDataProcessor processor = (StreamDataProcessor)component;
                int load = processor.Load;
                foreach (IWorkflowComponent subscriber in processor.SubscribedConsumers)
                {
                    int subscriberLoad = GetBranchLoadMax(subscriber);
                    if (subscriberLoad > load) { load = subscriberLoad; }
                }
                return load;
            }
            else if (component is StreamDataConsumer)
            {
                StreamDataConsumer consumer = (StreamDataConsumer)component;
                return consumer.Load;
            }
            return 0;
        }

        internal static int GetBranchLoadSum(IWorkflowComponent component)
        {
            if (component is StreamDataProcessor)
            {
                StreamDataProcessor processor = (StreamDataProcessor)component;
                int load = processor.Load;
                foreach (IWorkflowComponent subscriber in processor.SubscribedConsumers)
                {
                    load += GetBranchLoadSum(subscriber);
                }
                return load;
            }
            else if (component is StreamDataConsumer)
            {
                StreamDataConsumer consumer = (StreamDataConsumer)component;
                return consumer.Load;
            }
            return 0;
        }

        protected void DispatchData(object data)
        {
            Utils.ThrowException(data == null ? new ArgumentNullException("data") : null);
            if (mDataConsumers.Count == 0)
            {
                mLogger.Warn("DispatchData", "Data ready but nobody is listening.");
                return;
            }
            if (mDispatchPolicy == DispatchPolicy.BalanceLoadSum || mDispatchPolicy == DispatchPolicy.BalanceLoadMax)
            {
                mLogger.Trace("DispatchData", "Dispatching data of type {0} (load balancing) ...", data.GetType());
                lock (mDispatchLock)
                {
                    int minLoad = int.MaxValue;
                    IDataConsumer target = null;
                    foreach (IDataConsumer consumer in mDataConsumers)
                    {
                        int load = (mDispatchPolicy == DispatchPolicy.BalanceLoadSum) ?
                            GetBranchLoadSum(consumer) : GetBranchLoadMax(consumer);
                        if (load < minLoad) { minLoad = load; target = consumer; }
                    }
                    target.ReceiveData(this, data);
                }
            }
            else if (mDispatchPolicy == DispatchPolicy.Random)
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

