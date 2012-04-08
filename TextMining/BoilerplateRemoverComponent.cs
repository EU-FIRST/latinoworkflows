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
using Latino.Workflows.TextMining;

namespace Latino.Workflows.WebMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class BoilerplateRemoverComponent
       |
       '-----------------------------------------------------------------------
    */
    public class BoilerplateRemoverComponent : DocumentProcessor
    {
        private BoilerplateRemover mBoilerplateRemover
            = BoilerplateRemover.GetDefaultBoilerplateRemover();

        public BoilerplateRemoverComponent() : base(typeof(BoilerplateRemoverComponent))
        {
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("contentType");
            if (contentType != "Html") { return; } 
            try
            {
                List<BoilerplateRemover.HtmlBlock> blocks;
                mBoilerplateRemover.ExtractText(new StringReader(document.Text), BoilerplateRemover.TextClass.Unknown, out blocks);
                StringBuilder text = new StringBuilder();
                foreach (BoilerplateRemover.HtmlBlock block in blocks)
                {
                    int spanStart = text.Length;
                    string blockTxt = block.text;
                    if (blockTxt != null && blockTxt.Length > 0)
                    {
                        document.AddAnnotation(new Annotation(spanStart, spanStart + (blockTxt.Length - 1), "TextBlock/" + block.textClass.ToString()));
                        text.AppendLine(blockTxt);
                    }
                }
                document.Text = text.ToString();
                document.Features.SetFeatureValue("contentType", "Text");
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}