using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CS124Project.BWT.Database;
using CS124Project.Genome;
using CS124Project.SAIS;
using NHibernate;
using LinqExtensionMethods = NHibernate.Linq.LinqExtensionMethods;
using SufBounds = System.Tuple<uint, uint, int>;

namespace CS124Project.BWT
{
    internal class BwtAligner
    {
        public BwtAligner(uint[] c, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev, int readLength)
        {
            C = c;
            Occurrences = occurrences;
            OccurrencesRev = occurrencesRev;
            SuffixArray = suffixArray;
            SuffixArrayRev = suffixArrayRev;
            ReadLength = readLength;
        }

        private uint[] C { get; set; }
        private int ReadLength { get; set; }
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

        public static BwtAligner CreateBwtAlignerFromFiles(string baseFileName, int readLength)
        {
            DnaBwt bwt = DnaBwt.ReadFromFile(baseFileName + ".bwt");
            uint[] C = ReadCFromFile(baseFileName + ".c");
            OccurrenceArray[] occ = OccurrenceArray.CreateFromFile(baseFileName + ".occ", bwt);
            CompressedSuffixArray csa = CompressedSuffixArray.CreateFromFile(baseFileName + ".csa", bwt, occ, C);

            DnaBwt bwtRev = DnaBwt.ReadFromFile(baseFileName + "_rev.bwt");
            OccurrenceArray[] occRev = OccurrenceArray.CreateFromFile(baseFileName + "_rev.occ", bwtRev);
            CompressedSuffixArray csaRev = CompressedSuffixArray.CreateFromFile(baseFileName + "_rev.csa", bwtRev,
                                                                                occRev, C);

            BwtAligner aligner = new BwtAligner(C, occ, occRev, csa, csaRev, readLength);
            return aligner;
        }

        public void AlignReadsAndConstructGenome(string readsFile, string outFile)
        {
            AlignmentDatabase database = new AlignmentDatabase("temp.db");

            using (var session = database.SessionFactory.OpenStatelessSession())
            using (var transaction = session.BeginTransaction())
            using (var file = File.OpenRead(readsFile))
            {
                var reader = new BinaryReader(file);
                var numReads = file.Length/DnaSequence.ByteArrayLength(ReadLength);
                Random[] randoms = new Random[10];
                ManualResetEvent[] manualResetEvents = new ManualResetEvent[10];
                for (int i = 0; i < manualResetEvents.Length; i++)
                {
                    randoms[i] = new Random();
                    manualResetEvents[i] = new ManualResetEvent(true);
                }

                long readsAligned = 0;
                while (readsAligned < numReads)
                {
                    if (readsAligned%5000 == 0)
                    {
                        Console.WriteLine(readsAligned);
                    }
                    byte[] shortReadBytes = reader.ReadBytes(DnaSequence.ByteArrayLength(ReadLength));
                    DnaSequence shortRead = new DnaSequence(shortReadBytes, ReadLength);
                    int mreIndex = WaitHandle.WaitAny(manualResetEvents);
                    manualResetEvents[mreIndex].Reset();
                    var parameters =
                        new Tuple<DnaSequence, int, Random, IStatelessSession, ManualResetEvent, long>(shortRead, 2,
                                                                                                       randoms[mreIndex],
                                                                                                       session,
                                                                                                       manualResetEvents
                                                                                                           [mreIndex],
                                                                                                       readsAligned);
                    ThreadPool.QueueUserWorkItem(o =>
                        {
                            var p = o as Tuple<DnaSequence, int, Random, IStatelessSession, ManualResetEvent, long>;
                            Debug.Assert(p != null, "p != null");
                            AddAlignmentsToDictionary(p.Item1, p.Item2, p.Item3, p.Item4, p.Item6);
                            p.Item5.Set();
                        }, parameters);

                    readsAligned++;
                }

                WaitHandle.WaitAll(manualResetEvents);
                transaction.Commit();
            }

            using (var session = database.SessionFactory.OpenSession())
            using (var file = File.Open(outFile, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                Dictionary<uint, List<byte[]>> positionsToReads = new Dictionary<uint, List<byte[]>>();

                uint[] maxInserted = new uint[1] {0};
                var parameters = new Tuple<ISession, Dictionary<uint, List<byte[]>>, uint[]>(session, positionsToReads,
                                                                                             maxInserted);
                ThreadPool.QueueUserWorkItem(o =>
                    {
                        var p = o as Tuple<ISession, Dictionary<uint, List<byte[]>>, uint[]>;
                        var iSession = p.Item1;
                        var dict = p.Item2;
                        var max = p.Item3;
                        for (int i = 0; i < SuffixArray.Length - 1; i += 100000)
                        {
                            int i1 = i;
                            var lookup = LinqExtensionMethods.Query<Alignment>(iSession)
                                                             .Where(a => a.Position >= i1 && a.Position < i1 + 100000)
                                                             .ToLookup(a => a.Position, a => a.ShortRead);
                            var keys = lookup.Select(g => g.Key).ToArray();
                            Array.Sort(keys);
                            foreach (var key in keys)
                            {
                                lock (dict)
                                {
                                    dict.Add(key, lookup[key].ToList());
                                    lock (max)
                                    {
                                        max[0] = key;
                                    }
                                }
                            }
                        }

                        max[0] = uint.MaxValue;
                    }, parameters);

                for (long textIndex = 0; textIndex < SuffixArray.Length - 1; textIndex++)
                {
                    lock (positionsToReads)
                    {
                        positionsToReads.Remove((uint) (textIndex - ReadLength));
                    }

                    if (textIndex%50000 == 0)
                    {
                        Console.WriteLine(textIndex);
                    }
                    int[] characterCounts = new int[4];
                    for (uint i = (uint) textIndex; i > textIndex - ReadLength && i > 0; i--)
                    {
                        List<byte[]> reads;
                        uint i1 = i;
                        SpinWait.SpinUntil(() =>
                            {
                                lock (maxInserted)
                                {
                                    return maxInserted[0] >= i1;
                                }
                            });
                        bool gotValue;
                        lock (positionsToReads)
                        {
                            gotValue = positionsToReads.TryGetValue(i, out reads);
                        }
                        if (gotValue)
                        {
                            foreach (var read in reads)
                            {
                                DnaSequence sequence = new DnaSequence(read, ReadLength);
                                var character = sequence[textIndex - i];
                                characterCounts[character]++;
                            }
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

                    writer.Write(finalCharacter);
                }
            }
        }

        public void AddAlignmentsToDictionary(DnaSequence shortRead, int allowedDifferences, Random rng,
                                              IStatelessSession session, long readId)
        {
            var minDifferences = CalculateMinimumDifferences(shortRead);
            var maxDiff = allowedDifferences < minDifferences[minDifferences.Length - 1]
                              ? allowedDifferences
                              : minDifferences[minDifferences.Length - 1];
            var sufBounds =
                GetSuffixArrayBounds(shortRead, (int)shortRead.Length - 1, maxDiff, minDifferences, 0,
                                     (uint) (SuffixArray.Length - 1)).ToArray();

            if (sufBounds.Any())
            {
                var indices = new HashSet<uint>();
                foreach (var sufBound in sufBounds)
                {
                    for (uint saIndex = sufBound.Item1; saIndex <= sufBound.Item2; saIndex++)
                        indices.Add(saIndex);
                }
                var indicesArray = indices.ToArray();
                var randomIndex = rng.Next(indicesArray.Count());
                var textPos = (uint) SuffixArray[indicesArray[randomIndex]];
                Alignment alignment = new Alignment() {Id = readId, Position = textPos, ShortRead = shortRead.Bytes};
                lock (session)
                {
                    session.Insert(alignment);
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

        private IEnumerable<SufBounds> GetSuffixArrayBounds(DnaSequence shortRead, int shortReadIndex, int allowedDiff,
                                                            byte[] minDiffs, uint minIndex,
                                                            uint maxIndex)
        {
            if (shortReadIndex < 0)
                return new List<SufBounds>
                    {
                        new SufBounds(minIndex, maxIndex, allowedDiff)
                    };
            if (allowedDiff < minDiffs[shortReadIndex])
                return new List<SufBounds>();


            List<SufBounds> alignments = new List<SufBounds>();
            //var deletionAlignments = GetSuffixArrayBounds(shortRead, i - 1, allowedDiff - 1, minDiffs, minSaIndex, maxSaIndex);
            //alignments = alignments.Union(deletionAlignments);

            for (uint dnaBase = 0; dnaBase < 4; dnaBase++)
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
                        var matchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1, allowedDiff,
                                                                     minDiffs, minSaIndex,
                                                                     maxSaIndex);
                        alignments.AddRange(matchedAlignments);
                    }
                    else
                    {
                        if (allowedDiff > 0)
                        {
                            var mismatchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1,
                                                                            allowedDiff - 1, minDiffs,
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