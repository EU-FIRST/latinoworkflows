/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentProcessor.cs
 *  Desc:    Document processor base class
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text.RegularExpressions;

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
        protected string mBlockSelector
            = ".*";

        public DocumentProcessor(string loggerName) : base(loggerName)
        { 
        }

        public DocumentProcessor(Type loggerType) : this(loggerType.ToString())
        { 
        }

        public string BlockSelector
        {
            get { return mBlockSelector; }
            set 
            {
                Utils.ThrowException(value == null ? new ArgumentNullException("BlockSelector") : null);
                mBlockSelector = value; 
            }
        }

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
