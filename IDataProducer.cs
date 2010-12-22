/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IDataProducer.cs
 *  Desc:    Data producer interface
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Interface IDataProducer
       |
       '-----------------------------------------------------------------------
    */
    public interface IDataProducer
    {
        void Subscribe(IDataConsumer dataConsumer);
        void Unsubscribe(IDataConsumer dataConsumer);
        void Stop();
    }
}
