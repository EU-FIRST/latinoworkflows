/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    WorkflowUtils.cs
 *  Desc:    Common constants and utilities 
 *  Created: Dec-2010
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Enum DispatchPolicy
       |
       '-----------------------------------------------------------------------
    */
    public enum DispatchPolicy
    {
        ToAll,
        Random,
        BalanceLoadSum,
        BalanceLoadMax
    }

    /* .-----------------------------------------------------------------------
       |
       |  Class WorkflowUtils
       |
       '-----------------------------------------------------------------------
    */
    internal static class WorkflowUtils
    {
        private static Random mRandom
            = new Random();
        
        public static Logger CreateLogger(string loggerBaseName, string name)
        {
            if (loggerBaseName == null && name == null)
            {
                return Logger.GetRootLogger();
            }
            else if (loggerBaseName == null)
            {
                return Logger.GetLogger(name);
            }
            else if (name == null)
            {
                return Logger.GetLogger(loggerBaseName);
            }
            else
            {
                return Logger.GetLogger(loggerBaseName + "." + name);
            }
        }

        public static int GetBranchLoadMax(IWorkflowComponent component)
        {
            if (component is StreamDataProducer)
            {
                StreamDataProducer producer = (StreamDataProducer)component;
                int load = 0;
                foreach (IWorkflowComponent subscriber in producer.SubscribedConsumers)
                {
                    int subscriberLoad = GetBranchLoadMax(subscriber);
                    if (subscriberLoad > load) { load = subscriberLoad; }
                }
                return load;
            }
            else if (component is StreamDataProcessor)
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

        public static int GetBranchLoadSum(IWorkflowComponent component)
        {
            if (component is StreamDataProducer)
            {
                StreamDataProducer producer = (StreamDataProducer)component;
                int load = 0;
                foreach (IWorkflowComponent subscriber in producer.SubscribedConsumers)
                {
                    load += GetBranchLoadSum(subscriber);
                }
                return load;
            }
            else if (component is StreamDataProcessor)
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

        public static void DispatchData(IDataProducer producer, object data, bool cloneDataOnFork, DispatchPolicy dispatchPolicy, 
            Set<IDataConsumer> dataConsumers, Logger logger)
        {
            if (data == null) { return; }
            if (dataConsumers.Count == 0)
            {
                if (logger != null) { logger.Warn("DispatchData", "Data ready but nobody is listening."); }
                return;
            }
            if (dispatchPolicy == DispatchPolicy.BalanceLoadSum || dispatchPolicy == DispatchPolicy.BalanceLoadMax)
            {
                if (logger != null) { logger.Trace("DispatchData", "Dispatching data of type {0} (load balancing) ...", data.GetType()); }
                int minLoad = int.MaxValue;
                IDataConsumer target = null;
                foreach (IDataConsumer consumer in dataConsumers)
                {
                    int load = (dispatchPolicy == DispatchPolicy.BalanceLoadSum) ? GetBranchLoadSum(consumer) : GetBranchLoadMax(consumer);
                    if (load < minLoad) { minLoad = load; target = consumer; }
                }
                target.ReceiveData(producer, data);
            }
            else if (dispatchPolicy == DispatchPolicy.Random)
            {
                if (logger != null) { logger.Trace("DispatchData", "Dispatching data of type {0} (random policy) ...", data.GetType()); }
                ArrayList<IDataConsumer> tmp = new ArrayList<IDataConsumer>(dataConsumers.Count);
                foreach (IDataConsumer dataConsumer in dataConsumers) { tmp.Add(dataConsumer); }
                tmp[mRandom.Next(0, tmp.Count)].ReceiveData(producer, data);
            }
            else
            {
                if (logger != null) { logger.Trace("DispatchData", "Dispatching data of type {0} (to-all policy) ...", data.GetType()); }
                if (dataConsumers.Count > 1 && cloneDataOnFork)
                {
                    foreach (IDataConsumer dataConsumer in dataConsumers)
                    {
                        dataConsumer.ReceiveData(producer, Utils.Clone(data, /*deepClone=*/true));
                    }
                }
                else
                {
                    foreach (IDataConsumer dataConsumer in dataConsumers)
                    {
                        dataConsumer.ReceiveData(producer, data);
                    }
                }
            }
            if (logger != null) { logger.Trace("DispatchData", "Data dispatched."); }
        }
    }
}