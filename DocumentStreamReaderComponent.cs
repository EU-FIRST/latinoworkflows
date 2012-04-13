/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentStreamReaderComponent.cs
 *  Desc:    Document stream reader component
 *  Created: Apr-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.WebMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentStreamReaderComponent
       |
       '-----------------------------------------------------------------------
    */
    public class DocumentStreamReaderComponent : StreamDataProducerPoll
    {
        private string[] mDataDirs;
        private string[] mFiles
            = null;
        private int mCurrentDirIdx
            = 0;
        private int mCurrentFileIdx
            = 0;

        public DocumentStreamReaderComponent(string rootPath) : base(typeof(DocumentStreamReaderComponent))
        {
            TimeBetweenPolls = 1;
            // collect data directories
            mDataDirs = Directory.GetDirectories(rootPath, "*.*", SearchOption.AllDirectories);
            Array.Sort(mDataDirs);
        }

        protected override object ProduceData()
        {
            // are we done?
            if (mCurrentDirIdx >= mDataDirs.Length)
            {
                Stop();
                return null;
            }
            // do we need to get more files?
            if (mFiles == null)
            {
                mFiles = Directory.GetFiles(mDataDirs[mCurrentDirIdx], "*.xml");
                Array.Sort(mFiles);
            }
            // did we process all currently available files?
            if (mCurrentFileIdx >= mFiles.Length)
            {
                mFiles = null;
                mCurrentFileIdx = 0;
                mCurrentDirIdx++;
                return null;
            }
            // read next file
            mLogger.Info("ProduceData", "Reading " + mFiles[mCurrentFileIdx] + " ...");
            DocumentCorpus corpus = new DocumentCorpus();
            StreamReader reader = new StreamReader(mFiles[mCurrentFileIdx]);
            corpus.ReadXml(new XmlTextReader(reader));
            reader.Close();
            // remove underscores in feature names
            string[] tmp = new string[corpus.Features.Names.Count];
            corpus.Features.Names.CopyTo(tmp, /*index=*/0);
            foreach (string featureName in tmp)
            {
                if (featureName.StartsWith("_"))
                {
                    corpus.Features.SetFeatureValue(featureName.TrimStart('_'), corpus.Features.GetFeatureValue(featureName));
                    corpus.Features.RemoveFeature(featureName);
                }
            }
            foreach (Document doc in corpus.Documents)
            {
                // remove annotations
                doc.ClearAnnotations();
                // remove underscores in feature names
                tmp = new string[doc.Features.Names.Count];
                doc.Features.Names.CopyTo(tmp, /*index=*/0);
                foreach (string featureName in tmp)
                {
                    if (featureName.StartsWith("_"))
                    {
                        doc.Features.SetFeatureValue(featureName.TrimStart('_'), doc.Features.GetFeatureValue(featureName));
                        doc.Features.RemoveFeature(featureName);
                    }
                }
                // remove processing-specific features
                foreach (string featureName in new string[] { 
                    "detectedLanguage", 
                    "detectedCharRange", 
                    "bprHeuristicsType", 
                    "domainName", 
                    "urlKey" })
                {
                    doc.Features.RemoveFeature(featureName);
                }
                // if there's raw data available, reset the content
                string raw = doc.Features.GetFeatureValue("raw");
                if (raw != null)
                {
                    doc.Features.SetFeatureValue("contentType", "Html");
                    doc.Text = Encoding.GetEncoding(doc.Features.GetFeatureValue("charSet")).GetString(Convert.FromBase64String(raw));
                }
            }
            mCurrentFileIdx++;
            while (WorkflowUtils.GetBranchLoadMax(this) > 10) // I'm giving it all she's got, Captain!
            {
                Thread.Sleep(1000);
            }            
            return corpus;
        }
    }
}
