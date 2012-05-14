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
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private class FileNameComparer : IComparer<string>
        {
            private static Regex mDigitRegex
                = new Regex(@"\\(\d(\\|$))", RegexOptions.Compiled);

            public int Compare(string x, string y)
            {
                while (mDigitRegex.Match(x).Success) { x = mDigitRegex.Replace(x, @"\0$1"); }
                while (mDigitRegex.Match(y).Success) { y = mDigitRegex.Replace(y, @"\0$1"); }
                return x.CompareTo(y);
            }
        }

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
            Array.Sort(mDataDirs, new FileNameComparer());
            //foreach (string dir in mDataDirs) { Console.WriteLine(dir); }
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
            string fileName = new FileInfo(mFiles[mCurrentFileIdx]).Name;
            string corpusId = new Guid(fileName.Split('_', '.')[3]).ToString();
            corpus.Features.SetFeatureValue("guid", corpusId);
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
