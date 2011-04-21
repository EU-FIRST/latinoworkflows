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

        public StreamDataProcessor(string loggerName) : base(loggerName)
        { 
        }

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            // process data
            data = ProcessData(sender, data);
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
