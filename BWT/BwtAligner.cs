using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;
using CS124Project.SAIS;

namespace CS124Project.BWT
{
    class BwtAligner
    {
        private static byte[] C;
        private uint[][] Occurrences { get; set; }
        private uint[][] Occurrences_rev { get; set; }
        private ISuffixArray SuffixArray { get; set; }
        private ISuffixArray SuffixArray_rev { get; set; }
        private DnaSequence ReferenceGenome;

        public BwtAligner(byte[] C, uint[][] occurrences, uint[][] occurrencesRev, )

        public IEnumerable<Tuple<uint, uint>> GetAlignments(DnaSequence shortRead, int allowedDifferences)
        {
            var minDifferences = CalculateMinimumDifferences(shortRead);
            return GetSuffixArrayBounds(shortRead, (int)shortRead.Length - 1, allowedDifferences, minDifferences, 1, SuffixArray.Length - 1);
        }

        /// <summary>
        /// Calculates the lower bound of the number of differences needed to align shortRead[0..i]
        /// </summary>
        /// <param name="?"></param>
        /// <param name="shortRead"></param>
        byte[] CalculateMinimumDifferences(DnaSequence shortRead)
        {
            byte differences = 0;
            byte[] minDifferences = new byte[shortRead.Length];
            for (uint readIndex = 0, minSAIndex = 1, maxSAIndex = SuffixArray_rev.Length-1; readIndex < shortRead.Length - 1; readIndex++)
            {
                minSAIndex = C[shortRead[readIndex]] + Occurrences_rev[shortRead[readIndex]][minSAIndex - 1] + 1;
                maxSAIndex = C[shortRead[readIndex]] + Occurrences_rev[shortRead[readIndex]][maxSAIndex];
                if (minSAIndex > maxSAIndex)
                {
                    minSAIndex = 1;
                    maxSAIndex = SuffixArray_rev.Length - 1;
                    differences++;
                }
                minDifferences[readIndex] = differences;
            }

            return minDifferences;
        }

        IEnumerable<Tuple<uint, uint>> GetSuffixArrayBounds(DnaSequence shortRead, int i, int allowedDiff, byte[] minDiffs, uint minSaIndex, uint maxSaIndex)
        {
            if (allowedDiff < minDiffs[i])
                return new List<Tuple<uint, uint>>();
            if (i < 0)
                return new List<Tuple<uint, uint>>();

            IEnumerable<Tuple<uint, uint>> alignments = new List<Tuple<uint, uint>>();
            var deletionAlignments = GetSuffixArrayBounds(shortRead, i - 1, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
            alignments = alignments.Union(deletionAlignments);

            for (int dnaBase = 0; dnaBase < 4; dnaBase++)
            {
                minSaIndex = C[dnaBase] + Occurrences[dnaBase][minSaIndex - 1] + 1;
                maxSaIndex = C[dnaBase] + Occurrences[dnaBase][maxSaIndex];

                if (minSaIndex <= maxSaIndex)
                {
                    var insertionAlignments = GetSuffixArrayBounds(shortRead, i, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
                    alignments = alignments.Union(insertionAlignments);

                    if (dnaBase == shortRead[i])
                    {
                        var matchedAlignments = GetSuffixArrayBounds(shortRead, i - 1, allowedDiff, minDiffs, minSaIndex, maxSaIndex);
                        alignments = alignments.Union(matchedAlignments);
                    }
                    else
                    {
                        var mismatchedAlignments = GetSuffixArrayBounds(shortRead, i - 1, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
                        alignments = alignments.Union(mismatchedAlignments);
                    }
                }
            }

            return alignments;
        }
    }
}
