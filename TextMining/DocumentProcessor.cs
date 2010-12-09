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
        protected override object ProcessData(object data)
        {
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
