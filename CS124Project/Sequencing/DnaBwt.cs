using System;
using System.IO;
using CS124Project.Sais;

namespace CS124Project.Sequencing
{
    internal class DnaBwt
    {
        private readonly byte[] _bwt;
        private readonly long _sentinelIndex;

        public DnaBwt(byte[] bwt, long length, long sentinelIndex)
        {
            _bwt = bwt;
            Length = length;
            _sentinelIndex = sentinelIndex;
        }

        public DnaBwt(ISaisString text, SuffixArray suffixArray)
        {
            Length = text.Length;
            _bwt = new byte[Length/4 + (Length%4 != 0 ? 1 : 0)];

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
                var character = (int)text[textIndex] - 1;
                this[i] = character; // ISaisString values range from 0 to 4, 0 being sentinel
            }
        }

        public DnaBwt(ISaisString text, LongSuffixArray suffixArray)
        {
            Length = text.Length;
            _bwt = new byte[Length / 4 + (Length % 4 != 0 ? 1 : 0)];

            for (long i = 1; i < suffixArray.Length; i++)
            {
                if (((long)suffixArray[i]) - 1 >= 0) continue;

                _sentinelIndex = i;
                break;
            }
            for (long i = 0; i < suffixArray.Length; i++)
            {
                var textIndex = ((long)suffixArray[i]) - 1;
                if (textIndex < 0) continue;
                var character = text[textIndex] - 1;
                this[i] = (int)character; // ISaisString values range from 0 to 4, 0 being sentinel
            }
        }

        public long Length { get; set; }

        public int this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                if (index == _sentinelIndex)
                    return -1;

                var adjustedIndex = index < _sentinelIndex ? index : index - 1;

                var bwtByte = _bwt[adjustedIndex/4];
                var shift = (int) (2*(adjustedIndex%4));
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

                var bwtByte = _bwt[adjustedIndex/4];
                var shift = (int)(2 * (adjustedIndex % 4));
                bwtByte &= (byte) ~(3 << shift);
                var mask = value << shift;
                bwtByte |= (byte) mask;
                _bwt[adjustedIndex/4] = bwtByte;
            }
        }

        public void WriteToFile(string fileName)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                writer.Write((uint) Length);
                writer.Write((uint) _sentinelIndex);
                writer.Write(_bwt);
            }
        }

        public void WriteToTextFile(string fileName)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                file.Write(BitConverter.GetBytes((uint) _sentinelIndex), 0, sizeof (uint));
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

        public static DnaBwt ReadFromFile(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            {
                var reader = new BinaryReader(file);
                var length = reader.ReadUInt32();
                var sentinelIndex = reader.ReadUInt32();

                byte[] bwt = reader.ReadBytes((int) (length/4 + (length%4 != 0 ? 1 : 0)));

                DnaBwt dnaBwt = new DnaBwt(bwt, length, sentinelIndex);
                return dnaBwt;
            }
        }
    }
}