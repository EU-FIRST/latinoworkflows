/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentProcessor.cs
 *  Desc:    Document processor base 
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentProcessor
       |
       '-----------------------------------------------------------------------
    */
    public abstract class DocumentProcessor : StreamDataProcessor
    {
        protected override object ProcessData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            DocumentCorpus corpus = (DocumentCorpus)data;
            foreach (Document document in corpus.Documents)
            {
                ProcessDocument(document);
            }
            return corpus;
        }

        protected abstract void ProcessDocument(Document document);
    }
}
