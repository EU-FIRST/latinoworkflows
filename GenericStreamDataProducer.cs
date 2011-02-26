/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    GenericStreamDataProducer.cs
 *  Desc:    Generic (customizable) stream data producer
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
       |  Class GenericStreamDataProducer
       |
       '-----------------------------------------------------------------------
    */
    public class GenericStreamDataProducer : StreamDataProducer
    {
        public delegate object ProduceDataHandler();

        public event ProduceDataHandler OnProduceData
            = null;

        public GenericStreamDataProducer() : base("Latino.Workflows.GenericStreamDataProducer")
        {
        }

        protected override object ProduceData()
        {
            Utils.ThrowException(OnProduceData == null ? new ArgumentValueException("OnProduceData") : null);
            return OnProduceData();
        }
    }
}
