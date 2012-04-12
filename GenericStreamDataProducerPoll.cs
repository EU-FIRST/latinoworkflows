/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    GenericStreamDataProducerPoll.cs
 *  Desc:    Generic (customizable) stream data producer (polling)
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
       |  Class GenericStreamDataProducerPoll
       |
       '-----------------------------------------------------------------------
    */
    public class GenericStreamDataProducerPoll : StreamDataProducerPoll
    {
        public delegate object ProduceDataHandler();

        public event ProduceDataHandler OnProduceData
            = null;

        public GenericStreamDataProducerPoll() : base(typeof(GenericStreamDataProducerPoll))
        {
        }

        protected override object ProduceData()
        {
            Utils.ThrowException(OnProduceData == null ? new ArgumentValueException("OnProduceData") : null);
            return OnProduceData();
        }
    }
}
