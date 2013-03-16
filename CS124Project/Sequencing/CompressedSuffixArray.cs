using System;
using System.IO;

namespace CS124Project.Sequencing
{
    class CompressedSuffixArray
    {
        private const int CompressionFactor = 32;
        private readonly uint[] _baseArray;
        private readonly DnaBwt _bwt;
        private readonly OccurrenceArray[] _occ;
        private readonly uint[] _c;

        public CompressedSuffixArray(uint[] compressedSuffixArray, DnaBwt bwt, OccurrenceArray[] occ, uint[] c)
        {
            _baseArray = compressedSuffixArray;
            _bwt = bwt;
            _occ = occ;
            _c = c;
            Length = bwt.Length;
        }

        public long this[long index]
        {
            get 
            { 
                //if (index < 0 || index >= Length)
                //    throw new IndexOutOfRangeException();

                if (index % CompressionFactor == 0)
                    return _baseArray[index / CompressionFactor];

                if (_bwt[index] < 0)
                    return 0;

                var j = 1;
                var k = _c[_bwt[index]] + _occ[_bwt[index]][index];
                while (k % CompressionFactor != 0)
                {
                    if (_bwt[k] == -1)
                        return j;
                    k = _c[_bwt[k]] + _occ[_bwt[k]][k];
                    j++;
                }

                return _baseArray[k / CompressionFactor] + j;
            }
        }

        public long Length { get; private set; }

        public static CompressedSuffixArray CreateFromFile(string fileName, DnaBwt bwt, OccurrenceArray[] occ, uint[] c)
        {
            var buffer = File.ReadAllBytes(fileName);
            uint[] compressedSufArray = new uint[buffer.Length/4];
            for (int i = 0; i < compressedSufArray.Length; i++)
            {
                compressedSufArray[i] = BitConverter.ToUInt32(buffer, i*4);
            }

            CompressedSuffixArray csa = new CompressedSuffixArray(compressedSufArray, bwt, occ, c);
            return csa;
        }

        public void WriteToTextFile(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                for (int i = 0; i < Length; i++)
                {
                    writer.Write(String.Format("{0}\n", this[i]));
                }
            }
        }
    }
}
