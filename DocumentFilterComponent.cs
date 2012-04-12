/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentFilterComponent.cs
 *  Desc:    Generic document filter component
 *  Created: Apr-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.WebMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentFilterComponent
       |
       '-----------------------------------------------------------------------
    */
    public class DocumentFilterComponent : StreamDataProcessor
    {
        public delegate bool FilterDocumentHandler(Document document, Logger logger);

        public event FilterDocumentHandler OnFilterDocument
            = null;

        private bool mCloneDumpOnFork // TODO: make configurable
            = false;
        private DispatchPolicy mDumpDispatchPolicy // TODO: make configurable
            = DispatchPolicy.ToAll;
        private Set<IDataConsumer> mDumpDataConsumers
            = new Set<IDataConsumer>();

        public DocumentFilterComponent() : base(typeof(DocumentFilterComponent))
        {
        }

        public void SubscribeDumpConsumer(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            mDumpDataConsumers.Add(dataConsumer);
        }

        public void UnsubscribeDumpConsumer(IDataConsumer dataConsumer)
        {
            Utils.ThrowException(dataConsumer == null ? new ArgumentNullException("dataConsumer") : null);
            mDumpDataConsumers.Remove(dataConsumer);
        }

        public Set<IDataConsumer>.ReadOnly SubscribedDumpConsumers
        {
            get { return mDumpDataConsumers; }
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            try
            {
                DocumentCorpus corpus = (DocumentCorpus)data;
                DocumentCorpus filteredCorpus = new DocumentCorpus();
                DocumentCorpus dumpCorpus = new DocumentCorpus();
                filteredCorpus.CopyFeaturesFrom(corpus);
                dumpCorpus.CopyFeaturesFrom(corpus);
                ArrayList<Document> dumpDocumentList = new ArrayList<Document>();
                foreach (Document document in corpus.Documents)
                {                    
                    try
                    {
                        if (OnFilterDocument != null)
                        {
                            if (!OnFilterDocument(document, mLogger)) 
                            { 
                                dumpDocumentList.Add(document); 
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        mLogger.Error("ProcessDocument", exception);
                    }                    
                }
                foreach (Document doc in dumpDocumentList)
                {
                    corpus.Remove(doc);
                    dumpCorpus.AddDocument(doc);
                }
                if (dumpCorpus.Documents.Count > 0)
                {
                    WorkflowUtils.DispatchData(this, dumpCorpus, mCloneDumpOnFork, mDumpDispatchPolicy, mDumpDataConsumers, mLogger);
                }
                return corpus.Documents.Count > 0 ? corpus : null;
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessData", exception);
                return data;
            }
        }
    }
}
