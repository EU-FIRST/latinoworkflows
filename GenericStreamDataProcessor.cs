/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    GenericStreamDataProcessor.cs
 *  Desc:    Generic (customizable) stream data processor
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

namespace Latino.Workflows
{
    /* .-----------------------------------------------------------------------
       |
       |  Class GenericStreamDataProcessor
       |
       '-----------------------------------------------------------------------
    */
    public class GenericStreamDataProcessor : StreamDataProcessor
    {
        public delegate object ProcessDataHandler(IDataProducer sender, object data);

        public event ProcessDataHandler OnProcessData
            = null;

        public GenericStreamDataProcessor() : base(typeof(GenericStreamDataProcessor))
        {
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            if (OnProcessData != null)
            {
                return OnProcessData(sender, data);
            }
            return data;
        }
    }
}
