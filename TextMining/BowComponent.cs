using System;
using System.Text;
using System.Collections.Generic;
using Latino;
using Latino.Model;
using Latino.TextMining;
using Latino.Experimental.TextMining;
using Latino.Workflows;
using Latino.Workflows.TextMining;

namespace LatinoWorkflows.TextMining
{
    // TODO: move BowRecord into separate file
    public class BowRecord
    {
        private Guid mCorpusId;
        private Guid mDocId;

        private SparseVector<double>.ReadOnly mBowVec;

        private DateTime mTime;

        public BowRecord(Guid corpusId, Guid docId, SparseVector<double>.ReadOnly bowVec, DateTime time)
        {
            mCorpusId = corpusId;
            mDocId = docId;
            mBowVec = bowVec;
            mTime = time;
        }

        public Guid CorpusId
        {
            get { return mCorpusId; }
        }

        public Guid DocId
        {
            get { return mDocId; }
        }

        public SparseVector<double>.ReadOnly BowVec
        {
            get { return mBowVec; }
        }

        public DateTime Time
        {
            get { return mTime; }
        }
    }

    // TODO: move BowDelta into separate file
    public class BowDelta
    {
        private Set<int> mRemovedWords;
        private Set<KeyDat<int, Word>> mUpdatedWords;

        private ArrayList<BowRecord> mOutdatedRecords;
        private ArrayList<BowRecord> mNewRecords;

        public BowDelta(ArrayList<BowRecord> outdatedRecords, ArrayList<BowRecord> newRecords, Set<int> removedWords, Set<KeyDat<int, Word>> updatedWords)
        {
            mOutdatedRecords = outdatedRecords;
            mNewRecords = newRecords;
            mRemovedWords = removedWords;
            mUpdatedWords = updatedWords;
        }

        public ArrayList<BowRecord> OutdatedRecords
        {
            get { return mOutdatedRecords; }
        }

        public ArrayList<BowRecord> NewRecords
        {
            get { return mNewRecords; }
        }

        public Set<int> RemovedWords
        {
            get { return mRemovedWords; }
        }

        public Set<KeyDat<int, Word>> UpdatedWords
        { 
            get { return mUpdatedWords; }
        }
    }

    public class BowComponent : DocumentProcessor
    {
        private IncrementalBowSpace mBowSpace
            = new IncrementalBowSpace();
        private Queue<BowRecord> mQueue
            = new Queue<BowRecord>();

        public BowComponent() : base(typeof(BowComponent))
        {
            mBlockSelector = "TextBlock";
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            DocumentCorpus corpus = (DocumentCorpus)data;
            LabeledDataset<string, SparseVector<double>> bows = null;
            try
            {
                bows = new LabeledDataset<string, SparseVector<double>>();
                foreach (Document document in corpus.Documents)
                {
                    try
                    {
                        string contentType = document.Features.GetFeatureValue("contentType");
                        if (contentType != "Text") { continue; }
                        StringBuilder txt = new StringBuilder();
                        foreach (TextBlock tb in document.GetAnnotatedBlocks(mBlockSelector))
                        {
                            txt.AppendLine(tb.Text);
                        }
                        LabeledExample<string, SparseVector<double>> lblEx;
                        lock (mBowSpace.Words)
                        {
                            // TODO: dequeue
                            // TODO: create doc ref string
                            lblEx = new LabeledExample<string, SparseVector<double>>("doc_ref", mBowSpace.Enqueue(new string[] { txt.ToString() })[0]);
                        }
                        bows.Add(lblEx);
                    }
                    catch (Exception exception)
                    {
                        mLogger.Error("ProcessData (ProcessDocument)", exception);
                    }
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessData", exception);
            }
            return new Pair<ArrayList<Word>.ReadOnly, LabeledDataset<string, SparseVector<double>>>(mBowSpace.Words, bows);
        }
    }
}
