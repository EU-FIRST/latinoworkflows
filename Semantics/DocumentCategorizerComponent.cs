/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    DocumentCategorizerComponent.cs
 *  Desc:    Document categorizer component
 *  Created: Jun-2012
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Latino.TextMining;
using Latino.Model;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Semantics
{
    /* .-----------------------------------------------------------------------
       |
       |  Class DocumentCategorizerComponent
       |
       '-----------------------------------------------------------------------
    */
    public class DocumentCategorizerComponent : DocumentProcessor
    {
        private Dictionary<string, ArrayList<string>> mSymbolicLinks
            = new Dictionary<string, ArrayList<string>>();
        private BowSpace mBowSpace;
        private SparseMatrix<double> mCentroidsTr
            = new SparseMatrix<double>();
        private ArrayList<string> mLabels
            = new ArrayList<string>();
        private ArrayList<int> mClusterSize
            = new ArrayList<int>();

        public DocumentCategorizerComponent(BinarySerializer taxonomyReader) : base(typeof(DocumentCategorizerComponent))
        {
            mBlockSelector = "TextBlock";
            LoadTaxonomy(taxonomyReader);
        }

        public DocumentCategorizerComponent(string taxonomyFileName) : base(typeof(DocumentCategorizerComponent))
        {
            mBlockSelector = "TextBlock";
            BinarySerializer reader = new BinarySerializer(taxonomyFileName, FileMode.Open);
            LoadTaxonomy(reader);
        }

        private void LoadTaxonomy(BinarySerializer reader)
        {
            mLogger.Info("LoadTaxonomy", "Loading taxonomy ...");
            // load data
            mBowSpace = new BowSpace(reader);
            mBowSpace.CutLowWeightsPerc = 0;
            Cluster root = new Cluster(reader);
            ArrayList<Pair<string, string>> symLinks = new ArrayList<Pair<string, string>>(reader);
            // create nearest centroid classifier
            int rowIdx = 0;
            ProcessTaxonomy(root, ref rowIdx);
            mCentroidsTr = mCentroidsTr.GetTransposedCopy();
            // process symbolic links
            foreach (Pair<string, string> symLink in symLinks)
            {
                if (!mSymbolicLinks.ContainsKey(symLink.Second)) { mSymbolicLinks.Add(symLink.Second, new ArrayList<string>()); }
                mSymbolicLinks[symLink.Second].Add(symLink.First);
            }
            mLogger.Info("LoadTaxonomy", "Taxonomy loaded.");
        }

        private void ProcessTaxonomy(Cluster cluster, ref int rowIdx)
        {
            Pair<string, SparseVector<double>> clusterInfo = (Pair<string, SparseVector<double>>)cluster.ClusterInfo;
            mCentroidsTr[rowIdx++] = clusterInfo.Second;
            mLabels.Add(clusterInfo.First);
            mClusterSize.Add(cluster.Items.Count);
            //Console.WriteLine(mLabels.Last);
            foreach (Cluster child in cluster.Children)
            {
                ProcessTaxonomy(child, ref rowIdx);
            }
        }

        public override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Text") { return; }
            try
            {
                StringBuilder txt = new StringBuilder();
                foreach (TextBlock tb in document.GetAnnotatedBlocks(mBlockSelector))
                {
                    txt.AppendLine(tb.Text);
                }
                SparseVector<double> bow = mBowSpace.ProcessDocument(txt.ToString());
                double[] simVec = ModelUtils.GetDotProductSimilarity(mCentroidsTr, mLabels.Count, bow);
                ArrayList<KeyDat<double, string>> tmp = new ArrayList<KeyDat<double, string>>();
                for (int i = 0; i < mLabels.Count; i++)
                {
                    tmp.Add(new KeyDat<double, string>(simVec[i], mLabels[i] + " " + mClusterSize[i]));
                }
                tmp.Sort(DescSort<KeyDat<double, string>>.Instance);
                int n = 0;
                foreach (KeyDat<double, string> item in tmp)
                {
                    //if (item.Second.Split('/').Length <= 4)
                    {
                        Console.WriteLine(item);
                        if (++n > 10) { break; }
                    }
                }
                Console.WriteLine();
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}