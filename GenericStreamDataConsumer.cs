/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    GenericStreamDataConsumer.cs
 *  Desc:    Generic (customizable) stream data consumer
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class GenericStreamDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public class GenericStreamDataConsumer : StreamDataConsumer
    {
        public delegate void ConsumeDataHandler(IDataProducer sender, object data);

        public event ConsumeDataHandler OnConsumeData
            = null;

        public GenericStreamDataConsumer() : base("Latino.Workflows.GenericStreamDataConsumer")
        {
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            if (OnConsumeData != null)
            {
                OnConsumeData(sender, data);
            }
        }
    }
}
