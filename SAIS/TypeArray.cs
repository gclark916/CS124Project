using CS124Project.Genome;

namespace CS124Project.SAIS
{
    class TypeArray
    {
        private readonly byte[] _types;
        readonly uint[] _bucketSizes = { 0, 0, 0, 0 };

        public uint[] BucketSizes { get { return _bucketSizes; } }

        public TypeArray(ISaisString text)
        {
            _types = new byte[text.Length/8+1];

            // Scan backwards
            // Also, keep track of bucket sizes here so we don't have to do it later, since we are scanning whole text
            SaisType succeedingType = SaisType.S;
            long succeedingCharacter = -1; /* succeding as in it comes after the currentCharacter in the text.
                                            * since we are iterating backwards, setting it to the sentinel */
            SetType(text.Length, SaisType.S);
            for (long index = text.Length - 1; index >= 0; index--)
            {
                long currentCharacter = text.GetCharacter(index);
                _bucketSizes[currentCharacter] = _bucketSizes[currentCharacter] + 1;

                if (currentCharacter < succeedingCharacter
                    || (currentCharacter == succeedingCharacter && succeedingType == SaisType.S))
                {
                    SetType(index, SaisType.S);
                    succeedingType = SaisType.S;
                }
                else
                {
                    SetType(index, SaisType.L);
                    succeedingType = SaisType.L;
                }

                succeedingCharacter = currentCharacter;
            }
        }

        private void SetType(long index, SaisType type)
        {
            byte mask = (byte) ((int)type << (int)(index%8));

            // Clear to 0 if L, set to 1 if S
            // Type S is considered bigger than type L
            if (type == SaisType.L)
                _types[index / 8] &= (byte)~mask;
            else
                _types[index / 8] |= mask;
        }

        public SaisType GetType(long index)
        {
            byte typeByte = _types[index/8];
            SaisType type = (SaisType)((typeByte >> (int)(index%8)) & 0x1);
            return type;
        }

        public uint GetBucketSize(Base dnaBase)
        {
            return _bucketSizes[(int)dnaBase];
        }
    }
}