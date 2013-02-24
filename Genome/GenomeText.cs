namespace CS124Project.Genome
{
    class GenomeText : IGenomeText
    {
        private readonly byte[] _text;
        private readonly long _length;

        public byte[] Bytes { get { return _text; } }

        public GenomeText(byte[] text, long length)
        {
            _text = text;
            _length = length;
        }

        public long Length { get { return _length; } }

        public virtual Base GetBase(long index)
        {
            if (index >= _length)
                return Base.Sentinel;
            var byteIndex = index / 4;
            int shift = (int) (2 * ((index) % 4));
            int baseByte = ((_text[byteIndex] >> shift) & 0x3);
            return (Base) baseByte;
        }

        public static GenomeText CreateGenomeFromString(string text)
        {
            var byteArrayLength = text.Length/4 + (text.Length%4 == 0 ? 0 : 1);
            byte[] binaryText = new byte[byteArrayLength];

            for (int byteIndex = 0; byteIndex < text.Length / 4; byteIndex++)
            {
                var stringIndex = byteIndex*4;
                byte base0 = StringCharToBaseByte(text[stringIndex]);
                byte base1 = StringCharToBaseByte(text[stringIndex + 1]);
                byte base2 = StringCharToBaseByte(text[stringIndex + 2]);
                byte base3 = StringCharToBaseByte(text[stringIndex + 3]);

                byte baseByte = (byte) (base0 | (base1 << 2) | (base2 << 4) | (base3 << 6));
                binaryText[byteIndex] = baseByte;
            }

            // Add the last byte if necessary
            if (text.Length%4 != 0)
            {
                byte finalByte = 0;
                for (int index = (text.Length/4)*4; index < text.Length; index++)
                {
                    byte baseByte = (byte) (StringCharToBaseByte(text[index]));
                    byte shiftedByte = (byte) (baseByte << (2 * (index%4)));
                    finalByte |= shiftedByte;
                }
                binaryText[text.Length/4] = finalByte;
            }

            return new GenomeText(binaryText, (uint) text.Length);
        }

        protected static byte StringCharToBaseByte(char c)
        {
            switch (c)
            {
                case 'a':
                case'A':
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

        public IGenomeText SubString(long offset, long length)
        {
            return new SubGenomeText(this, offset, length);
        }
    }

    internal class SubGenomeText : IGenomeText
    {
        private readonly long _offset;
        private readonly long _length;
        private readonly GenomeText _parentGenome;

        public long Length { get { return _length; } }

        public IGenomeText SubString(long offset, long length)
        {
            return new SubGenomeText(_parentGenome, _offset + offset, length);
        }

        public SubGenomeText(GenomeText parent, long offset, long length)
        {
            _parentGenome = parent;
            _length = length;
            _offset = offset;
        }

        public Base GetBase(long index)
        {
            if (index >= _length)
                return Base.Sentinel;

            return _parentGenome.GetBase(index + _offset);
        }
    }

    internal interface IGenomeText
    {
        Base GetBase(long index);
        long Length { get; }
        IGenomeText SubString(long offset, long length);
    }

    internal enum Base
    {
        Sentinel = -1, A = 0, C = 1, G = 2, T = 3
    }
}
