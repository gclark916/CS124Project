﻿using System;
using System.IO;
using CS124Project.Genome;
using CS124Project.SAIS;

namespace CS124Project.BWT
{
    class DnaBwt
    {
        private readonly long _sentinelIndex;
        private readonly byte[] _bwt;
        public long Length { get; set; }

        public DnaBwt(byte[] bwt, long length, long sentinelIndex)
        {
            _bwt = bwt;
            Length = length;
            _sentinelIndex = sentinelIndex;
        }

        public DnaBwt(ISaisString text, SuffixArray suffixArray)
        {
            Length = text.Length;
            _bwt = new byte[Length / 4 + (Length % 4 != 0 ? 1 : 0)];

            for (long i = 1; i < suffixArray.Length; i++)
            {
                if (suffixArray[i] - 1 >= 0) continue;

                _sentinelIndex = i;
                break;
            }
            for (long i = 0; i < suffixArray.Length; i++)
            {
                var textIndex = suffixArray[i] - 1;
                if (textIndex < 0) continue;
                this[i] = text[textIndex] - 1; // ISaisString values range from 0 to 4, 0 being sentinel
            }
        }

        public void WriteToFile(string fileName)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                file.Write(BitConverter.GetBytes((uint)Length), 0, sizeof(uint));
                file.Write(BitConverter.GetBytes((uint)_sentinelIndex), 0, sizeof(uint));
                file.Write(_bwt, 0, _bwt.Length);
            }
        }

        public void WriteToTextFile(string fileName)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                file.Write(BitConverter.GetBytes((uint)_sentinelIndex), 0, sizeof(uint));
                for (int i = 0; i < Length; i++)
                {
                    switch (this[i])
                    {
                        case 0:
                            file.Write(BitConverter.GetBytes('A'), 0, 1);
                            break;
                        case 1:
                            file.Write(BitConverter.GetBytes('C'), 0, 1);
                            break;
                        case 2:
                            file.Write(BitConverter.GetBytes('G'), 0, 1);
                            break;
                        case 3:
                            file.Write(BitConverter.GetBytes('T'), 0, 1);
                            break;
                    }
                }
            }
        }

        public int this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                if (index == _sentinelIndex)
                    return -1;

                var adjustedIndex = index < _sentinelIndex ? index : index - 1;

                var bwtByte = _bwt[adjustedIndex / 4];
                var shift = (int)(2 * adjustedIndex % 4);
                var finalByte = (bwtByte >> shift) & 3;
                return finalByte;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                if (value < 0 || value >= 4)
                    throw new ArgumentOutOfRangeException();

                var adjustedIndex = index < _sentinelIndex ? index : index - 1;

                var bwtByte = _bwt[adjustedIndex / 4];
                var shift = (int)(2 * (index % 4));
                bwtByte &= (byte)~(3 << shift);
                var mask = value << shift;
                bwtByte |= (byte)mask;
                _bwt[adjustedIndex / 4] = bwtByte;
            }
        }

        public static DnaBwt ReadFromFile(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            {
                byte[] buffer = new byte[sizeof (uint)];
                file.Read(buffer, 0, sizeof (uint));
                var length = BitConverter.ToUInt32(buffer, 0);
                file.Read(buffer, 0, sizeof(uint));
                var sentinelIndex = BitConverter.ToUInt32(buffer, 0);

                byte[] bwtBuffer = new byte[length/4 + (length%4 != 0 ? 1 : 0)];
                file.Read(bwtBuffer, 0, bwtBuffer.Length);
                
                DnaBwt dnaBwt = new DnaBwt(bwtBuffer, length, sentinelIndex);
                return dnaBwt;
            }
        }
    }
}