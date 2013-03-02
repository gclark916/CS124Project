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
        private uint[] _compressedSufArray;
        private DnaBwt _bwt;
        private OccurrenceArray[] _occ;
        private uint[] _C;

        public CompressedSuffixArray(uint[] compressedSuffixArray, DnaBwt bwt, OccurrenceArray[] occ, uint[] c)
        {
            _compressedSufArray = compressedSuffixArray;
            _bwt = bwt;
            _occ = occ;
            _C = c;
        }

        public long this[long index]
        {
            get 
            { 
                var j = 0;
                var k = _C[(int) _bwt[index]] + _occ[(int) _bwt[index]][index];
                while (k%32 != 0)
                {
                    k = _C[(int)_bwt[k]] + _occ[(int)_bwt[k]][k];
                    j++;
                }

                return _compressedSufArray[k/32] + j;
            }
        }

        public uint Length { get; private set; }

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
    }
}
