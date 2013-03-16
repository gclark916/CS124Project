using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CS124Project.Dna;
using CS124Project.Sais;
using CS124Project.Sequencing.Database;
using NHibernate;
using NHibernate.Linq;

namespace CS124Project.Sequencing
{
    internal class Sequencer
    {
        private readonly uint[] _c;
        private readonly AlignmentDatabase _database;
        private readonly OccurrenceArray[] _occurrences;
        private readonly OccurrenceArray[] _occurrencesRev;
        private readonly CompressedSuffixArray _suffixArray;
        private readonly CompressedSuffixArray _suffixArrayRev;
        private readonly int _threadCount;

        public Sequencer(uint[] c, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev)
            : this(c, occurrences, occurrencesRev,
                   suffixArray, suffixArrayRev, Environment.ProcessorCount + 2)
        {
        }

        public Sequencer(uint[] c, OccurrenceArray[] occurrences, OccurrenceArray[] occurrencesRev,
                          CompressedSuffixArray suffixArray, CompressedSuffixArray suffixArrayRev,
                          int threadCount)
        {
            _c = c;
            _occurrences = occurrences;
            _occurrencesRev = occurrencesRev;
            _suffixArray = suffixArray;
            _suffixArrayRev = suffixArrayRev;
            _database = new AlignmentDatabase("temp.db");
            _threadCount = threadCount;
        }

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

            OccurrenceArray[] occs = OccurrenceArray.CreateOccurrenceArrays(bwt);
            OccurrenceArray.WriteToFile(baseFileName + "_rev.occ", occs);
        }

        public static Sequencer CreateBwtAlignerFromFiles(string baseFileName, int readLength)
        {
            DnaBwt bwt = DnaBwt.ReadFromFile(baseFileName + ".bwt");
            uint[] c = ReadCFromFile(baseFileName + ".c");
            OccurrenceArray[] occ = OccurrenceArray.CreateFromFile(baseFileName + ".occ", bwt);
            CompressedSuffixArray csa = CompressedSuffixArray.CreateFromFile(baseFileName + ".csa", bwt, occ, c);

            DnaBwt bwtRev = DnaBwt.ReadFromFile(baseFileName + "_rev.bwt");
            OccurrenceArray[] occRev = OccurrenceArray.CreateFromFile(baseFileName + "_rev.occ", bwtRev);
            CompressedSuffixArray csaRev = CompressedSuffixArray.CreateFromFile(baseFileName + "_rev.csa", bwtRev,
                                                                                occRev, c);

            Sequencer aligner = new Sequencer(c, occ, occRev, csa, csaRev, readLength);
            return aligner;
        }

        public void AlignReads(string readsFile)
        {
            using (var session = _database.SessionFactory.OpenStatelessSession())
            using (var transaction = session.BeginTransaction())
            using (var file = File.OpenRead(readsFile))
            {
                var reader = new BinaryReader(file);

                var readLength = reader.ReadInt32();
                var numReads = (file.Length - 4) / DnaSequence.ByteArrayLength(readLength);

                Metadata metadata = new Metadata()
                    {
                        Id = 1,
                        GenomeLength = _suffixArray.Length - 1,
                        ReadLength = readLength
                    };
                session.Insert(metadata);

                Random[] randoms = new Random[_threadCount];
                var manualResetEvents = new ManualResetEvent[_threadCount];

                bool[] doneReading = {false};
                Queue<byte[]> queue = new Queue<byte[]>();
                long[] readsAligned = {0};
                ManualResetEvent queueReadyForMore = new ManualResetEvent(true);

                for (int i = 0; i < manualResetEvents.Length; i++)
                {
                    randoms[i] = new Random();
                    manualResetEvents[i] = new ManualResetEvent(false);

                    var i1 = i;
                    ThreadPool.QueueUserWorkItem(o =>
                        {
                            while (Volatile.Read(ref readsAligned[0]) != numReads)
                            {
                                byte[] shortReadBytes;
                                long readId;
                                lock (queue)
                                {
                                    if (!queue.Any())
                                        continue;
                                    shortReadBytes = queue.Dequeue();
                                    if (queue.Count < 20000)
                                        queueReadyForMore.Set();
                                    readId = Volatile.Read(ref readsAligned[0]);
                                    Interlocked.Increment(ref readsAligned[0]);
                                }
                                var shortRead = new DnaSequence(shortReadBytes, readLength);
                                AddAlignmentsToDatabase(shortRead, 2, randoms[i1], session, readId);
                                if (readId%20000 == 0)
                                {
                                    Console.WriteLine(readId);
                                    GC.Collect();
                                }
                            }

                            manualResetEvents[i1].Set();
                        });

                    readsAligned[0]++;
                }

                long readsRead = 0;
                while (readsRead < numReads)
                {
                    byte[] shortReadBytes = reader.ReadBytes(DnaSequence.ByteArrayLength(readLength));
                    queueReadyForMore.WaitOne();

                    lock (queue)
                    {
                        queue.Enqueue(shortReadBytes);
                        if (queue.Count > 40000)
                            queueReadyForMore.Reset();
                    }
                    readsRead++;
                }

                lock (doneReading)
                {
                    doneReading[0] = true;
                }
                WaitHandle.WaitAll(manualResetEvents);
                transaction.Commit();
            }
        }

        public void ConstructGenome(string outFile)
        {
            int readLength;
            long genomeLength;
            using (var session = _database.SessionFactory.OpenStatelessSession())
            {
                var metadata = session.Query<Metadata>().First();
                readLength = metadata.ReadLength;
                genomeLength = metadata.GenomeLength;
            }
            using (var file = File.Open(outFile, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                var positionsToReads = new Dictionary<uint, byte[][]>();
                long maxInserted = 0;

                ThreadPool.QueueUserWorkItem(o =>
                    {
                        Queue<ILookup<uint, byte[]>> queue = new Queue<ILookup<uint, byte[]>>();
                        long[] maxQueried = {0};
                        ManualResetEvent enqueueEvent = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(o2 =>
                            {
                                using (var session = _database.SessionFactory.OpenStatelessSession())
                                {
                                    const long readInterval = 1000000;
                                    for (long i = 0; i < genomeLength; i += readInterval)
                                    {
                                        long i1 = i;
                                        var lookup = LinqExtensionMethods.Query<Alignment>(session)
                                                                         .Where(
                                                                             a =>
                                                                             a.Position >= i1 &&
                                                                             a.Position < i1 + readInterval)
                                                                         .ToLookup(a => a.Position, a => a.ShortRead);
                                        lock (queue)
                                        {
                                            queue.Enqueue(lookup);
                                            Interlocked.Add(ref maxQueried[0], readInterval);
                                            enqueueEvent.Set();
                                        }
                                    }
                                }

                                Interlocked.Exchange(ref maxQueried[0], long.MaxValue);
                            });

                        while (Volatile.Read(ref maxQueried[0]) != long.MaxValue)
                        {
                            ILookup<uint, byte[]> lookup;
                            enqueueEvent.WaitOne();
                            lock (queue)
                            {
                                lookup = queue.Dequeue();
                                enqueueEvent.Reset();
                            }
                            var keys = lookup.Select(g => g.Key).ToArray();
                            Array.Sort(keys);
                            foreach (var key in keys)
                            {
                                lock (positionsToReads)
                                {
                                    positionsToReads.Add(key, lookup[key].ToArray());
                                    Interlocked.Exchange(ref maxInserted, key);
                                }
                            }
                        }

                        while (queue.Any())
                        {
                            ILookup<uint, byte[]> lookup = queue.Dequeue();
                            var keys = lookup.Select(g => g.Key).ToArray();
                            Array.Sort(keys);
                            foreach (var key in keys)
                            {
                                lock (positionsToReads)
                                {
                                    positionsToReads.Add(key, lookup[key].ToArray());
                                    Interlocked.Exchange(ref maxInserted, key);
                                }
                            }
                        }

                        Interlocked.Exchange(ref maxInserted, long.MaxValue);
                    });

                for (long textIndex = 0; textIndex < genomeLength; textIndex++)
                {
                    lock (positionsToReads)
                    {
                        positionsToReads.Remove((uint)(textIndex - readLength));
                    }

                    if (textIndex%100000 == 0)
                    {
                        Console.WriteLine(textIndex);
                    }

                    int[] characterCounts = new int[4];
                    for (uint i = (uint)textIndex; i > textIndex - readLength && i > 0; i--)
                    {
                        uint i1 = i;
                        SpinWait.SpinUntil(() => Volatile.Read(ref maxInserted) >= i1);
                        bool gotValue;
                        byte[][] reads;
                        lock (positionsToReads)
                        {
                            gotValue = positionsToReads.TryGetValue(i, out reads);
                        }
                        if (!gotValue) continue;

                        foreach (var read in reads)
                        {
                            DnaSequence sequence = new DnaSequence(read, readLength);
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

        public void AddAlignmentsToDatabase(DnaSequence shortRead, int allowedDifferences, Random rng,
                                            IStatelessSession session, long readId)
        {
            uint revMinSAIndex, revMaxSAIndex;
            var minDifferences = CalculateMinimumDifferences(shortRead, out revMinSAIndex, out revMaxSAIndex);
            if (minDifferences[minDifferences.Length - 1] > allowedDifferences)
                return;
            if (minDifferences[minDifferences.Length - 1] == 0)
            {
                var randomIndex = revMinSAIndex + rng.Next((int) (revMaxSAIndex+1-revMinSAIndex));
                var textPos = (uint)(_suffixArray.Length - 1 - _suffixArrayRev[randomIndex] - shortRead.Length);
                Alignment alignment = new Alignment {Id = readId, Position = textPos, ShortRead = shortRead.Bytes};
                lock (session)
                {
                    session.Insert(alignment);
                }
            }
            else
            {
                var sufBounds =
                    GetSuffixArrayBoundsSansRecursion(shortRead, allowedDifferences, minDifferences);

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
                    var textPos = (uint) _suffixArray[indicesArray[randomIndex]];
                    Alignment alignment = new Alignment {Id = readId, Position = textPos, ShortRead = shortRead.Bytes};
                    lock (session)
                    {
                        session.Insert(alignment);
                    }
                }
            }
        }

        /// <summary>
        ///     Calculates a loose bound of the minimum number of differences needed to align shortRead[0..i]
        /// </summary>
        /// <param name="?"></param>
        /// <param name="shortRead"></param>
        /// <param name="minSAIndex"></param>
        /// <param name="maxSAIndex"></param>
        private byte[] CalculateMinimumDifferences(DnaSequence shortRead, out uint minSAIndex, out uint maxSAIndex)
        {
            minSAIndex = 1;
            maxSAIndex = (uint) (_suffixArrayRev.Length - 1);
            byte differences = 0;
            byte[] minDifferences = new byte[shortRead.Length];
            for (uint readIndex = 0;
                 readIndex < shortRead.Length;
                 readIndex++)
            {
                var character = shortRead[readIndex];
                minSAIndex = (uint)(_c[character] + _occurrencesRev[character][minSAIndex - 1] + 1);
                maxSAIndex = (uint) (_c[character] + _occurrencesRev[character][maxSAIndex]);
                if (minSAIndex > maxSAIndex)
                {
                    minSAIndex = 1;
                    maxSAIndex = (uint) (_suffixArrayRev.Length - 1);
                    differences++;
                }
                minDifferences[readIndex] = differences;
            }

            return minDifferences;
        }

        private List<System.Tuple<uint, uint>> GetSuffixArrayBoundsSansRecursion(DnaSequence shortRead, int allowedDiff,
                                                                                 byte[] minDiffs)
        {
            var maxHeap = new BinaryHeap<Tuple<int, int, uint, uint, int>>((x, y) => y.Item2.CompareTo(x.Item2));

            var curAllowedDiff = 0;
            var alignments = new List<System.Tuple<uint, uint>>();

            for (int dnaBase = 0; dnaBase < 4; dnaBase++)
            {
                if (dnaBase == shortRead[shortRead.Length - 1])
                {
                    uint minSAIndex = 0;
                    var maxSAIndex = (uint)(_suffixArray.Length - 1);
                    maxHeap.Add(new Tuple<int, int, uint, uint, int>((int)(shortRead.Length - 2), allowedDiff,
                                                                    minSAIndex, maxSAIndex, dnaBase));
                }
                else
                {
                    if (allowedDiff - 1 >= minDiffs[shortRead.Length - 1])
                    {
                        uint minSAIndex = 0;
                        var maxSAIndex = (uint)(_suffixArray.Length - 1);
                        maxHeap.Add(new Tuple<int, int, uint, uint, int>((int)(shortRead.Length - 2), allowedDiff-1,
                                                                        minSAIndex, maxSAIndex, dnaBase));
                    }
                }
            }

            while (maxHeap.Count > 0)
            {
                var tuple = maxHeap.Remove();
                int remainingAllowedDiff = tuple.Item2;
                if (remainingAllowedDiff < curAllowedDiff)
                    break;
                int readIndex = tuple.Item1;
                int dbase = tuple.Item5;
                uint minSAIndex = (uint) (_c[dbase] + _occurrences[dbase][tuple.Item3 - 1] + 1);
                uint maxSAIndex = (uint)(_c[dbase] + _occurrences[dbase][tuple.Item4]);
                if (minSAIndex > maxSAIndex)
                    continue;

                if (readIndex < 0)
                {
                    if (remainingAllowedDiff > curAllowedDiff)
                    {
                        alignments.Clear();
                        curAllowedDiff = remainingAllowedDiff;
                    }
                    alignments.Add(new System.Tuple<uint, uint>(minSAIndex, maxSAIndex));
                    continue;
                }

                for (int dnaBase = 0; dnaBase < 4; dnaBase++)
                {
                    if (dnaBase == shortRead[readIndex])
                    {
                        maxHeap.Add(new Tuple<int, int, uint, uint, int>(readIndex - 1, remainingAllowedDiff,
                                                                        minSAIndex, maxSAIndex, dnaBase));
                    }
                    else
                    {
                        if (remainingAllowedDiff - 1 >= minDiffs[readIndex])
                        {
                            maxHeap.Add(new Tuple<int, int, uint, uint, int>(readIndex - 1, remainingAllowedDiff - 1,
                                                                            minSAIndex, maxSAIndex, dnaBase));
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