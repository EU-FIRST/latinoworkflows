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
        public BoilerplateRemoverComponent() : base(typeof(BoilerplateRemoverComponent))
        {
        }

        protected override void ProcessDocument(Document document)
        {
            string contentType = document.Features.GetFeatureValue("_contentType");
            if (contentType != "Html") { return; } 
            try
            {
                BoilerplateRemover br = BoilerplateRemover.GetDefaultBoilerplateRemover();
                List<BoilerplateRemover.HtmlBlock> blocks;
                br.ExtractText(new StringReader(document.Text), BoilerplateRemover.TextClass.Unknown, out blocks);
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
                document.Features.SetFeatureValue("_contentType", "Text");
            }
            catch (Exception exception)
            {
                mLogger.Error("ProcessDocument", exception);
            }
        }
    }
}