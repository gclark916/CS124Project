using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CS124Project.Genome;
using CS124Project.SAIS;

namespace CS124Project.BWT
{
    internal class BwtAligner
    {
        private uint[] C;

        public BwtAligner(uint[] C, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev)
        {
            this.C = C;
            Occurrences = occurrences;
            OccurrencesRev = occurrencesRev;
            SuffixArray = suffixArray;
            SuffixArrayRev = suffixArrayRev;
        }

        private OccurrenceArray[] Occurrences { get; set; }
        private OccurrenceArray[] OccurrencesRev { get; set; }
        public CompressedSuffixArray SuffixArray { get; set; }
        public CompressedSuffixArray SuffixArrayRev { get; set; }

        public static void SavePrecomputedDataToFiles(string baseFileName, DnaSequence referenceGenome,
                                                      DnaSequence reverseGenome)
        {
            Level0String level0String = new Level0String(referenceGenome);
            SuffixArray suffixArray = SAIS.SuffixArray.CreateSuffixArray(level0String);
            DnaBwt bwt = new DnaBwt(level0String, suffixArray);
            OccurrenceArray[] occs = OccurrenceArray.CreateOccurrenceArrays(bwt);

            suffixArray.WriteToFile(baseFileName + ".csa", 32);
            bwt.WriteToFile(baseFileName + ".bwt");
            WriteCToFile(baseFileName + ".c", bwt);
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
            CompressedSuffixArray csaRev = CompressedSuffixArray.CreateFromFile(baseFileName + "_rev.csa", bwtRev,
                                                                                occRev, C);

            BwtAligner aligner = new BwtAligner(C, occ, occRev, csa, csaRev);
            return aligner;
        }

        public void AlignReadsAndConstructGenome(string readsFile, string outFile)
        {
            Dictionary<uint, List<byte[]>> positionsToReads = new Dictionary<uint, List<byte[]>>();
            
            using (var file = File.OpenRead(readsFile))
            {
                var reader = new BinaryReader(file);
                var numReads = file.Length/8;
                Random[] randoms = new Random[8];
                ManualResetEvent[] manualResetEvents = new ManualResetEvent[8];
                for (int i = 0; i < manualResetEvents.Length; i++)
                {
                    randoms[i] = new Random();
                    manualResetEvents[i] = new ManualResetEvent(true);
                }

                long readsAligned = 0;
                while (readsAligned < numReads)
                {
                    readsAligned++;
                    byte[] shortReadBytes = reader.ReadBytes(8);
                    DnaSequence shortRead = new DnaSequence(shortReadBytes, 30);
                    int mreIndex = WaitHandle.WaitAny(manualResetEvents);
                    manualResetEvents[mreIndex].Reset();
                    var parameters = new Tuple<DnaSequence, int, Random, Dictionary<uint, List<byte[]>>, ManualResetEvent>(shortRead, 2, randoms[mreIndex], positionsToReads, manualResetEvents[mreIndex]);
                    ThreadPool.QueueUserWorkItem(o =>
                        {
                            var p = o as Tuple<DnaSequence, int, Random, Dictionary<uint, List<byte[]>>, ManualResetEvent>;
                            Debug.Assert(p != null, "p != null");
                            AddAlignmentsToDictionary(p.Item1, p.Item2, p.Item3, p.Item4);
                            p.Item5.Set();
                        }, parameters);
                    
                }

                WaitHandle.WaitAll(manualResetEvents);
            }


            using (var file = File.Open(outFile, FileMode.Create))
            {
                for (long textIndex = 0; textIndex < SuffixArray.Length - 1; textIndex++)
                {
                    int[] characterCounts = new int[4];
                    for (long i = textIndex; i > textIndex - 30 && i >= 0; i--)
                    {
                        List<byte[]> readsAtPos;
                        positionsToReads.TryGetValue((uint) i, out readsAtPos);
                        if (readsAtPos == null) continue;

                        foreach (var shortRead in readsAtPos)
                        {
                            DnaSequence sequence = new DnaSequence(shortRead, 30);
                            var character = sequence[textIndex - i];
                            characterCounts[character]++;
                        }
                    }

                    var finalCharacter = 'N';
                    var maxCount = 0;
                    var maxIndex = -1;
                    for (int i = 0; i < 4; i++)
                    {
                        if (characterCounts[i] > maxCount)
                        {
                            maxCount = characterCounts[i];
                            maxIndex = i;
                        }
                    }
                    switch (maxIndex)
                    {
                        case 0:
                            finalCharacter = 'A';
                            break;
                        case 1:
                            finalCharacter = 'C';
                            break;
                        case 2:
                            finalCharacter = 'G';
                            break;
                        case 3:
                            finalCharacter = 'T';
                            break;
                    }

                    file.Write(BitConverter.GetBytes(finalCharacter), 0, 1);
                }
            }
        }

        public void AddAlignmentsToDictionary(DnaSequence shortRead, int allowedDifferences, Random rng, Dictionary<uint, List<byte[]>> positionsToReads)
        {
            var minDifferences = CalculateMinimumDifferences(shortRead);
            var maxDiff = allowedDifferences < minDifferences[29] ? allowedDifferences : minDifferences[29];
            var alignments = GetSuffixArrayBounds(shortRead, (int) shortRead.Length - 1, maxDiff, minDifferences, 0, (uint) (SuffixArray.Length - 1)).ToArray();
            Tuple<uint, uint, int> finalAlignment = null;

            var noMismatches = alignments.Where(a => a.Item3 == 2).ToArray();
            if (noMismatches.Any())
            {
                var randomIndex = rng.Next(noMismatches.Count());
                finalAlignment = noMismatches[randomIndex];
            }

            if (finalAlignment == null)
            {
                var oneMismatch = alignments.Where(a => a.Item3 == 1).ToArray();
                if (oneMismatch.Any())
                {
                    var randomIndex = rng.Next(oneMismatch.Count());
                    finalAlignment = oneMismatch[randomIndex];
                }

                if (finalAlignment == null)
                {
                    var twoMismatches = alignments.Where(a => a.Item3 == 0).ToArray();
                    if (twoMismatches.Any())
                    {
                        var randomIndex = rng.Next(twoMismatches.Count());
                        finalAlignment = twoMismatches[randomIndex];
                    }
                }
            }

            if (finalAlignment != null)
            {
                var randomSuffixArrayIndex = finalAlignment.Item1 +
                                                rng.Next((int) (finalAlignment.Item2 - finalAlignment.Item1));
                var textPos = (uint) SuffixArray[randomSuffixArrayIndex];
                lock (positionsToReads)
                {
                    List<byte[]> readsAtPosition;
                    if (positionsToReads.TryGetValue(textPos, out readsAtPosition))
                        readsAtPosition.Add(shortRead.Bytes);
                    else
                    {
                        readsAtPosition = new List<byte[]>() {shortRead.Bytes};
                        positionsToReads.Add(textPos, readsAtPosition);
                    }
                }
            }
        }

        /// <summary>
        ///     Calculates the lower bound of the number of differences needed to align shortRead[0..i]
        /// </summary>
        /// <param name="?"></param>
        /// <param name="shortRead"></param>
        private byte[] CalculateMinimumDifferences(DnaSequence shortRead)
        {
            byte differences = 0;
            byte[] minDifferences = new byte[shortRead.Length];
            for (uint readIndex = 0, minSAIndex = 0, maxSAIndex = (uint) (SuffixArrayRev.Length - 1);
                 readIndex < shortRead.Length;
                 readIndex++)
            {
                var character = shortRead[readIndex];
                if (minSAIndex == 0)
                    minSAIndex = C[character] + 1;
                else
                    minSAIndex = (uint) (C[character] + OccurrencesRev[character][minSAIndex - 1] + 1);
                maxSAIndex = (uint) (C[character] + OccurrencesRev[character][maxSAIndex]);
                if (minSAIndex > maxSAIndex)
                {
                    minSAIndex = 0;
                    maxSAIndex = (uint) (SuffixArrayRev.Length - 1);
                    differences++;
                }
                minDifferences[readIndex] = differences;
            }

            return minDifferences;
        }

        private IEnumerable<Tuple<uint, uint, int>> GetSuffixArrayBounds(DnaSequence shortRead, int shortReadIndex, int allowedDiff,
                                                                         byte[] minDiffs, uint minIndex,
                                                                         uint maxIndex)
        {
            if (shortReadIndex < 0)
                return new List<Tuple<uint, uint, int>>
                    {
                        new Tuple<uint, uint, int>(minIndex, maxIndex, allowedDiff)
                    };
            if (allowedDiff < minDiffs[shortReadIndex])
                return new List<Tuple<uint, uint, int>>();


            List<Tuple<uint, uint, int>> alignments = new List<Tuple<uint, uint, int>>();
            //var deletionAlignments = GetSuffixArrayBounds(shortRead, i - 1, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
            //alignments = alignments.Union(deletionAlignments);

            for (uint dnaBase = 0; dnaBase < 4; dnaBase++)
                //for (int dnaBase = shortRead[i]; dnaBase == shortRead[i]; dnaBase++ )
            {
                uint minSaIndex = minIndex;
                uint maxSaIndex = maxIndex;
                if (minSaIndex == 0)
                    minSaIndex = C[dnaBase] + 1;
                else
                    minSaIndex = (uint) (C[dnaBase] + Occurrences[dnaBase][minSaIndex - 1] + 1);
                maxSaIndex = (uint) (C[dnaBase] + Occurrences[dnaBase][maxSaIndex]);

                if (minSaIndex <= maxSaIndex)
                {
                    //var insertionAlignments = GetSuffixArrayBounds(shortRead, i, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
                    //alignments = alignments.Union(insertionAlignments);

                    if (dnaBase == shortRead[shortReadIndex])
                    {
                        var matchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1, allowedDiff, minDiffs, minSaIndex,
                                                                     maxSaIndex);
                        alignments.AddRange(matchedAlignments);
                    }
                    else
                    {
                        if (allowedDiff > 0)
                        {
                            var mismatchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1, allowedDiff - 1, minDiffs,
                                                                            minSaIndex, maxSaIndex);
                            alignments.AddRange(mismatchedAlignments);
                        }
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
                for (int i = 0; i < bwt.Length; i++)
                {
                    if (bwt[i] < 0)
                        continue;

                    C[bwt[i]]++;
                }

                C[3] = C[0] + C[1] + C[2];
                C[2] = C[0] + C[1];
                C[1] = C[0];
                C[0] = 0;

                var writer = new BinaryWriter(file);
                for (int i = 0; i < 4; i++)
                {
                    writer.Write(C[i]);
                }
            }
        }

        public static uint[] ReadCFromFile(string fileName)
        {
            uint[] C = new uint[4];
            using (var file = File.OpenRead(fileName))
            {
                var reader = new BinaryReader(file);
                C[0] = reader.ReadUInt32();
                C[1] = reader.ReadUInt32();
                C[2] = reader.ReadUInt32();
                C[3] = reader.ReadUInt32();
            }
            return C;
        }
    }
}