/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProcessor.cs
 *  Desc:    Stream data processor base class
 *  Created: Dec-2010
 *
 *  Author:  Miha Grcar
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

        public StreamDataProcessor(string loggerName) : base(loggerName)
        { 
        }

        public StreamDataProcessor(Type loggerType) : this(loggerType.ToString())
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
            WorkflowUtils.DispatchData(this, data, mCloneDataOnFork, mDispatchPolicy, mDataConsumers, mLogger);
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
