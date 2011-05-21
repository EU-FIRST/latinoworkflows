/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    BoilerplateRemoverComponent.cs
 *  Desc:    Boilerplate remover component 
 *  Created: Apr-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Latino.WebMining;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class BoilerplateRemoverComponent
       |
       '-----------------------------------------------------------------------
    */
    public class BoilerplateRemoverComponent : DocumentProcessor
    {
        public BoilerplateRemoverComponent() : base(typeof(BoilerplateRemoverComponent).ToString())
        {
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("_contentType");
            if (contentType != "Html") { return; } // *** currently handles only documents that have _contentType set to Html
            try
            {
                BoilerplateRemover br = BoilerplateRemover.GetDefaultBoilerplateRemover();
                List<BoilerplateRemover.HtmlBlock> blocks;
                br.ExtractText(new StringReader(document.Text), BoilerplateRemover.TextClass.Unknown, out blocks);
                StringBuilder text = new StringBuilder();
                ArrayList<Annotation> tmp = new ArrayList<Annotation>();
                foreach (BoilerplateRemover.HtmlBlock block in blocks)
                {
                    int spanStart = text.Length;
                    string blockTxt = block.text;
                    if (blockTxt != null && blockTxt.Length > 0)
                    {
                        Annotation annot = new Annotation(spanStart, spanStart + (blockTxt.Length - 1), "Block/" + block.textClass.ToString());
                        tmp.Add(annot);
                        text.AppendLine(blockTxt);
                        text.AppendLine();
                    }
                }
                document.Text = text.ToString().TrimEnd();
                foreach (Annotation annot in tmp) { document.AddAnnotation(annot); }
            }
            catch (Exception e)
            {
                mLogger.Error("ProcessDocument", e);
            }
        }
    }
}