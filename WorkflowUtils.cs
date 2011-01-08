/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    WorkflowUtils.cs
 *  Desc:    Common constants and utilities 
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
       |  Class WorkflowUtils
       |
       '-----------------------------------------------------------------------
    */
    public static class WorkflowUtils
    {
        public const string TimeFmt
            = "ddd, dd MMM yyyy HH:mm:ss K"; // RFC 822 format (incl. time zone)

        //public static object InvokeStreamDataProcessor(Type processorType, object inData)
        //{
        //    object outData = null;
        //    using (IDataProducer processor = (IDataProducer)processorType.GetConstructor(new Type[0]).Invoke(new object[0]))
        //    {
        //        using (GenericStreamDataConsumer consumer = new GenericStreamDataConsumer())
        //        {
        //            consumer.OnConsumeData
        //                += new GenericStreamDataConsumer.ConsumeDataHandler(delegate(IDataProducer producer, object data) { outData = data; });
        //            processor.Subscribe(consumer);
        //            ((IDataConsumer)processor).ReceiveData(null, inData);
        //            while (outData == null) { Thread.Sleep(100); }
        //        }
        //    }
        //    return outData;
        //}
    }
}
