/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IDataConsumer.cs
 *  Desc:    Data consumer interface
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
       |  Interface IDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public interface IDataConsumer : IDisposable
    {
        void ReceiveData(IDataProducer sender, object data);
        void Stop();
        void Resume();
        bool IsRunning { get; }
    }
}
