/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Features.cs
 *  Desc:    Feature collection data structure
 *  Created: Dec-2010
 *
 *  Authors: Jasmina Smailovic, Miha Grcar
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections;
using Latino;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Features
       |
       '-----------------------------------------------------------------------
    */
    public class Features : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> mFeatures;

        internal Features(Dictionary<string, string> features)
        {
            mFeatures = features;
        }

        public Dictionary<string, string>.KeyCollection Names
        {
            get { return mFeatures.Keys; }
        }

        public void SetFeatureValue(string name, string val)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            if (mFeatures.ContainsKey(name))
            {
                mFeatures[name] = val;
            }
            else
            {
                mFeatures.Add(name, val);
            }
        }

        public string GetFeatureValue(string name)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            string value;
            if (mFeatures.TryGetValue(name, out value))
            {
                return value;
            }
            return null;
        }

        public bool RemoveFeature(string name)
        {
            Utils.ThrowException(name == null ? new ArgumentNullException("name") : null);
            if (mFeatures.ContainsKey(name))
            {
                mFeatures.Remove(name);
                return true;
            }
            return false;
        }

        // *** IEnumerable<KeyValuePair<string,string>> interface implementation ***

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return mFeatures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
