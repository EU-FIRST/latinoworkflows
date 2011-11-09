/*

TODO: customizable path root given as concept URI, sync tokenization, show immediate neighborhood
 
*/

/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    SemanticAnnotator.cs
 *  Desc:    Simple ontology-based entity recognition
 *  Created: Nov-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SemWeb;
using Latino.TextMining;
using Latino.Workflows.TextMining;

namespace Latino.Workflows.Semantics
{
    /* .-----------------------------------------------------------------------
       |
       |  Class SemanticAnnotator
       |
       '-----------------------------------------------------------------------
    */
    public class SemanticAnnotator
    {
        /* .-----------------------------------------------------------------------
           |
           |  Class Term
           |
           '-----------------------------------------------------------------------
        */
        private class Term
        {
            public ArrayList<string> mWords
                = null;

            public bool Match(string[] words, int startIdx, out int len, Gazetteer gazetteer)
            {
                int idx = startIdx;
                len = 0;
                if (string.Compare(words[idx++], mWords[0], gazetteer.mIgnoreCase) != 0) { return false; } // first word must match
                for (int i = 1; i < mWords.Count; i++)
                {
                    while (idx < words.Length && gazetteer.IsStopWord(words[idx].ToLower())) { idx++; } // skip stop words
                    if (idx == words.Length) { return false; }
                    if (string.Compare(words[idx++], mWords[i], gazetteer.mIgnoreCase) != 0) { return false; }
                }
                len = idx - startIdx;
                return true;
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
            public ArrayList<Term> mTerms
                = new ArrayList<Term>();
            public Set<string> mStopWords
                = new Set<string>();
            public ArrayList<Gazetteer> mImportedGazetteers
                = new ArrayList<Gazetteer>();
            // settings
            public bool mIgnoreCase
                = true;

            public bool IsStopWord(string word)
            {
                if (mStopWords.Contains(word)) { return true; }
                foreach (Gazetteer importedGazetteer in mImportedGazetteers)
                {
                    if (importedGazetteer.IsStopWord(word)) { return true; }
                }
                return false;
            }

            public void ReadStopWords(MemoryStore rdfStore)
            {
                Resource[] stopWords = rdfStore.SelectObjects(mUri, P_HAS_STOP_WORD);
                foreach (Literal word in stopWords)
                {
                    string stopWordStr = Normalize(word.Value.ToLower());
                    mStopWords.Add(stopWordStr);
                }
            }

            public void ImportGazetteers(MemoryStore mRdfStore, Dictionary<string, Gazetteer> gazetteers)
            {
                Resource[] importedGazetteers = mRdfStore.SelectObjects(mUri, P_IMPORTS);
                foreach (Entity importedGazetteer in importedGazetteers)
                {
                    mImportedGazetteers.Add(gazetteers[importedGazetteer.Uri]);
                }
            }

            public void ReadTerms(MemoryStore mRdfStore)
            {
                Resource[] terms = mRdfStore.SelectObjects(mUri, P_HAS_TERM);
                Set<string> skipList = new Set<string>();
                foreach (Literal term in terms)
                {
                    mTokenizer.Text = Normalize(mIgnoreCase ? term.Value.ToLower() : term.Value);
                    ArrayList<string> tokens = new ArrayList<string>();
                    foreach (string token in mTokenizer)
                    {
                        string tokenLower = token.ToLower();
                        if (!IsStopWord(tokenLower)) { tokens.Add(token); }
                    }
                    if (tokens.Count > 0 && !skipList.Contains(tokens.ToString()))
                    {
                        Term termObj = new Term();
                        termObj.mWords = tokens;
                        mTerms.Add(termObj);
                        skipList.Add(tokens.ToString());
                    }
                }              
            }

            public void ReadSettings(MemoryStore mRdfStore)
            {
                Resource[] r = mRdfStore.SelectObjects(mUri, P_IGNORES_CASE);
                if (r.Length > 0 && ((Literal)r[0]).Value == "false") { mIgnoreCase = false; }
            }

            public bool Match(string[] tokens, int startIdx, out int len)
            {
                return Match(tokens, startIdx, out len, /*gazetteer=*/this);
            }

            public bool Match(string[] tokens, int startIdx, out int len, Gazetteer gazetteer)
            {
                if (IsMatch(tokens, startIdx, out len, gazetteer)) { return true; }
                foreach (Gazetteer importedGazetteer in mImportedGazetteers)
                {
                    if (importedGazetteer.Match(tokens, startIdx, out len, gazetteer)) { return true; }
                }
                return false;
            }

            public bool IsMatch(string[] tokens, int startIdx, out int len, Gazetteer gazetteer)
            {
                len = startIdx;
                foreach (Term term in mTerms)
                {
                    if (term.Match(tokens, startIdx, out len, gazetteer)) { return true; }
                }
                return false;
            }
        }

        private const string NAMESPACE
            = "http://project-first.eu/ontology#";
        private static Entity C_GAZETTEER
            = NAMESPACE + "Gazetteer";
        private static Entity P_HAS_TERM
            = NAMESPACE + "hasTerm";
        private static Entity P_HAS_STOP_WORD
            = NAMESPACE + "hasStopWord";
        private static Entity P_IMPORTS
            = NAMESPACE + "importsGazetteer";
        private static Entity P_HAS_GAZETTEER
            = NAMESPACE + "hasGazetteer";
        private static Entity P_IGNORES_CASE
            = NAMESPACE + "ignoreCase";
        private static Entity P_TYPE
            = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        private static Entity P_SUBCLASS_OF
            = "http://www.w3.org/2000/01/rdf-schema#subClassOf";
        private static Entity P_LABEL
            = "http://www.w3.org/2000/01/rdf-schema#label";

        private MemoryStore mRdfStore
            = new MemoryStore();

        private Dictionary<string, Gazetteer> mGazetteers
            = new Dictionary<string, Gazetteer>();
        
        private static SimpleTokenizer mTokenizer
            = new SimpleTokenizer();

        private static Logger mLogger
            = Logger.GetLogger(typeof(SemanticAnnotator));

        static SemanticAnnotator()
        {
            mTokenizer.Type = TokenizerType.AlphanumOnly;
        }

        private static string RemoveDiacritics(string term)
        {
            string stFormD = term.Normalize(NormalizationForm.FormD);
            int len = stFormD.Length;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                System.Globalization.UnicodeCategory uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark &&
                    uc != System.Globalization.UnicodeCategory.SpacingCombiningMark &&
                    uc != System.Globalization.UnicodeCategory.EnclosingMark)
                {
                    sb.Append(stFormD[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        private static string Normalize(string term)
        {
            Set<char> umlauts = new Set<char>(new char[] { 'Ä', 'Ö', 'Ü' });
            string termNrm = "";
            for (int i = 0; i < term.Length; i++)
            {
                if (umlauts.Contains(Char.ToUpper(term[i])))
                {
                    if (Char.IsLower(term[i]) || (i < term.Length - 1 && Char.IsLower(term[i + 1])))
                    {
                        termNrm += term[i] + "e";
                    }
                    else
                    {
                        termNrm += term[i] + "E";
                    }
                }
                else
                {
                    termNrm += term[i];
                }
            }
            return RemoveDiacritics(termNrm);
        }

        public void LoadOntologyN3(TextReader reader)
        {
            mRdfStore.Import(new N3Reader(reader));
        }

        public void LoadOntologyN3(string url)
        {
            mRdfStore.Import(N3Reader.LoadFromUri(new Uri(url)));
        }

        public void ReadGazetteers()
        {
            mLogger.Info("ReadGazetteers", "Reading gazetteers ...");
            Entity[] gazetteers = mRdfStore.SelectSubjects(P_TYPE, C_GAZETTEER);
            mLogger.Info("ReadGazetteers", "Found {0} gazetteers.", gazetteers.Length);
            // gazetteer objects
            foreach (Entity gazetteer in gazetteers)
            {
                Gazetteer gazetteerObj = new Gazetteer();
                gazetteerObj.mUri = gazetteer.Uri;
                mGazetteers.Add(gazetteer.Uri, gazetteerObj);
                gazetteerObj.ReadStopWords(mRdfStore); // stop words
                gazetteerObj.ReadSettings(mRdfStore); // settings
            }
            // imported gazetteers
            foreach (Entity gazetteer in gazetteers)
            {
                mGazetteers[gazetteer.Uri].ImportGazetteers(mRdfStore, mGazetteers);
            }
            // terms
            foreach (Entity gazetteer in gazetteers)
            {
                mGazetteers[gazetteer.Uri].ReadTerms(mRdfStore);
            }
        }

        private void GetPaths(int depth, ArrayList<Entity> currentPath, ArrayList<ArrayList<Entity>> allPaths)
        {
            Entity currentEntity = currentPath.Last;
            Resource[] entities;
            if (depth == 0) // gazetteer
            {
                entities = mRdfStore.SelectSubjects(P_HAS_GAZETTEER, currentEntity);
            }
            else if (depth == 1) // instance
            {
                entities = mRdfStore.SelectObjects(currentEntity, P_TYPE);
            }
            else // depth > 1 // taxonomy
            {
                entities = mRdfStore.SelectObjects(currentEntity, P_SUBCLASS_OF);
            }
            if (entities.Length == 0)
            {
                // copy current path to allPaths
                allPaths.Add(currentPath.Clone());
            }
            else
            {
                foreach (Entity entity in entities)
                {
                    currentPath.Add(entity);
                    Console.WriteLine(entity.Uri);
                    GetPaths(depth + 1, currentPath, allPaths);
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }
        }

        public static string GetLabel(Entity entity, MemoryStore rdfStore)
        {
            string label = new ArrayList<string>(entity.Uri.Split('/', '#')).Last;
            Resource[] labels = rdfStore.SelectObjects(entity, P_LABEL);
            if (labels.Length > 0) 
            { 
                label = ((Literal)labels[0]).Value; // *** always take the first available label
                label = label.Replace('/', '-');
            } 
            return label;
        }

        public ArrayList<Annotation> ExtractEntities(string text, int offset)
        {
            mLogger.Debug("ExtractEntities", "Extracting entities ...");
            mTokenizer.Text = text;
            ArrayList<Annotation> annotations = new ArrayList<Annotation>();
            ArrayList<string> tmp = new ArrayList<string>();
            ArrayList<int> pos = new ArrayList<int>();
            for (SimpleTokenizer.Enumerator e = mTokenizer.GetEnumerator(); e.MoveNext(); )
            {
                tmp.Add(Normalize(e.Current));
                pos.Add(e.CurrentTokenIdx);
            }            
            string[] tokens = tmp.ToArray();
            foreach (Gazetteer gazetteer in mGazetteers.Values)
            {
                if (gazetteer.mTerms.Count > 0)
                {
                    int len;
                    for (int startIdx = 0; startIdx < tokens.Length; startIdx++)
                    {
                        if (gazetteer.Match(tokens, startIdx, out len))
                        {
                            ArrayList<ArrayList<Entity>> allPaths = new ArrayList<ArrayList<Entity>>();
                            GetPaths(0, new ArrayList<Entity>(new Entity[] { gazetteer.mUri }), allPaths);                           
                            foreach (ArrayList<Entity> path in allPaths)
                            {
                                string pathStr = "";
                                for (int i = path.Count - 1; i > 1; i--)
                                {
                                    pathStr += GetLabel(path[i], mRdfStore) + "/";
                                }
                                pathStr = pathStr.TrimEnd('/');
                                annotations.Add(new Annotation(pos[startIdx] + offset, pos[startIdx + len - 1] + tokens[startIdx + len - 1].Length - 1 + offset, pathStr));
                                annotations.Last.Features.SetFeatureValue("gazUri", gazetteer.mUri);
                                if (path.Count >= 2)
                                {
                                    annotations.Last.Features.SetFeatureValue("objUri", path[1].Uri);
                                    annotations.Last.Features.SetFeatureValue("objLabel", GetLabel(path[1], mRdfStore));
                                }
                            }
                        }
                    }
                }
            }
            return annotations;
        }
    }
}
