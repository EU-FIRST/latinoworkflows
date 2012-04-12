/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IDataConsumer.cs
 *  Desc:    Data consumer interface
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
       |  Interface IDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public interface IDataConsumer : IWorkflowComponent
    {
        void ReceiveData(IDataProducer sender, object data);
    }
}
