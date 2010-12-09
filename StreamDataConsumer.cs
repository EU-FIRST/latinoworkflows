/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    StreamDataConsumer.cs
 *  Desc:    Stream data consumer base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading;

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class StreamDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public abstract class StreamDataConsumer : IDataConsumer
    {
        private Queue<object> mQueue
            = new Queue<object>();
        private bool mThreadAlive
            = false;
        private Thread mThread;

        public StreamDataConsumer()
        { 
            mThread = new Thread(new ThreadStart(ProcessQueue));
        }

        private void ProcessQueue()
        {
            while (true)
            {
                while (true)
                {
                    // get data
                    object data;
                    lock (mQueue)
                    {
                        data = mQueue.Dequeue();
                    }
                    // consume data
                    try
                    {
                        ConsumeData(data);
                    }
                    catch (Exception exc)
                    {
                        Log.Critical(exc);
                    }
                    // check if more data available
                    lock (mQueue)
                    {
                        mThreadAlive = mQueue.Count > 0;
                        if (!mThreadAlive) { break; }
                    }
                } 
                Thread.CurrentThread.Suspend();
            } 
        }

        protected abstract void ConsumeData(object data);

        // *** IDataConsumer interface implementation ***

        public void ReceiveData(object data)
        {
            lock (mQueue)
            {
                mQueue.Enqueue(data);
                if (!mThreadAlive)
                {
                    mThreadAlive = true;
                    if (!mThread.IsAlive)
                    {
                        mThread.Start();
                    }
                    else
                    {
                        while (mThread.ThreadState != ThreadState.Suspended) { Thread.Sleep(1); }                    
                        mThread.Resume();
                    }
                }
            }
        }
    }
}
