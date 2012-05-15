/*
 Copyright (c) 2012 Eran Meir

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using C5;

namespace Sufa
{
    [Serializable]
    internal class Chain : IComparable<Chain>
    {
        public int head;
        public int length;
        private string m_str;

        public Chain(string str)
        {
            m_str = str;
        }

        public int CompareTo(Chain other)
        {
            return m_str.Substring(head, length).CompareTo(m_str.Substring(other.head, other.length));
        }

        public override string ToString()
        {
            return m_str.Substring(head, length);
        }
    }

    [Serializable]
    internal class CharComparer : System.Collections.Generic.EqualityComparer<char>
    {
        public override bool Equals(char x, char y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(char obj)
        {
            return obj.GetHashCode();
        }
    }

    internal struct SuffixRank
    {
        public int head;
        public int rank;
    }

    class SuffixRankComparer : IComparer<SuffixRank>
    {
        public bool Equals(SuffixRank x, SuffixRank y)
        {
            return x.rank.Equals(y.rank);
        }

        public int Compare(SuffixRank x, SuffixRank y)
        {
            return x.rank.CompareTo(y.rank);
        }
    }

    [Serializable]
    public class SuffixArray
    {
        private const int EOC = int.MaxValue;
        private int[] m_sa;
        private int[] m_isa;
        private int[] m_lcp;
        private C5.HashDictionary<char, int> m_chainHeadsDict = new HashDictionary<char, int>(new CharComparer());
        private List<Chain> m_chainStack = new List<Chain>();
        ArrayList<Chain> m_subChains = new ArrayList<Chain>();
        private int m_nextRank = 1;
        private string m_str;
        //private List<int> m_currentChain = new List<int>();

        public int Length
        {
            get { return m_sa.Length; }
        }

        public int this[int index]
        {
            get { return m_sa[index]; }
        }

        public int[] Lcp
        {
            get { return m_lcp; }
        }

        public string Str
        {
            get { return m_str; }
        }

        public SuffixArray(string str) : this(str, true) {}

        public SuffixArray(string str, bool buildLcps) 
        {
            m_str = str;
            if (m_str == null)
            {
                m_str = "";
            }
            m_sa = new int[m_str.Length];
            m_isa = new int[m_str.Length];

            FormInitialChains();
            BuildSuffixArray();
            if (buildLcps)
                BuildLcpArray();
        }

        /// <summary>
        /// Link all suffixes that have the same first character
        /// </summary>
        private void FormInitialChains()
        {
            FindInitialChains();
            SortAndPushSubchains();
        }

        private void FindInitialChains()
        {
            // Scan the string left to right, keeping rightmost occurences of characters as the chain heads
            for (int i = 0; i < m_str.Length; i++)
            {
                if (m_chainHeadsDict.Contains(m_str[i]))
                {
                    m_isa[i] = m_chainHeadsDict[m_str[i]];
                }
                else
                {
                    m_isa[i] = EOC;
                }
                m_chainHeadsDict[m_str[i]] = i;
            }

            // Prepare chains to be pushed to stack
            foreach (int headIndex in m_chainHeadsDict.Values)
            {
                Chain newChain = new Chain(m_str);
                newChain.head = headIndex;
                newChain.length = 1;
                m_subChains.Add(newChain);
            }
        }

        private void SortAndPushSubchains()
        {
            m_subChains.Sort();
            for (int i = m_subChains.Count - 1; i >= 0; i--)
            {
                m_chainStack.Add(m_subChains[i]);
            }
        }

        private void BuildSuffixArray()
        {
            while (m_chainStack.Count > 0)
            {
                // Pop chain
                Chain chain = m_chainStack[m_chainStack.Count - 1];
                m_chainStack.RemoveAt(m_chainStack.Count - 1);
                
                if (m_isa[chain.head] == EOC)
                {
                    // Singleton (A chain that contain only 1 suffix)
                    RankSuffix(chain.head);
                }
                else
                {
                    //RefineChains(chain);
                    RefineChainWithInductionSorting(chain);
                }
            }
        }

        private void RefineChains(Chain chain)
        {
            m_chainHeadsDict.Clear();
            m_subChains.Clear();
            while (chain.head != EOC)
            {
                // TODO - refactor this to get rid of the side effect of changing m_isa
                int nextIndex = m_isa[chain.head];
                UpdateSubChains(chain);
                chain.head = nextIndex;
            }
            // Keep stack lexically sorted
            SortAndPushSubchains();
        }

        private void UpdateSubChains(Chain chain)
        {
            if (chain.head + chain.length > m_str.Length - 1)
            {
                RankSuffix(chain.head);
            }
            else
            {
                char sym = m_str[chain.head + chain.length];
                if (m_chainHeadsDict.Contains(sym))
                {
                    // Continuation of a known chain, this is the leftmost
                    // occurence currently known (others may come up later)
                    m_isa[m_chainHeadsDict[sym]] = chain.head;
                    m_isa[chain.head] = EOC;
                }
                else
                {
                    // This is the beginning of a new subchain
                    m_isa[chain.head] = EOC;
                    Chain newChain = new Chain(m_str);
                    newChain.head = chain.head;
                    newChain.length = chain.length + 1;
                    m_subChains.Add(newChain);
                }
                // Save index in case we find a continuation of this chain
                m_chainHeadsDict[sym] = chain.head;
            }
        }

        private void RefineChainWithInductionSorting(Chain chain)
        {
            // TODO - refactor m_chainHeadsDict and m_subChains into a subchains class, remove class members and pass a 
            // variable instead (get rid of global state)
            m_chainHeadsDict.Clear();
            m_subChains.Clear();

            // TODO - and refactor notedSuffixes too
            ArrayList<SuffixRank> notedSuffixes = new ArrayList<SuffixRank>();

            while (chain.head != EOC)
            {
                int nextIndex = m_isa[chain.head];
                // TODO - refactor 
                if (chain.head + chain.length > m_str.Length - 1)
                {
                    RankSuffix(chain.head);
                }
                else if (m_isa[chain.head + chain.length] < 0)
                {
                    SuffixRank sr = new SuffixRank();
                    sr.head = chain.head;
                    sr.rank = -m_isa[chain.head + chain.length];
                    notedSuffixes.Add(sr);
                }
                else
                {
                    UpdateSubChains(chain);
                }
                chain.head = nextIndex;
            }
            // Keep stack lexically sorted
            SortAndPushSubchains();
            SortAndRankNotedSuffixes(notedSuffixes);
        }

        private void SortAndRankNotedSuffixes(ArrayList<SuffixRank> notedSuffixes)
        {
            notedSuffixes.Sort(new SuffixRankComparer());
            // Rank sorted noted suffixes 
            for (int i = 0; i < notedSuffixes.Count; ++i)
            {
                RankSuffix(notedSuffixes[i].head);
            }
        }

        private void RankSuffix(int index)
        {
            // We use the ISA to hold both ranks and chain links, so we differentiate by setting
            // the sign.
            m_isa[index] = -m_nextRank;
            m_sa[m_nextRank - 1] = index;
            m_nextRank++;
        }

        private void BuildLcpArray()
        {
            m_lcp = new int[m_sa.Length + 1];
            m_lcp[0] = m_lcp[m_sa.Length] = 0;

            for (int i = 1; i < m_sa.Length; i++)
            {
                m_lcp[i] = CalcLcp(m_sa[i - 1], m_sa[i]);
            }
        }

        private int CalcLcp(int i, int j)
        {
            int lcp;
            int maxIndex = m_str.Length - Math.Max(i, j);       // Out of bounds prevention
            for (lcp = 0; (lcp < maxIndex) && (m_str[i + lcp] == m_str[j + lcp]); lcp++) ;
            return lcp;
        }

        public int IndexOf(string substr)
        {
            int l = 0;
            int r = m_sa.Length;
            int m = -1;

            if ((substr == null) || (substr.Length == 0))
            {
                return -1;
            }

            // Binary search for substring
            while (r > l)
            {
                m = (l + r) / 2;
                if (m_str.Substring(m_sa[m]).CompareTo(substr) < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }
            if ((l == r) && (l < m_str.Length) && (m_str.Substring(m_sa[l]).StartsWith(substr)))
            {
                return m_sa[l];
            }
            else
            {
                return -1;
            }
        }
    }
}

