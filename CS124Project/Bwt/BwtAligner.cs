using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CS124Project.Bwt.Database;
using CS124Project.Dna;
using CS124Project.Sais;
using NHibernate;
using LinqExtensionMethods = NHibernate.Linq.LinqExtensionMethods;
using SufBounds = System.Tuple<uint, uint, int>;

namespace CS124Project.Bwt
{
    internal class BwtAligner
    {
        public BwtAligner(uint[] c, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev, int readLength)
        {
            _c = c;
            _occurrences = occurrences;
            _occurrencesRev = occurrencesRev;
            SuffixArray = suffixArray;
            SuffixArrayRev = suffixArrayRev;
            _readLength = readLength;
            _database = new AlignmentDatabase("temp.db");
        }

        private readonly uint[] _c;
        private readonly int _readLength;
        private readonly OccurrenceArray[] _occurrences;
        private readonly OccurrenceArray[] _occurrencesRev;
        public CompressedSuffixArray SuffixArray { get; set; }
        public CompressedSuffixArray SuffixArrayRev { get; set; }
        private readonly AlignmentDatabase _database;

        public static void SavePrecomputedDataToFiles(string baseFileName, DnaSequence referenceGenome)
        {
            Level0String level0String = new Level0String(referenceGenome);
            LongSuffixArray suffixArray = LongSuffixArray.CreateSuffixArray(level0String);
            suffixArray.WriteToFile(baseFileName + ".csa", 32);

            DnaBwt bwt = new DnaBwt(level0String, suffixArray);
            bwt.WriteToFile(baseFileName + ".bwt");
            WriteCToFile(baseFileName + ".c", bwt);

            OccurrenceArray[] occs = OccurrenceArray.CreateOccurrenceArrays(bwt);
            OccurrenceArray.WriteToFile(baseFileName + ".occ", occs);
        }

        public static void SaveReversePrecomputedDataToFiles(string baseFileName, DnaSequence reverseGenome)
        {
            Level0String level0String = new Level0String(reverseGenome);
            LongSuffixArray suffixArray = LongSuffixArray.CreateSuffixArray(level0String);
            suffixArray.WriteToFile(baseFileName + "_rev.csa", 32);

            DnaBwt bwt = new DnaBwt(level0String, suffixArray);
            bwt.WriteToFile(baseFileName + "_rev.bwt");

            OccurrenceArray[]  occs = OccurrenceArray.CreateOccurrenceArrays(bwt);
            OccurrenceArray.WriteToFile(baseFileName + "_rev.occ", occs);
        }

        public static BwtAligner CreateBwtAlignerFromFiles(string baseFileName, int readLength)
        {
            DnaBwt bwt = DnaBwt.ReadFromFile(baseFileName + ".bwt");
            uint[] c = ReadCFromFile(baseFileName + ".c");
            OccurrenceArray[] occ = OccurrenceArray.CreateFromFile(baseFileName + ".occ", bwt);
            CompressedSuffixArray csa = CompressedSuffixArray.CreateFromFile(baseFileName + ".csa", bwt, occ, c);

            DnaBwt bwtRev = DnaBwt.ReadFromFile(baseFileName + "_rev.bwt");
            OccurrenceArray[] occRev = OccurrenceArray.CreateFromFile(baseFileName + "_rev.occ", bwtRev);
            CompressedSuffixArray csaRev = CompressedSuffixArray.CreateFromFile(baseFileName + "_rev.csa", bwtRev,
                                                                                occRev, c);

            BwtAligner aligner = new BwtAligner(c, occ, occRev, csa, csaRev, readLength);
            return aligner;
        }

        public void AlignReadsAndConstructGenome(string readsFile, string outFile, bool construct)
        {
            var alignmentStart = DateTime.Now;
            var threadCount = 12;

            using (var session = _database.SessionFactory.OpenStatelessSession())
            using (var transaction = session.BeginTransaction())
            using (var file = File.OpenRead(readsFile))
            {
                var reader = new BinaryReader(file);
                var numReads = file.Length/DnaSequence.ByteArrayLength(_readLength);
                Random[] randoms = new Random[threadCount];
                var manualResetEvents = new ManualResetEvent[threadCount];
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
                    byte[] shortReadBytes = reader.ReadBytes(DnaSequence.ByteArrayLength(_readLength));
                    DnaSequence shortRead = new DnaSequence(shortReadBytes, _readLength);
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

            var alignmentEnd = DateTime.Now;

            if (construct)
            {
                ConstructGenome(outFile);
            }

            Console.WriteLine("Took {0} seconds to align, {1} seconds to construct", alignmentEnd.Subtract(alignmentStart).TotalSeconds, DateTime.Now.Subtract(alignmentEnd).TotalSeconds);
        }

        public void ConstructGenome(string outFile)
        {
            using (var session = _database.SessionFactory.OpenStatelessSession())
            using (var file = File.Open(outFile, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                var positionsToReads = new Dictionary<uint, byte[][]>();

                uint[] maxInserted = new uint[1] { 0 };
                var parameters = new Tuple<IStatelessSession, Dictionary<uint, byte[][]>, uint[]>(session,
                                                                                             positionsToReads,
                                                                                             maxInserted);
                ThreadPool.QueueUserWorkItem(o =>
                {
                    var p = o as Tuple<IStatelessSession, Dictionary<uint, byte[][]>, uint[]>;
                    Debug.Assert(p != null, "p != null");
                    var iSession = p.Item1;
                    var dict = p.Item2;
                    var max = p.Item3;
                    for (int i = 0; i < SuffixArray.Length - 1; i += 1000000)
                    {
                        int i1 = i;
                        var lookup = LinqExtensionMethods.Query<Alignment>(iSession)
                                                            .Where(
                                                                a => a.Position >= i1 && a.Position < i1 + 1000000)
                                                            .ToLookup(a => a.Position, a => a.ShortRead);
                        var keys = lookup.Select(g => g.Key).ToArray();
                        Array.Sort(keys);
                        foreach (var key in keys)
                        {
                            lock (dict)
                            {
                                dict.Add(key, lookup[key].ToArray());
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
                        positionsToReads.Remove((uint)(textIndex - _readLength));
                    }

                    if (textIndex % 50000 == 0)
                    {
                        Console.WriteLine(textIndex);
                    }
                    int[] characterCounts = new int[4];
                    for (uint i = (uint)textIndex; i > textIndex - _readLength && i > 0; i--)
                    {
                        byte[][] reads;
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
                        if (!gotValue) continue;

                        foreach (var read in reads)
                        {
                            DnaSequence sequence = new DnaSequence(read, _readLength);
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

                    writer.Write(finalCharacter);
                }
            }
        }

        public void AddAlignmentsToDictionary(DnaSequence shortRead, int allowedDifferences, Random rng,
                                              IStatelessSession session, long readId)
        {
            var minDifferences = CalculateMinimumDifferences(shortRead);
            if (minDifferences[minDifferences.Length - 1] > allowedDifferences)
                return;
            if (minDifferences[minDifferences.Length - 1] == 0)
                allowedDifferences = 0;
            var sufBounds =
                GetSuffixArrayBoundsSansRecursion(shortRead, (int)shortRead.Length - 1, allowedDifferences, minDifferences, 0,
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
                Alignment alignment = new Alignment {Id = readId, Position = textPos, ShortRead = shortRead.Bytes};
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
                    minSAIndex = _c[character] + 1;
                else
                    minSAIndex = (uint) (_c[character] + _occurrencesRev[character][minSAIndex - 1] + 1);
                maxSAIndex = (uint) (_c[character] + _occurrencesRev[character][maxSAIndex]);
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

        private List<SufBounds> GetSuffixArrayBounds(DnaSequence shortRead, int shortReadIndex, int allowedDiff,
                                                            byte[] minDiffs, uint minIndex,
                                                            uint maxIndex)
        {
            if (shortReadIndex < 0)
                return new List<SufBounds> { new SufBounds(minIndex, maxIndex, allowedDiff) };
            if (allowedDiff < minDiffs[shortReadIndex])
                return new List<SufBounds>();

            var alignments = new List<SufBounds>();
            var maxAllowedDiff = 0;
            for (int dnaBase = shortRead[shortReadIndex], i = 0; i < 4; i++, dnaBase = (shortRead[shortReadIndex]+i)%4)
            {
                uint minSaIndex = minIndex;
                uint maxSaIndex = maxIndex;
                if (minSaIndex == 0)
                    minSaIndex = _c[dnaBase] + 1;
                else
                    minSaIndex = (uint) (_c[dnaBase] + _occurrences[dnaBase][minSaIndex - 1] + 1);
                maxSaIndex = (uint) (_c[dnaBase] + _occurrences[dnaBase][maxSaIndex]);

                if (minSaIndex <= maxSaIndex)
                {
                    if (dnaBase == shortRead[shortReadIndex])
                    {
                        var matchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1, allowedDiff,
                                                                     minDiffs, minSaIndex,
                                                                     maxSaIndex);
                        if (matchedAlignments.Any())
                        {
                            alignments.AddRange(matchedAlignments);
                            maxAllowedDiff = alignments.First().Item3;
                        }
                    }
                    else
                    {
                        if (allowedDiff - 1 >= maxAllowedDiff)
                        {
                            var mismatchedAlignments = GetSuffixArrayBounds(shortRead, shortReadIndex - 1,
                                                                            allowedDiff - 1, minDiffs,
                                                                            minSaIndex, maxSaIndex);
                            if (mismatchedAlignments.Any() && mismatchedAlignments.First().Item3 >= maxAllowedDiff)
                            {
                                if (mismatchedAlignments.First().Item3 > maxAllowedDiff)
                                    alignments.Clear();
                                alignments.AddRange(mismatchedAlignments);
                            }
                        }
                    }
                }
            }

            return alignments;
        }

        private List<Tuple<uint, uint>> GetSuffixArrayBoundsSansRecursion(DnaSequence shortRead, int shortReadIndex, int allowedDiff,
                                                            byte[] minDiffs, uint minIndex,
                                                            uint maxIndex)
        {
            var stack = new Stack<Tuple<int, int, uint, uint>>();

            var curAllowedDiff = allowedDiff;
            var alignments = new List<Tuple<uint, uint>>();

            for (int dnaBase = 0; dnaBase < 4; dnaBase++)
            {
                if (dnaBase == shortRead[shortRead.Length - 1])
                {
                    var minSAIndex = _c[dnaBase] + 1;
                    var maxSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][SuffixArray.Length - 1]);
                    if (maxSAIndex >= minSAIndex)
                        stack.Push(new Tuple<int, int, uint, uint>((int) (shortRead.Length-2), allowedDiff, minSAIndex, maxSAIndex));
                }
                else
                {
                    if (allowedDiff - 1 >= minDiffs[shortRead.Length-1])
                    {
                        var minSAIndex = _c[dnaBase] + 1;
                        var maxSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][SuffixArray.Length - 1]);
                        if (maxSAIndex >= minSAIndex)
                            stack.Push(new Tuple<int, int, uint, uint>((int) (shortRead.Length-2), allowedDiff, minSAIndex, maxSAIndex));
                    }
                }
            }

            while (stack.Any())
            {
                var tuple = stack.Pop();
                int readIndex = tuple.Item1;
                int remainingAllowedDiff = tuple.Item2;
                uint minSAIndex = tuple.Item3;
                uint maxSAIndex = tuple.Item4;
                if (readIndex < 0)
                {
                    if (remainingAllowedDiff >= curAllowedDiff)
                    {
                        if (remainingAllowedDiff > curAllowedDiff)
                        {
                            alignments.Clear();
                            curAllowedDiff = remainingAllowedDiff;
                        }
                        alignments.Add(new Tuple<uint, uint>(minSAIndex, maxSAIndex));
                    }
                    continue;
                }

                for (int dnaBase = 0; dnaBase < 4; dnaBase++)
                {
                    if (dnaBase == shortRead[readIndex])
                    {
                        var newMinSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][minSAIndex - 1] + 1);
                        var newMaxSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][maxSAIndex]);
                        if (newMaxSAIndex >= newMinSAIndex)
                            stack.Push(new Tuple<int, int, uint, uint>(readIndex-1, remainingAllowedDiff, newMinSAIndex, newMaxSAIndex));
                    }
                    else
                    {
                        if (remainingAllowedDiff - 1 >= minDiffs[readIndex])
                        {
                            var newMinSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][minSAIndex - 1] + 1);
                            var newMaxSAIndex = (uint)(_c[dnaBase] + _occurrences[dnaBase][maxSAIndex]);
                            if (newMaxSAIndex >= newMinSAIndex)
                                stack.Push(new Tuple<int, int, uint, uint>(readIndex - 1, remainingAllowedDiff-1, newMinSAIndex, newMaxSAIndex));
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
                uint[] c = new uint[4];
                for (long i = 0; i < bwt.Length; i++)
                {
                    if (bwt[i] < 0)
                        continue;

                    c[bwt[i]]++;
                }

                c[3] = c[0] + c[1] + c[2];
                c[2] = c[0] + c[1];
                c[1] = c[0];
                c[0] = 0;

                var writer = new BinaryWriter(file);
                for (int i = 0; i < 4; i++)
                {
                    writer.Write(c[i]);
                }
            }
        }

        public static uint[] ReadCFromFile(string fileName)
        {
            uint[] c = new uint[4];
            using (var file = File.OpenRead(fileName))
            {
                var reader = new BinaryReader(file);
                c[0] = reader.ReadUInt32();
                c[1] = reader.ReadUInt32();
                c[2] = reader.ReadUInt32();
                c[3] = reader.ReadUInt32();
            }
            return c;
        }
    }
}