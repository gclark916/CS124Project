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
        private GenomeText _text;

        public TrieAligner(TrieNode root, uint[] suffixArray, GenomeText text)
        {
            _root = root;
            _suffixArray = suffixArray;
            _text = text;
        }

        public uint[] GetAlignments(GenomeText shortRead)
        {
            var rangeTuple = GetSuffixArrayRange(shortRead, shortRead.Length - 1, _root);
            var range = rangeTuple.Item2 - rangeTuple.Item1 + 1;
            uint[] alignments = new uint[range];
            for (long suffixArrayIndex = rangeTuple.Item1, alignmentsIndex = 0;
                suffixArrayIndex <= rangeTuple.Item2; 
                suffixArrayIndex++, alignmentsIndex++)
            {
                alignments[alignmentsIndex] = _suffixArray[suffixArrayIndex];
            }

            return alignments;
        }

        private Tuple<long, long> GetSuffixArrayRange(GenomeText shortRead, long index, TrieNode root)
        {
            if (root == null)
                return null;

            var baseToAlign = shortRead.GetBase(index);
            var child = root.GetChild(baseToAlign);
            if (index == 0)
                return new Tuple<long, long>(child.Min, child.Max);

            return GetSuffixArrayRange(shortRead, index - 1, child);
        }
    }
}
