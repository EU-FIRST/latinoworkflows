/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    ZeroMqEmitterComponent.cs
 *  Desc:    ZeroMQ emitter component
 *  Created: Sep-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using Latino.Workflows.TextMining;
//using log4net;
//using log4net.Config;
using System.Xml;
using System.IO;
using Messaging;

namespace Latino.Workflows.Persistance
{
    /* .-----------------------------------------------------------------------
       |
       |  Class ZeroMqEmitter
       |
       '-----------------------------------------------------------------------
    */
    public class ZeroMqEmitterComponent : StreamDataConsumer
    {
        private Messenger mMessenger 
            = new Messenger();

        public ZeroMqEmitterComponent() : base(typeof(ZeroMqEmitterComponent))
        {
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings);
            ((DocumentCorpus)data).WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            // send message
            mMessenger.sendMessage(
                stringWriter.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
        }

        // *** IDisposable interface implementation ***

        public new void Dispose()
        {
            try
            {
                //mMessenger.stopMessaging();
            }
            catch
            {
            }
        }
    }
}