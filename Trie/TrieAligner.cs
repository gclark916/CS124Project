using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project.Trie
{
    class TrieAligner
    {
        private readonly TrieNode _root;
        private readonly uint[] _suffixArray;

        public TrieAligner(TrieNode root, uint[] suffixArray, DnaSequence text)
        {
            _root = root;
            _suffixArray = suffixArray;
        }

        public uint[] GetAlignments(DnaSequence shortRead)
        {
            long min, max;
            GetSuffixArrayRange(shortRead, shortRead.Length - 1, _root, out min, out max);
            var range = max - min + 1;
            uint[] alignments = new uint[range];
            for (long suffixArrayIndex = min, alignmentsIndex = 0;
                suffixArrayIndex <= max; 
                suffixArrayIndex++, alignmentsIndex++)
            {
                alignments[alignmentsIndex] = _suffixArray[suffixArrayIndex];
            }

            return alignments;
        }

        private bool GetSuffixArrayRange(DnaSequence shortRead, long index, TrieNode root, out long min, out long max)
        {
            min = long.MaxValue;
            max = long.MinValue;

            if (root == null)
                return false;

            var baseToAlign = shortRead.GetBase(index);
            var child = root.GetChild(baseToAlign);
            if (index == 0)
            {
                min = child.Min;
                max = child.Max;
                return true;
            }

            return GetSuffixArrayRange(shortRead, index - 1, child, out min, out max);
        }
    }
}
