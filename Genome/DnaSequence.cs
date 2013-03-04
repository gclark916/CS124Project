using System;
using System.Text;

namespace CS124Project.Genome
{
    [System.Diagnostics.DebuggerDisplay("{DebugString()}")]
    class DnaSequence
    {
        private readonly byte[] _text;
        private readonly long _length;

        public byte[] Bytes { get { return _text; } }

        public DnaSequence(byte[] text, long length)
        {
            if (text.Length < length/4 + (length%4 != 0 ? 1 : 0))
                throw new ArgumentException("Byte array must be large enough to hold <length> base pairs");

            _text = text;
            _length = length;
        }

        public long Length { get { return _length; } }

        public static DnaSequence CreateGenomeFromString(string text)
        {
            var byteArrayLength = text.Length / 4 + (text.Length % 4 == 0 ? 0 : 1);
            byte[] binaryText = new byte[byteArrayLength];

            for (int byteIndex = 0; byteIndex < text.Length / 4; byteIndex++)
            {
                var stringIndex = byteIndex * 4;
                byte base0 = StringCharToBaseByte(text[stringIndex]);
                byte base1 = StringCharToBaseByte(text[stringIndex + 1]);
                byte base2 = StringCharToBaseByte(text[stringIndex + 2]);
                byte base3 = StringCharToBaseByte(text[stringIndex + 3]);

                byte baseByte = (byte)(base0 | (base1 << 2) | (base2 << 4) | (base3 << 6));
                binaryText[byteIndex] = baseByte;
            }

            // Add the last byte if necessary
            if (text.Length % 4 != 0)
            {
                byte finalByte = 0;
                for (int index = (text.Length / 4) * 4; index < text.Length; index++)
                {
                    byte baseByte = (byte)(StringCharToBaseByte(text[index]));
                    byte shiftedByte = (byte)(baseByte << (2 * (index % 4)));
                    finalByte |= shiftedByte;
                }
                binaryText[text.Length / 4] = finalByte;
            }

            return new DnaSequence(binaryText, text.Length);
        }

        protected static byte StringCharToBaseByte(char c)
        {
            switch (c)
            {
                case 'a':
                case 'A':
                    return 0;
                case 'c':
                case 'C':
                    return 1;
                case 'g':
                case 'G':
                    return 2;
                case 't':
                case 'T':
                    return 3;
                default:
                    return 0;
            }
        }

        public int this[long index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new IndexOutOfRangeException();

                var byteIndex = index / 4;
                int shift = (int)(2 * (index % 4));
                int baseByte = ((_text[byteIndex] >> shift) & 0x3);
                return baseByte;
            }
        }

        public string DebugString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Length && i < 30; i++)
            {
                switch (this[i])
                {
                    case 0:
                        builder.Append('A');
                        break;
                    case 1:
                        builder.Append('C');
                        break;
                    case 2:
                        builder.Append('G');
                        break;
                    case 3:
                        builder.Append('T');
                        break;
                }
            }

            return builder.ToString();
        }
    }
}