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
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        public static void SetProcessorAffinity(ulong mask)
        {
            Utils.ThrowException(mask == 0 ? new ArgumentOutOfRangeException("mask") : null);
            Utils.ThrowException(mask > Math.Pow(2, Environment.ProcessorCount) - 1 ? new ArgumentOutOfRangeException("mask") : null);
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)mask;
        }

        public static void SetProcessorAffinity(string mask)
        {
            Utils.ThrowException(mask == null ? new ArgumentNullException("mask") : null);
            SetProcessorAffinity(Convert.ToUInt64(mask, 2)); // throws ArgumentException, FormatException, OverflowException, ArgumentOutOfRangeException
        }

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