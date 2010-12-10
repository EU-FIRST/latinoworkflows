/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentConsumer.cs
 *  Desc:    Document consumer base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentConsumer
       |
       '-----------------------------------------------------------------------
    */
    public abstract class DocumentConsumer : StreamDataConsumer
    {
        protected override void ConsumeData(IDataProducer sender, object data)
        {
            DocumentCorpus corpus = (DocumentCorpus)data;
            foreach (Document document in corpus.Documents)
            {
                ConsumeDocument(document);
            }
        }

        protected abstract void ConsumeDocument(Document document);
    }
}
