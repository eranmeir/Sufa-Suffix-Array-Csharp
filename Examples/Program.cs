using System;
using System.Collections.Generic;
using System.Text;
using Sufa;

namespace Sufa.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            string str = "abcracadabra";
            // Create a new suffix array
            SuffixArray sa = new SuffixArray(str);
            // Find substring
            int index = sa.IndexOf("rac");
            // Get LCP value 
            int lcp = sa.Lcp[index];
        }
    }
}
