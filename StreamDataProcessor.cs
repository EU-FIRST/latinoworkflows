/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProcessor.cs
 *  Desc:    Stream data processor base class
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
       |  Class StreamDataProcessor
       |
       '-----------------------------------------------------------------------
    */
    public abstract class StreamDataProcessor : StreamDataConsumer, IDataProducer
    {
        private Set<IDataConsumer> mDataConsumers
            = new Set<IDataConsumer>();
        private bool mCloneDataOnFork
            = true;
        private DispatchPolicy mDispatchPolicy
            = DispatchPolicy.ToAll;
        private Random mRandom
            = new Random();

        public StreamDataProcessor(string loggerName) : base(loggerName)
        { 
        }

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        public DispatchPolicy DispatchPolicy
        {
            get { return mDispatchPolicy; }
            set { mDispatchPolicy = value; }
        }

        public Set<IDataConsumer>.ReadOnly SubscribedConsumers
        {
            get { return mDataConsumers; }
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            // process data
            data = ProcessData(sender, data);
            // dispatch data
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
        }

        protected abstract object ProcessData(IDataProducer sender, object data);

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
