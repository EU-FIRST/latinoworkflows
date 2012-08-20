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
using System.IO;
using System.Text;
using System.Collections.Generic;
using Latino.Model;
using Latino.TextMining;
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
        private BowSpace mBowSpace;
        private Dictionary<string, IModel<string>> mModel;
        private double mTolerance 
            = 0.9;

        public DocumentCategorizerComponent(BinarySerializer modelReader) : base(typeof(DocumentCategorizerComponent))
        {
            mBlockSelector = "TextBlock";
            LoadModel(modelReader);
        }

        public DocumentCategorizerComponent(string modelFileName) : base(typeof(DocumentCategorizerComponent))
        {
            mBlockSelector = "TextBlock";
            BinarySerializer reader = new BinarySerializer(modelFileName, FileMode.Open);
            LoadModel(reader);
        }

        public double Tolerance // decrease this to get more categories
        {
            get { return mTolerance; }
            set { mTolerance = value; }
        }

        private void Categorize(string prefix, double thresh, SparseVector<double> vec, ArrayList<string> categories)
        {
            if (!mModel.ContainsKey(prefix))
            {
                categories.Add(prefix);
                return;
            }
            IModel<string> classifier = mModel[prefix];
            Prediction<string> p = ((IModel<string>)classifier).Predict(ModelUtils.ConvertExample(vec, classifier.RequiredExampleType));
            double maxSim = p.Count == 0 ? 0 : p.BestScore;
            foreach (KeyDat<double, string> item in p)
            {
                if (item.Key == 0) { break; }
                double score = item.Key / maxSim;
                if (score < thresh) { break; }
                Categorize(prefix + item.Dat + '/', thresh, vec, categories);
            }
        }

        private void LoadModel(BinarySerializer reader)
        {
            mLogger.Info("LoadModel", "Loading model ...");
            mBowSpace = new BowSpace(reader);
            mModel = Utils.LoadDictionary<string, IModel<string>>(reader);
            mLogger.Info("LoadModel", "Model successfully loaded.");
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
                ArrayList<string> categories = new ArrayList<string>();
                Categorize(/*prefix=*/"", mTolerance, bow, categories);
                document.Features.SetFeatureValue("NumCategories", categories.Count.ToString());
                for (int i = 0; i < categories.Count; i++)
                { 
                    document.Features.SetFeatureValue("Category" + i, categories[i]);
                }
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}