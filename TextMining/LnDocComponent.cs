/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    LnDocComponent.cs
 *  Desc:    Converts array of [named] line-documents to DocumentCorpus
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
       |  Class LnDocComponent
       |
       '-----------------------------------------------------------------------
    */
    public class LnDocComponent : StreamDataProcessor
    {
        private bool mIsNamedDoc
            = true;

        public bool IsNamedDoc
        {
            get { return mIsNamedDoc; }
            set { mIsNamedDoc = value; }
        }

        protected override object ProcessData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is string[]) ? new ArgumentTypeException("data") : null);
            DateTime timeStart = DateTime.Now;
            DocumentCorpus corpus = new DocumentCorpus();
            foreach (string line in (string[])data)
            {
                int splitIdx = line.IndexOfAny(new char[] { ' ', '\t', '\n' });
                Document doc;
                if (!mIsNamedDoc || splitIdx < 0)
                {
                    doc = new Document("", line.Trim());
                }
                else
                {
                    doc = new Document(line.Substring(0, splitIdx).Trim(), line.Substring(splitIdx).Trim());
                }
                doc.Features.SetFeatureValue("_time", DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));
                corpus.AddDocument(doc);
            }
            corpus.Features.SetFeatureValue("_provider", GetType().ToString());
            corpus.Features.SetFeatureValue("_isNamedDoc", mIsNamedDoc.ToString());
            corpus.Features.SetFeatureValue("_timeStart", timeStart.ToString(Utils.DATE_TIME_SIMPLE));
            corpus.Features.SetFeatureValue("_timeEnd", DateTime.Now.ToString(Utils.DATE_TIME_SIMPLE));            
            return corpus;
        }
    }
}
