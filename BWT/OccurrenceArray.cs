using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project.BWT
{
    class OccurrenceArray
    {
        private const int CompressFactor = 128;
        private readonly int[] _compressedOcc;
        private readonly DnaBwt _bwt;
        private readonly int _dnaBase;

        public static OccurrenceArray[] CreateOccurrenceArrays(DnaBwt bwt)
        {
            OccurrenceArray[] occArrays = new OccurrenceArray[4];
            int[] sums = new int[4];
            for (int i = 0; i < bwt.Length; i++)
            {
                if (bwt[i] > 0)
                    sums[bwt[i]]++;

                if (i%CompressFactor == 0)
                {
                    occArrays[0][i] = sums[0];
                    occArrays[1][i] = sums[1];
                    occArrays[2][i] = sums[2];
                    occArrays[3][i] = sums[3];
                }
            }

            return occArrays;
        }

        protected OccurrenceArray(DnaBwt bwt, int dnaBase, int[] compressedOccurrences)
        {
            _compressedOcc = compressedOccurrences;
            _bwt = bwt;
            _dnaBase = dnaBase;
        }

        public int this[long index]
        {
            get 
            {
                var compressedIndex = index / CompressFactor;
                var occurrences = _compressedOcc[compressedIndex];
                for (var i = compressedIndex * CompressFactor; i < compressedIndex * CompressFactor + index % CompressFactor; i++)
                {
                    if (_bwt[i] == _dnaBase)
                        occurrences++;
                }
                return occurrences;
            }
            private set
            {
                Debug.Assert(index%CompressFactor == 0);
                _compressedOcc[index/CompressFactor] = value;
            }
        }

        public static void WriteToFile(string fileName, OccurrenceArray[] occ)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                for (int i = 0; i < occ[0]._compressedOcc.Length; i++)
                {
                    for (int dnaBase = 0; dnaBase < 4; dnaBase++)
                    {
                        var buffer = BitConverter.GetBytes(occ[dnaBase][i]);
                        file.Write(buffer, 0, sizeof(uint));
                    }
                }
            }
        }

        public static OccurrenceArray[] CreateFromFile(string fileName, DnaBwt bwt)
        {
            using (var file = File.OpenRead(fileName))
            {
                int[][] compressedOccs = new int[4][]
                    {new int[bwt.Length], new int[bwt.Length], new int[bwt.Length], new int[bwt.Length]};

                var buffer = new byte[sizeof (uint)];
                for (int i = 0; i < compressedOccs[0].Length; i++)
                {
                    for (int dnaBase = 0; dnaBase < 4; dnaBase++)
                    {
                        file.Read(buffer, 0, sizeof (uint));
                        compressedOccs[dnaBase][i] = BitConverter.ToInt32(buffer, 0);
                    }
                }

                OccurrenceArray[] occs = new OccurrenceArray[]
                    {
                        new OccurrenceArray(bwt, 0, compressedOccs[0]), 
                        new OccurrenceArray(bwt, 1, compressedOccs[1]), 
                        new OccurrenceArray(bwt, 2, compressedOccs[2]), 
                        new OccurrenceArray(bwt, 3, compressedOccs[3])
                    };

                return occs;
            }
        }
    }
}
