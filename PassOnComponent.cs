/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    PassOnComponent.cs
 *  Desc:    Passes the data to the consumers
 *  Created: Mar-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class PassOnComponent
       |
       '-----------------------------------------------------------------------
    */
    public class PassOnComponent : StreamDataProcessor
    {
        public PassOnComponent() : base(typeof(PassOnComponent))
        {
        }

        public/*protected*/ override object ProcessData(IDataProducer sender, object data)
        {
            return data;
        }
    }
}
