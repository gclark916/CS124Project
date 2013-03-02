namespace CS124Project.Genome
{
    class DnaSequence
    {
        private readonly byte[] _text;
        private readonly long _length;

        public byte[] Bytes { get { return _text; } }

        public DnaSequence(byte[] text, long length)
        {
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
                var byteIndex = index / 4;
                int shift = (int)(2 * ((index) % 4));
                int baseByte = ((_text[byteIndex] >> shift) & 0x3);
                return baseByte;
            }
        }
    }
}