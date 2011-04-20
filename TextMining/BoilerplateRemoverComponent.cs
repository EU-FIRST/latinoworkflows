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
using Latino.TextMining;

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
            if (contentType != null && contentType != "Html") { return; } // TODO: handle XHTML as well
            try
            {
                StringBuilder tokens = new StringBuilder();
                HtmlTokenizerHap tok = new HtmlTokenizerHap(document.Text);
                tok.Normalize = false;
                int numTags = 0;
                foreach (string token in tok)
                {
                    numTags++;
                }
                //Guid docGuid = Guid.NewGuid();
                //File.WriteAllText(@"c:\ex\ok\" + docGuid + ".tok", tokens.ToString());
                //document.Features.SetFeatureValue("_contentType", "Text");
            }
            catch (Exception e)
            {
                Guid docGuid = Guid.NewGuid();
                mLogger.Error("ProcessDocument", "hapfail {0}", docGuid);
                File.WriteAllText(@"c:\ex\fail\" + docGuid + ".html", document.Text);
                mLogger.Error("ProcessDocument", e);
            }
        }
    }
}