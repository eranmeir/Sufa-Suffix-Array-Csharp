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
            SuffixArray sa = new SuffixArray("abracadabrax");
            //SuffixArray sa = new SuffixArray("aaaaaaaaaaaaaaaa");
            PrintSortedArray(sa);
        }


        static void PrintSortedArray(SuffixArray sa)
        {
            for (int i = 0; i < sa.Length; i++)
            {
                Console.Write(sa.Str.Substring(sa[i]));
                Console.WriteLine(" lcp = " + sa.Lcp[i].ToString());
            }
        }


    }
}
