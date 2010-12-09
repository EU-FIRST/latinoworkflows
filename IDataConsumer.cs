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

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Interface IDataConsumer
       |
       '-----------------------------------------------------------------------
    */
    public interface IDataConsumer
    {
        void ReceiveData(object data);
    }
}
