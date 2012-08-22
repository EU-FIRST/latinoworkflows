/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    EntityRecognitionEngine.cs
 *  Desc:    Simple ontology-based entity recognition engine
 *  Created: Nov-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Latino.TextMining;
using SemWeb;
using System.IO;

namespace Latino.Workflows.Semantics
{
    public class EntityRecognitionEngine
    {
        private MemoryStore mRdfStore
            = new MemoryStore();
        private Dictionary<string, Gazetteer> mGazetteers
            = new Dictionary<string, Gazetteer>();
        private Logger mLogger
            = Logger.GetLogger(typeof(EntityRecognitionEngine));

        private string mDefaultClassUri
            = "http://www.w3.org/2002/07/owl#Thing"; 

        private static IStemmer mLemmatizer // *** make configurable?
            = new Lemmatizer(Language.English);

        private static Regex mMicroTokenRegex
            = new Regex(@"[\d\p{L}]+", RegexOptions.Compiled); // *** currency symbols, punctuation marks?
        private static Regex mGazetteerMicroTokenRegex
            = new Regex(@"[\d\p{L}]+(/\p{L}+)?", RegexOptions.Compiled); // *** currency symbols, punctuation marks?
        private static Regex mConstraintRegex
            = new Regex(@"(/\p{L}+=[\p{L}\d]+)+", RegexOptions.Compiled);

        private const string NAMESPACE
            = "http://project-first.eu/ontology#";
        private static Entity C_GAZETTEER
            = NAMESPACE + "Gazetteer";
        private static Entity P_TERM
            = NAMESPACE + "term";
        private static Entity P_STOP_WORD
            = NAMESPACE + "stopWord";
        private static Entity P_IMPORTS
            = NAMESPACE + "imports";
        private static Entity P_HAS_SENTENCE_LEVEL_CONDITION
            = NAMESPACE + "hasSentenceLevelCondition";
        private static Entity P_HAS_BLOCK_LEVEL_CONDITION
            = NAMESPACE + "hasBlockLevelCondition";
        private static Entity P_HAS_DOCUMENT_LEVEL_CONDITION
            = NAMESPACE + "hasDocumentLevelCondition";
        private static Entity P_IDENTIFIED_BY
            = NAMESPACE + "identifiedBy";
        private static Entity P_SETTINGS
            = NAMESPACE + "settings";
        private static Entity P_COMMENT
            = "http://www.w3.org/2000/01/rdf-schema#comment";
        private static Entity P_TYPE
            = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        private static Entity P_SUBCLASS_OF
            = "http://www.w3.org/2000/01/rdf-schema#subClassOf";
        private static Entity P_LABEL
            = "http://www.w3.org/2000/01/rdf-schema#label";

        // *** Document *** 

        /* .-----------------------------------------------------------------------
           |
           |  Class Token
           |
           '-----------------------------------------------------------------------
        */
        private class Token
        {
            public string mTokenStr;
            public int mSpanStart;
            public int mSpanEnd;
            public string mPosTag;
            public string mLemma;

            public Token(string tokenStr, string posTag, string lemma, int spanStart, int spanEnd)
            {
                mTokenStr = tokenStr;
                mPosTag = posTag;
                mLemma = lemma;
                mSpanStart = spanStart;
                mSpanEnd = spanEnd;
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class Condition
           |
           '-----------------------------------------------------------------------
        */
        private class Condition
        {
            public enum Level
            { 
                Sentence,
                Block,
                Document
            }

            public Gazetteer mGazetteer;
            public Level mLevel;

            public Condition(Gazetteer gazetteer, Level level)
            {
                mGazetteer = gazetteer;
                mLevel = level;
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class Sentence
           |
           '-----------------------------------------------------------------------
        */
        private class Sentence
        {                       
            public TextBlock mTextBlock; 
            public ArrayList<Token> mTokens 
                = new ArrayList<Token>();              

            public Sentence(IEnumerable<string> tokens, IEnumerable<int> spanInfo, IEnumerable<string> posTags, TextBlock textBlock)
            {
                mTextBlock = textBlock;
                // create micro-tokens 
                IEnumerator<string> enumTokens = tokens.GetEnumerator();
                IEnumerator<string> enumPosTags = posTags.GetEnumerator();
                IEnumerator<int> enumSpanInfo = spanInfo.GetEnumerator();
                while (enumTokens.MoveNext() && enumPosTags.MoveNext() && enumSpanInfo.MoveNext())
                {
                    Match m = mMicroTokenRegex.Match(enumTokens.Current);
                    while (m.Success)
                    {
                        // lemmatize micro-token, inherit POS tag
                        int spanStart = enumSpanInfo.Current + m.Index;
                        int spanEnd = spanStart + m.Value.Length - 1;
                        string token = Normalize(m.Value);
                        string lemma = mLemmatizer.GetStem(token);
                        Token microToken = new Token(token, enumPosTags.Current, lemma, spanStart, spanEnd);
                        mTokens.Add(microToken);
                        m = m.NextMatch();
                    }
                }
            }

            private bool Match(GazetteerToken gazToken, Token docToken, CaseMatchingType caseMatchingType, bool firstToken)
            {
                // check POS tag
                if (gazToken.mPosConstraint != null && !docToken.mPosTag.StartsWith(gazToken.mPosConstraint)) { return false; }
                // check word or lemma
                string gazTokenStr;
                string docTokenStr;
                if (gazToken.mLemma == null)
                {
                    gazTokenStr = gazToken.mTokenStr;
                    docTokenStr = docToken.mTokenStr;
                }
                else
                {
                    gazTokenStr = gazToken.mLemma;
                    docTokenStr = docToken.mLemma;
                }
                switch (caseMatchingType)
                { 
                    case CaseMatchingType.IgnoreCase:
                        return string.Compare(gazTokenStr, docTokenStr, StringComparison.OrdinalIgnoreCase) == 0;
                    case CaseMatchingType.ExactMatch:
                    case CaseMatchingType.AllLowercase:
                    case CaseMatchingType.AllUppercase:
                    case CaseMatchingType.AllCapsStrict:
                    case CaseMatchingType.InitCapStrict:
                        return gazTokenStr == docTokenStr;
                    case CaseMatchingType.InitCapLoose:
                        return (!firstToken && string.Compare(gazTokenStr, docTokenStr, StringComparison.OrdinalIgnoreCase) == 0)
                            || (firstToken && char.IsUpper(docTokenStr[0]) && string.Compare(gazTokenStr, docTokenStr, StringComparison.OrdinalIgnoreCase) == 0);
                    case CaseMatchingType.AllCapsLoose:
                        return char.IsUpper(docTokenStr[0]) && string.Compare(gazTokenStr, docTokenStr, StringComparison.OrdinalIgnoreCase) == 0;
                    default:
                        throw new ArgumentValueException("caseMatchingType");
                }
            }

            public void Match(Gazetteer gazetteer, out ArrayList<Pair<int, int>> spans)
            {
                spans = new ArrayList<Pair<int, int>>();
                foreach (GazetteerTerm term in gazetteer.mTerms)
                {
                    if (!term.mEnabled) { continue; }
                    int lastIdx = mTokens.Count - term.mTokens.Count;
                    for (int i = 0; i <= lastIdx; i++)
                    {
                        int j = i;
                        bool found = false;
                        for (int k = 0; k < term.mTokens.Count; k++)
                        {
                            if (!Match(term.mTokens[k], mTokens[j], term.mCaseMatchingType, /*firstToken=*/k == 0)) { break; }
                            if (found = k == term.mTokens.Count - 1) { break; }
                            j++;
                            while (j < mTokens.Count && gazetteer.IsStopWord(mTokens[j].mTokenStr.ToLower())) { j++; }
                            if (j >= mTokens.Count) { break; }
                        }
                        if (found) // gazetteer term found (starting at micro-token i, ending at micro-token j)
                        {
                            int len = mTokens[j].mSpanEnd - mTokens[i].mSpanStart + 1; // *** this counts all chars in the annotation (incl. spaces and non-token chars)
                            if (len >= term.mMinLen)
                            {
                                spans.Add(new Pair<int, int>(mTokens[i].mSpanStart, mTokens[j].mSpanEnd));
                            }
                        }
                    }
                }
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class TextBlock
           |
           '-----------------------------------------------------------------------
        */
        private class TextBlock
        {
            public Document mDocument; 
            public ArrayList<Sentence> mSentences
                = new ArrayList<Sentence>();

            public TextBlock(Document document)
            {
                mDocument = document;
            }

            public void AddSentence(IEnumerable<string> tokens, IEnumerable<int> spanInfo, IEnumerable<string> posTags)
            {
                mSentences.Add(new Sentence(tokens, spanInfo, posTags, /*textBlock=*/this));
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class Document
           |
           '-----------------------------------------------------------------------
        */
        public class Document
        {
            private ArrayList<TextBlock> mTextBlocks
                = new ArrayList<TextBlock>();

            public void BeginNewTextBlock()
            {
                mTextBlocks.Add(new TextBlock(/*document=*/this));
            }

            public void AddSentence(IEnumerable<string> tokens, IEnumerable<int> spanInfo, IEnumerable<string> posTags)
            {
                if (mTextBlocks.Count == 0) { BeginNewTextBlock(); }
                mTextBlocks.Last.AddSentence(tokens, spanInfo, posTags);
            }

            public ArrayList<string> DiscoverEntities(EntityRecognitionEngine e, out ArrayList<Pair<int, int>> spans)
            {
                Dictionary<Sentence, Dictionary<Gazetteer, ArrayList<Pair<int, int>>>> sentenceEntityInfo
                    = new Dictionary<Sentence, Dictionary<Gazetteer, ArrayList<Pair<int, int>>>>();
                Dictionary<TextBlock, Set<Gazetteer>> textBlockEntityInfo
                    = new Dictionary<TextBlock, Set<Gazetteer>>();
                Set<Gazetteer> documentEntityInfo
                    = new Set<Gazetteer>();
                ArrayList<Pair<int, int>> sentenceSpans = new ArrayList<Pair<int, int>>();                
                // look for gazetteer terms 
                foreach (KeyValuePair<string, Gazetteer> gazetteer in e.mGazetteers)
                {
                    foreach (TextBlock textBlock in mTextBlocks)
                    { 
                        foreach (Sentence sentence in textBlock.mSentences)
                        {
                            sentence.Match(gazetteer.Value, out sentenceSpans); 
                            if (sentenceSpans.Count > 0)
                            {
                                Dictionary<Gazetteer, ArrayList<Pair<int, int>>> sentenceInfo;
                                if (sentenceEntityInfo.TryGetValue(sentence, out sentenceInfo))
                                {
                                    sentenceInfo.Add(gazetteer.Value, sentenceSpans);
                                }
                                else
                                {
                                    sentenceInfo = new Dictionary<Gazetteer, ArrayList<Pair<int, int>>>();
                                    sentenceInfo.Add(gazetteer.Value, sentenceSpans);
                                    sentenceEntityInfo.Add(sentence, sentenceInfo);
                                }
                            }
                        }
                    }
                }
                // propagate discovered entities
                foreach (KeyValuePair<Sentence, Dictionary<Gazetteer, ArrayList<Pair<int, int>>>> sentenceInfo in sentenceEntityInfo)
                {
                    foreach (KeyValuePair<Gazetteer, ArrayList<Pair<int, int>>> gazetteerInfo in sentenceInfo.Value)
                    {
                        documentEntityInfo.Add(gazetteerInfo.Key);
                        TextBlock textBlock = sentenceInfo.Key.mTextBlock;
                        Set<Gazetteer> textBlockInfo;
                        if (textBlockEntityInfo.TryGetValue(textBlock, out textBlockInfo))
                        {
                            textBlockInfo.Add(gazetteerInfo.Key);
                        }
                        else
                        {
                            textBlockInfo = new Set<Gazetteer>(new Gazetteer[] { gazetteerInfo.Key });
                            textBlockEntityInfo.Add(textBlock, textBlockInfo);
                        }
                    }
                }
                // check conditions
                spans = new ArrayList<Pair<int, int>>();
                ArrayList<string> discoveredEntities = new ArrayList<string>(); // gazetteer URIs
                foreach (KeyValuePair<Sentence, Dictionary<Gazetteer, ArrayList<Pair<int, int>>>> sentenceInfo in sentenceEntityInfo)
                {
                    foreach (KeyValuePair<Gazetteer, ArrayList<Pair<int, int>>> gazetteerInfo in sentenceInfo.Value)
                    {
                        Gazetteer gazetteer = gazetteerInfo.Key;
                        Set<Gazetteer> textBlockGazetteers = textBlockEntityInfo[sentenceInfo.Key.mTextBlock];
                        bool valid = true; 
                        foreach (Condition condition in gazetteer.mConditions)
                        {
                            if (condition.mLevel == Condition.Level.Document)
                            {
                                if (!documentEntityInfo.Contains(condition.mGazetteer)) { valid = false; break; }
                            }
                            else if (condition.mLevel == Condition.Level.Block)
                            {
                                if (!textBlockGazetteers.Contains(condition.mGazetteer)) { valid = false; break; }
                            }
                            else if (condition.mLevel == Condition.Level.Sentence)
                            {
                                if (!sentenceInfo.Value.ContainsKey(condition.mGazetteer)) { valid = false; break; }
                            }
                        }
                        if (valid)
                        {
                            for (int i = 0; i < gazetteerInfo.Value.Count; i++)
                            {
                                // check if inside another span
                                bool skip = false;
                                Pair<int, int> span = gazetteerInfo.Value[i];
                                foreach (KeyValuePair<Gazetteer, ArrayList<Pair<int, int>>> gazInfo in sentenceInfo.Value)
                                {
                                    foreach (Pair<int, int> otherSpan in gazInfo.Value)
                                    {
                                        if (span.First >= otherSpan.First && span.Second <= otherSpan.Second && span != otherSpan)
                                        {
                                            skip = true; 
                                            break;
                                        }
                                    }
                                    if (skip) { break; }
                                }
                                if (!skip)
                                {
                                    discoveredEntities.Add(gazetteer.mUri);
                                    spans.Add(span);
                                }
                            }
                        }
                    }
                }
                return discoveredEntities;
            }
        }

        // *** Gazetteer ***

        /* .-----------------------------------------------------------------------
           |
           |  Enum CaseMatchingType
           |
           '-----------------------------------------------------------------------
        */
        private enum CaseMatchingType
        {  
            IgnoreCase,
            ExactMatch,
            AllCapsStrict,
            AllCapsLoose,
            InitCapStrict,
            InitCapLoose,
            AllLowercase,
            AllUppercase
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class GazetteerToken
           |
           '-----------------------------------------------------------------------
        */
        private class GazetteerToken
        {
            public string mTokenStr;
            public string mPosConstraint; // constraint or null
            public string mLemma; // constraint or null

            public GazetteerToken(string tokenStr, string posConstraint, string lemma)
            {
                mTokenStr = tokenStr;
                mPosConstraint = posConstraint;
                mLemma = lemma;
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class GazetteerTerm
           |
           '-----------------------------------------------------------------------
        */
        private class GazetteerTerm
        {
            public ArrayList<GazetteerToken> mTokens
                = new ArrayList<GazetteerToken>();
            public CaseMatchingType mCaseMatchingType;
            public bool mEnabled;
            public int mMinLen;

            private void PrepareTokens(CaseMatchingType caseMatchingType, bool processLemmas)
            {
                switch (caseMatchingType)
                { 
                    case CaseMatchingType.AllLowercase:
                    case CaseMatchingType.IgnoreCase:
                        foreach (GazetteerToken token in mTokens) 
                        { 
                            token.mTokenStr = token.mTokenStr.ToLower();
                            if (processLemmas) { token.mLemma = token.mLemma.ToLower(); }
                        }
                        break;
                    case CaseMatchingType.InitCapStrict:
                    case CaseMatchingType.InitCapLoose:
                        foreach (GazetteerToken token in mTokens) 
                        { 
                            token.mTokenStr = token.mTokenStr.ToLower();
                            if (processLemmas) { token.mLemma = token.mLemma.ToLower(); }
                        }
                        mTokens[0].mTokenStr = char.ToUpper(mTokens[0].mTokenStr[0]) + mTokens[0].mTokenStr.Substring(1);
                        if (processLemmas) { mTokens[0].mLemma = char.ToUpper(mTokens[0].mLemma[0]) + mTokens[0].mLemma.Substring(1); }
                        break;
                    case CaseMatchingType.AllCapsStrict:
                    case CaseMatchingType.AllCapsLoose:
                        foreach (GazetteerToken token in mTokens) 
                        { 
                            token.mTokenStr = char.ToUpper(token.mTokenStr[0]) + token.mTokenStr.Substring(1).ToLower();
                            if (processLemmas) { token.mLemma = char.ToUpper(token.mLemma[0]) + token.mLemma.Substring(1).ToLower(); }
                        }
                        break;
                    case CaseMatchingType.AllUppercase:
                        foreach (GazetteerToken token in mTokens) 
                        { 
                            token.mTokenStr = token.mTokenStr.ToUpper();
                            if (processLemmas) { token.mLemma = token.mLemma.ToUpper(); }
                        }
                        break;
                }
            }

            private void InitializeInstance(IEnumerable<string> tokens, IEnumerable<string> posConstraints, bool lemmatize, CaseMatchingType caseMatchingType, bool enabled, int minLen, Gazetteer gazetteer)
            {
                mCaseMatchingType = caseMatchingType;
                mEnabled = enabled;
                mMinLen = minLen;
                IEnumerator<string> enumTokens = tokens.GetEnumerator();
                IEnumerator<string> enumPosConstraints = posConstraints.GetEnumerator();
                while (enumTokens.MoveNext() && enumPosConstraints.MoveNext())
                {
                    string tokenStr = Normalize(enumTokens.Current);
                    string posConstraint = enumPosConstraints.Current;
                    if (!gazetteer.IsStopWord(tokenStr.ToLower()))
                    {
                        string lemma = null;
                        if (lemmatize)
                        {
                            lemma = mLemmatizer.GetStem(tokenStr);
                            if (lemma == "") { lemma = tokenStr; } 
                        }
                        GazetteerToken token = new GazetteerToken(tokenStr, posConstraint, lemma);
                        mTokens.Add(token);
                    }
                }
                if (mTokens.Count > 0) 
                { 
                    PrepareTokens(caseMatchingType, lemmatize); 
                }
            }

            public GazetteerTerm(string termDef, Gazetteer gazetteer, CaseMatchingType defaultCaseMatchingType, bool defaultLemmatizeFlag, bool defaultEnabledFlag,
                int defaultMinLen)
            {
                // default settings
                CaseMatchingType caseMatchingType = defaultCaseMatchingType;
                bool lemmatize = defaultLemmatizeFlag;
                bool enabled = defaultEnabledFlag;
                int minLen = defaultMinLen;
                // parse term settings
                termDef = mConstraintRegex.Replace(termDef, new MatchEvaluator(delegate(Match m) {
                    ParseGazetteerSettings(m.Value, ref caseMatchingType, ref lemmatize, ref enabled, ref minLen);
                    return "";
                }));
                ArrayList<string> tokens = new ArrayList<string>();
                ArrayList<string> posConstraints = new ArrayList<string>();
                Match match = mGazetteerMicroTokenRegex.Match(termDef);
                while (match.Success)
                {
                    string token = match.Value;
                    string[] tokenParts = token.Split('/');
                    string posConstraint = null;
                    if (tokenParts.Length == 2)
                    {
                        token = tokenParts[0];
                        posConstraint = tokenParts[1];
                    }
                    tokens.Add(token);
                    posConstraints.Add(posConstraint);
                    match = match.NextMatch();
                }
                InitializeInstance(tokens, posConstraints, lemmatize, caseMatchingType, enabled, minLen, gazetteer);
            }

            public string GetLemma()
            {
                string lemmaTerm = "";
                foreach (GazetteerToken token in mTokens)
                {
                    lemmaTerm += "[" + token.mLemma + "] ";
                }
                return lemmaTerm.TrimEnd(' ');
            }

            public override string ToString()
            {
                string term = "";
                foreach (GazetteerToken token in mTokens)
                {
                    term += "[" + token.mTokenStr + "] ";
                }
                return term.TrimEnd(' ');
            }
        }

        /* .-----------------------------------------------------------------------
           |
           |  Class Gazetteer
           |
           '-----------------------------------------------------------------------
        */
        private class Gazetteer
        {
            public string mUri
                = null;
            public ArrayList<GazetteerTerm> mTerms
                = new ArrayList<GazetteerTerm>();
            public Set<string> mStopWords
                = new Set<string>();
            public ArrayList<Gazetteer> mImportedGazetteers
                = new ArrayList<Gazetteer>();
            public ArrayList<Condition> mConditions
                = new ArrayList<Condition>();
            public bool mEnabled
                = true;
            public int mMinLen
                = 1;

            public Gazetteer(string uri)
            {
                mUri = uri;
            }

            public void ReadStopWords(MemoryStore rdfStore)
            {
                Resource[] stopWords = rdfStore.SelectObjects(mUri, P_STOP_WORD);
                foreach (Literal word in stopWords)
                {
                    string stopWordStr = Normalize(word.Value).ToLower();
                    mStopWords.Add(stopWordStr);
                }
            }

            public bool IsStopWord(string word)
            {
                if (mStopWords.Contains(word)) { return true; }
                foreach (Gazetteer importedGazetteer in mImportedGazetteers)
                {
                    if (importedGazetteer.IsStopWord(word)) { return true; }
                }
                return false;
            }

            public void ImportGazetteers(MemoryStore rdfStore, Dictionary<string, Gazetteer> gazetteers)
            {
                Resource[] importedGazetteers = rdfStore.SelectObjects(mUri, P_IMPORTS);
                foreach (Entity importedGazetteer in importedGazetteers)
                {
                    mImportedGazetteers.Add(gazetteers[importedGazetteer.Uri]);
                }
            }

            public void ReadConditions(MemoryStore rdfStore, Dictionary<string, Gazetteer> gazetteers)
            {
                ArrayList<string> crumbs = new ArrayList<string>(new string[] { mUri });
                Entity[] objects = rdfStore.SelectSubjects(P_IDENTIFIED_BY, new Entity(mUri));
                if (objects.Length > 0)
                {
                    Resource[] objTypes = rdfStore.SelectObjects(objects[0].Uri, P_TYPE);
                    if (objTypes.Length > 0)
                    {
                        crumbs.Add(objTypes[0].Uri);
                        Resource[] superClass = rdfStore.SelectObjects((Entity)objTypes[0], P_SUBCLASS_OF);
                        while (superClass.Length > 0)
                        {
                            crumbs.Add(superClass[0].Uri);
                            superClass = rdfStore.SelectObjects((Entity)superClass[0], P_SUBCLASS_OF);
                        }
                    }
                }
                foreach (string uri in crumbs)
                {
                    Resource[] conditionGazetteers = rdfStore.SelectObjects(uri, P_HAS_SENTENCE_LEVEL_CONDITION);
                    foreach (Entity conditionGazetteer in conditionGazetteers)
                    {
                        mConditions.Add(new Condition(gazetteers[conditionGazetteer.Uri], Condition.Level.Sentence));
                    }
                    conditionGazetteers = rdfStore.SelectObjects(uri, P_HAS_BLOCK_LEVEL_CONDITION);
                    foreach (Entity conditionGazetteer in conditionGazetteers)
                    {
                        mConditions.Add(new Condition(gazetteers[conditionGazetteer.Uri], Condition.Level.Block));
                    }
                    conditionGazetteers = rdfStore.SelectObjects(uri, P_HAS_DOCUMENT_LEVEL_CONDITION);
                    foreach (Entity conditionGazetteer in conditionGazetteers)
                    {
                        mConditions.Add(new Condition(gazetteers[conditionGazetteer.Uri], Condition.Level.Document));
                    }
                }
            }

            private void ReadGazetteerSettings(MemoryStore rdfStore, out CaseMatchingType caseMatchingType, out bool lemmatize, out bool enabled,
                out int minLen)
            {
                caseMatchingType = CaseMatchingType.IgnoreCase;
                lemmatize = false;
                enabled = true;
                minLen = 1;
                ArrayList<string> crumbs = new ArrayList<string>(new string[] { mUri });
                Entity[] objects = rdfStore.SelectSubjects(P_IDENTIFIED_BY, new Entity(mUri));
                if (objects.Length > 0)
                {
                    Resource[] objTypes = rdfStore.SelectObjects(objects[0].Uri, P_TYPE);
                    if (objTypes.Length > 0)
                    {
                        crumbs.Add(objTypes[0].Uri);
                        Resource[] superClass = rdfStore.SelectObjects((Entity)objTypes[0], P_SUBCLASS_OF);
                        while (superClass.Length > 0)
                        {
                            crumbs.Add(superClass[0].Uri);
                            superClass = rdfStore.SelectObjects((Entity)superClass[0], P_SUBCLASS_OF);
                        }
                    }
                }
                crumbs.Reverse();
                foreach (string uri in crumbs)
                {
                    Resource[] settings = rdfStore.SelectObjects(uri, P_SETTINGS);
                    if (settings.Length == 0) { settings = rdfStore.SelectObjects(uri, P_COMMENT); } // compatibility with OWL-DL
                    if (settings.Length > 0)
                    {
                        string settingsStr = ((Literal)settings[0]).Value;
                        ParseGazetteerSettings(settingsStr, ref caseMatchingType, ref lemmatize, ref enabled, ref minLen);
                    }
                }
            }

            public void ReadTerms(MemoryStore rdfStore)
            {
                // read default settings
                CaseMatchingType caseMatchingType;
                bool lemmatize;
                ReadGazetteerSettings(rdfStore, out caseMatchingType, out lemmatize, out mEnabled, out mMinLen);
                // read terms
                Resource[] terms = rdfStore.SelectObjects(mUri, P_TERM);
                Set<string> skipList = new Set<string>();
                foreach (Literal term in terms)
                {
                    GazetteerTerm termObj = new GazetteerTerm(term.Value, /*gazetteer=*/this, caseMatchingType, lemmatize, mEnabled, mMinLen);
                    string termStr = termObj.ToString();
                    if (termObj.mTokens.Count > 0 && !skipList.Contains(termStr))
                    {
                        mTerms.Add(termObj);
                        skipList.Add(termStr);
                    }
                }
            }
        }

        public string DefaultClassUri
        {
            get { return mDefaultClassUri; }
            set { mDefaultClassUri = value; }
        }

        public void ImportRdfFromUrl(string url)
        {
            int statementCount = mRdfStore.StatementCount;
            mRdfStore.Import(RdfXmlReader.LoadFromUri(new Uri(url)));
            mLogger.Info("ImportRdfFromUri", "Imported {0} statements.", mRdfStore.StatementCount - statementCount);
        }

        public void ImportRdfFromFile(string fileName)
        {
            int statementCount;
            try
            {
                statementCount = mRdfStore.StatementCount;
                mRdfStore.Import(new RdfXmlReader(fileName));
            }
            catch
            {
                statementCount = mRdfStore.StatementCount;
                mRdfStore.Import(new N3Reader(fileName));
            }
            mLogger.Info("ImportRdfFromFile", "Imported {0} statements.", mRdfStore.StatementCount - statementCount);
        }

        public void ImportRdfFromFolder(string folderName)
        {
            ArrayList<string> fileNames = new ArrayList<string>();
            foreach (string searchPattern in new string[] { "*.rdf", "*.xml", "*.n3" })
            {
                fileNames.AddRange(Directory.GetFiles(folderName, searchPattern, SearchOption.AllDirectories));
            }
            foreach (string fileName in fileNames)
            {
                ImportRdfFromFile(fileName);
            }
        }

        public string GetIdentifiedInstance(string gazetteerUri)
        {
            Entity[] objects = mRdfStore.SelectSubjects(P_IDENTIFIED_BY, new Entity(gazetteerUri));
            if (objects.Length > 0) { return objects[0].Uri; }
            return null;
        }

        public string GetInstanceClass(string instanceUri)
        {
            Resource[] objTypes = mRdfStore.SelectObjects(instanceUri, P_TYPE);
            if (objTypes.Length > 0) { return objTypes[0].Uri; }
            return null;
        }

        public ArrayList<string> GetInstanceClassPath(string instanceUri)
        {
            ArrayList<string> crumbs = new ArrayList<string>();
            string instanceClass = GetInstanceClass(instanceUri);
            if (instanceClass == null) { instanceClass = mDefaultClassUri; } 
            crumbs.Add(instanceClass);
            Resource[] superClass = mRdfStore.SelectObjects(instanceClass, P_SUBCLASS_OF);
            while (superClass.Length > 0)
            {
                crumbs.Add(superClass[0].Uri);
                superClass = mRdfStore.SelectObjects((Entity)superClass[0], P_SUBCLASS_OF);
            }
            return crumbs;
        }

        public void LoadGazetteers()
        {
            mLogger.Info("LoadGazetteers", "Loading gazetteers ...");
            Entity[] gazetteers = mRdfStore.SelectSubjects(P_TYPE, C_GAZETTEER);
            mLogger.Info("LoadGazetteers", "Found {0} gazetteers.", gazetteers.Length);
            // create gazetteer objects
            foreach (Entity gazetteer in gazetteers)
            {
                Gazetteer gazetteerObj = new Gazetteer(gazetteer.Uri);               
                mGazetteers.Add(gazetteer.Uri, gazetteerObj);
                // read stop words
                gazetteerObj.ReadStopWords(mRdfStore); 
            }
            // import gazetteers and read conditions
            foreach (Entity gazetteer in gazetteers)
            {
                mGazetteers[gazetteer.Uri].ImportGazetteers(mRdfStore, mGazetteers);
                mGazetteers[gazetteer.Uri].ReadConditions(mRdfStore, mGazetteers);
            }
            // read terms
            foreach (Entity gazetteer in gazetteers)
            {
                mGazetteers[gazetteer.Uri].ReadTerms(mRdfStore);                
            }
        }

        // *** Utils ***

        private static string RemoveDiacritics(string str)
        {
            string stFormD = str.Normalize(NormalizationForm.FormD);
            int len = stFormD.Length;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                if (uc != UnicodeCategory.NonSpacingMark &&
                    uc != UnicodeCategory.SpacingCombiningMark &&
                    uc != UnicodeCategory.EnclosingMark)
                {
                    sb.Append(stFormD[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        private static string Normalize(string str)
        {
            Set<char> umlauts = new Set<char>(new char[] { 'Ä', 'Ö', 'Ü' });
            string strNrm = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (umlauts.Contains(Char.ToUpper(str[i])))
                {
                    if (Char.IsLower(str[i]) || (i < str.Length - 1 && Char.IsLower(str[i + 1]))) { strNrm += str[i] + "e"; }
                    else { strNrm += str[i] + "E"; }
                }
                else { strNrm += str[i]; } 
            }
            return RemoveDiacritics(strNrm);
        }

        private static void ParseGazetteerSettings(string settingsStr, ref CaseMatchingType caseMatchingType, ref bool lemmatize, ref bool enabled,
            ref int minLen)
        {
            string[] settings = settingsStr.TrimStart('/').Split('/');
            foreach (string setting in settings)
            {
                string[] keyVal = setting.Split('=');
                if (keyVal.Length == 2)
                {
                    if (keyVal[0] == "e") // enabled 
                    {
                        enabled = keyVal[1] != "n";
                    }
                    else if (keyVal[0] == "l") // lemmatize 
                    {
                        lemmatize = keyVal[1] == "y";
                    }
                    else if (keyVal[0] == "ml") // minimum annotation length
                    {
                        minLen = Convert.ToInt32(keyVal[1]);
                    }
                    else if (keyVal[0] == "c") // case-matching type
                    {
                        if (keyVal[1] == "ic") { caseMatchingType = CaseMatchingType.IgnoreCase; }
                        else if (keyVal[1] == "em") { caseMatchingType = CaseMatchingType.ExactMatch; }
                        else if (keyVal[1] == "acs") { caseMatchingType = CaseMatchingType.AllCapsStrict; }
                        else if (keyVal[1] == "acl") { caseMatchingType = CaseMatchingType.AllCapsLoose; }
                        else if (keyVal[1] == "ics") { caseMatchingType = CaseMatchingType.InitCapStrict; }
                        else if (keyVal[1] == "icl") { caseMatchingType = CaseMatchingType.InitCapLoose; } 
                        else if (keyVal[1] == "alc") { caseMatchingType = CaseMatchingType.AllLowercase; }
                        else if (keyVal[1] == "auc") { caseMatchingType = CaseMatchingType.AllUppercase; }
                    }
                }
            }
        }
    }
}
