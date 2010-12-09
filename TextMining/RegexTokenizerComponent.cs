/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    RegexTokenizerComponent.cs
 *  Desc:    Regex-based tokenizer component
 *  Created: Dec-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using Latino.TextMining;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class RegexTokenizerComponent
       |
       '-----------------------------------------------------------------------
    */
    public abstract class RegexTokenizerComponent : DocumentProcessor
    {
        private RegexTokenizer mTokenizer
            = new RegexTokenizer();

        protected override void ProcessDocument(Document document)
        {
            // ...
        }
    }
}