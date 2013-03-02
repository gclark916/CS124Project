using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;
using CS124Project.SAIS;

namespace CS124Project.BWT
{
    class BwtAligner
    {
        private uint[] C;
        private OccurrenceArray[] Occurrences { get; set; }
        private OccurrenceArray[] OccurrencesRev { get; set; }
        private CompressedSuffixArray SuffixArray { get; set; }
        private CompressedSuffixArray SuffixArrayRev { get; set; }

        public static void SavePrecomputedDateToFiles(string baseFileName, DnaSequence referenceGenome, DnaSequence reverseGenome)
        {
            Level0String level0String = new Level0String(referenceGenome);
            SuffixArray suffixArray = SAIS.SuffixArray.CreateSuffixArray(level0String);
            DnaBwt bwt = new DnaBwt(level0String, suffixArray);
            OccurrenceArray[] occs = OccurrenceArray.CreateOccurrenceArrays(bwt);
            
            suffixArray.WriteToFile(baseFileName+".csa", 32);
            bwt.WriteToFile(baseFileName+".bwt");
            WriteCToFile(baseFileName+".c", bwt);
            OccurrenceArray.WriteToFile(baseFileName + ".occ", occs);

            level0String = new Level0String(reverseGenome);
            suffixArray = SAIS.SuffixArray.CreateSuffixArray(level0String);
            bwt = new DnaBwt(level0String, suffixArray);
            occs = OccurrenceArray.CreateOccurrenceArrays(bwt);

            suffixArray.WriteToFile(baseFileName + "_rev.csa", 32);
            bwt.WriteToFile(baseFileName + "_rev.bwt");
            OccurrenceArray.WriteToFile(baseFileName + "_rev.occ", occs);
        }

        public static BwtAligner CreateBwtAlignerFromFiles(string baseFileName)
        {
            DnaBwt bwt = DnaBwt.ReadFromFile(baseFileName + ".bwt");
            uint[] C = ReadCFromFile(baseFileName + ".c");
            OccurrenceArray[] occ = OccurrenceArray.CreateFromFile(baseFileName + ".occ", bwt);
            CompressedSuffixArray csa = CompressedSuffixArray.CreateFromFile(baseFileName + ".csa", bwt, occ, C);

            DnaBwt bwtRev = DnaBwt.ReadFromFile(baseFileName + "_rev.bwt");
            OccurrenceArray[] occRev = OccurrenceArray.CreateFromFile(baseFileName + "_rev.occ", bwtRev);
            CompressedSuffixArray csaRev = CompressedSuffixArray.CreateFromFile(baseFileName + "_rev.csa", bwtRev, occRev, C);

            BwtAligner aligner = new BwtAligner(C, occ, occRev, csa, csaRev);
            return aligner;
        }

        public BwtAligner(uint[] C, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev)
        {
            this.C = C;
            Occurrences = occurrences;
            OccurrencesRev = occurrencesRev;
            SuffixArray = suffixArray;
            SuffixArrayRev = suffixArrayRev;
        }

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
            for (uint readIndex = 0, minSAIndex = 1, maxSAIndex = SuffixArrayRev.Length-1; readIndex < shortRead.Length - 1; readIndex++)
            {
                minSAIndex = (uint) (C[shortRead[readIndex]] + OccurrencesRev[shortRead[readIndex]][minSAIndex - 1] + 1);
                maxSAIndex = (uint) (C[shortRead[readIndex]] + OccurrencesRev[shortRead[readIndex]][maxSAIndex]);
                if (minSAIndex > maxSAIndex)
                {
                    minSAIndex = 1;
                    maxSAIndex = SuffixArrayRev.Length - 1;
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
                minSaIndex = (uint) (C[dnaBase] + Occurrences[dnaBase][minSaIndex - 1] + 1);
                maxSaIndex = (uint) (C[dnaBase] + Occurrences[dnaBase][maxSaIndex]);

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

        public static void WriteCToFile(string fileName, DnaBwt bwt)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                uint[] C = new uint[4];
                for (int i = 0; i < bwt.Length - 1; i++)
                {
                    if (bwt[i] < 0)
                        continue;

                    C[bwt[i]]++;
                }

                C[3] = C[0] + C[1] + C[2];
                C[2] = C[0] + C[1];
                C[1] = C[0];
                C[0] = 0;

                for (int i = 0; i < 4; i++)
                {
                    file.Write(BitConverter.GetBytes(C[i]), 0, sizeof(uint));
                }
            }
        }

        public static uint[] ReadCFromFile(string fileName)
        {
            uint[] C = new uint[4];
            using (var file = File.Open(fileName, FileMode.Create))
            {
                byte[] buffer = new byte[4 * sizeof(uint)];
                file.Read(buffer, 0, 4 * sizeof(uint));
                C[0] = BitConverter.ToUInt32(buffer, 0);
                C[1] = BitConverter.ToUInt32(buffer, sizeof(uint));
                C[2] = BitConverter.ToUInt32(buffer, 2 * sizeof(uint));
                C[3] = BitConverter.ToUInt32(buffer, 3 * sizeof(uint));
            }
            return C;
        }
    }
}
