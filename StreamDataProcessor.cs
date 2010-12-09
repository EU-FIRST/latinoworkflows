/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataProcessor.cs
 *  Desc:    Stream data processor base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

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

        public bool CloneDataOnFork
        {
            get { return mCloneDataOnFork; }
            set { mCloneDataOnFork = value; }
        }

        protected override void ConsumeData(object data)
        {
            // process data
            data = ProcessData(data);
            // dispatch data
            if (mDataConsumers.Count > 1 && mCloneDataOnFork)
            {
                foreach (IDataConsumer dataConsumer in mDataConsumers)
                {
                    dataConsumer.ReceiveData(Utils.Clone(data, /*deepClone=*/true));
                }
            }
            else 
            {
                foreach (IDataConsumer dataConsumer in mDataConsumers)
                {
                    dataConsumer.ReceiveData(data);
                }
            }
        }

        protected abstract object ProcessData(object data);

        // *** IDataProducer interface implementation ***

        public void Subscribe(IDataConsumer dataConsumer)
        {
            mDataConsumers.Add(dataConsumer);
        }

        public void Unsubscribe(IDataConsumer dataConsumer)
        {
            if (mDataConsumers.Contains(dataConsumer))
            {
                mDataConsumers.Remove(dataConsumer);
            }
        }
    }
}
