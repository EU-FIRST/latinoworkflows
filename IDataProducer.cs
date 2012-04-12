/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IDataProducer.cs
 *  Desc:    Data producer interface
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
       |  Interface IDataProducer
       |
       '-----------------------------------------------------------------------
    */
    public interface IDataProducer : IWorkflowComponent
    {
        void Subscribe(IDataConsumer dataConsumer);
        void Unsubscribe(IDataConsumer dataConsumer);
    }
}
