using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.BWT
{
    class CompressedSuffixArray
    {
        private readonly uint[] _compressedSufArray;
        private readonly DnaBwt _bwt;
        private readonly OccurrenceArray[] _occ;
        private readonly uint[] _C;

        public CompressedSuffixArray(uint[] compressedSuffixArray, DnaBwt bwt, OccurrenceArray[] occ, uint[] c)
        {
            _compressedSufArray = compressedSuffixArray;
            _bwt = bwt;
            _occ = occ;
            _C = c;
            Length = bwt.Length;
        }

        public long this[long index]
        {
            get 
            { 
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                if (index%32 == 0)
                    return _compressedSufArray[index/32];

                if (_bwt[index] < 0)
                    return 0;

                var j = 1;
                var k = _C[_bwt[index]] + _occ[_bwt[index]][index];
                while (k%32 != 0)
                {
                    if (_bwt[k] == -1)
                        return j;
                    k = _C[_bwt[k]] + _occ[_bwt[k]][k];
                    j++;
                }

                return _compressedSufArray[k/32] + j;
            }
        }

        public long Length { get; private set; }

        public static CompressedSuffixArray CreateFromFile(string fileName, DnaBwt bwt, OccurrenceArray[] occ, uint[] C)
        {
            var buffer = File.ReadAllBytes(fileName);
            uint[] compressedSufArray = new uint[buffer.Length/4];
            for (int i = 0; i < compressedSufArray.Length; i++)
            {
                compressedSufArray[i] = BitConverter.ToUInt32(buffer, i*4);
            }

            CompressedSuffixArray csa = new CompressedSuffixArray(compressedSufArray, bwt, occ, C);
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
