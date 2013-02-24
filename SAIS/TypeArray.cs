using System.Collections.Generic;
using System.Text;

namespace CS124Project.SAIS
{
    class TypeArray
    {
        private readonly byte[] _types;

        /// <summary>
        /// Map of character to number of times that character appears in text
        /// </summary>
        private readonly Dictionary<uint, uint> _bucketSizes = new Dictionary<uint, uint>();

        public uint[] BucketSizes
        {
            get
            {
                uint[] sizeArray = new uint[_bucketSizes.Count];
                for (uint bucketIndex = 0; bucketIndex < _bucketSizes.Count; bucketIndex++)
                {
                    uint bucketSize;
                    _bucketSizes.TryGetValue(bucketIndex, out bucketSize);
                    sizeArray[bucketIndex] = bucketSize;
                }
                
                return sizeArray;
            }
        }

        public uint[] BucketIndices
        {
            get
            {
                uint[] bucketIndices = new uint[_bucketSizes.Count];
                bucketIndices[0] = 0;
                for (uint bucketIndex = 1; bucketIndex < _bucketSizes.Count; bucketIndex++)
                {
                    uint previousBucketSize;
                    _bucketSizes.TryGetValue(bucketIndex - 1, out previousBucketSize);
                    bucketIndices[bucketIndex] = bucketIndices[bucketIndex-1] + previousBucketSize;
                }

                return bucketIndices;
            }
        }

        public string TypesAsString 
        { 
            get
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < 10 && i < _types.Length*8; i++)
                {
                    builder.Append(this[i] == SaisType.L ? 'L' : 'S');
                }

                return builder.ToString();
            }
        }

        public TypeArray(ISaisString text)
        {
            _types = new byte[text.Length/8+1];

            // Scan backwards
            // Also, keep track of bucket sizes here so we don't have to do it later, since we are scanning whole text
            SaisType succeedingType = SaisType.S;
            long succeedingCharacter = 0; /* succeding as in it comes after the currentCharacter in the text.
                                            * since we are iterating backwards, setting it to the sentinel */
            for (long index = text.Length-1; index >= 0; index--)
            {
                uint currentCharacter = text[(uint) index];

                uint bucketSize;
                if (_bucketSizes.TryGetValue(currentCharacter, out bucketSize))
                {
                    _bucketSizes[currentCharacter] = _bucketSizes[currentCharacter] + 1;
                }
                else
                {
                    _bucketSizes.Add(currentCharacter, 1);
                }

                if (currentCharacter < succeedingCharacter
                    || (currentCharacter == succeedingCharacter && succeedingType == SaisType.S))
                {
                    this[index] = SaisType.S;
                    succeedingType = SaisType.S;
                }
                else
                {
                    this[index] = SaisType.L;
                    succeedingType = SaisType.L;
                }

                succeedingCharacter = currentCharacter;
            }
        }

        public SaisType this[long index]
        {
            get
            {
                byte typeByte = _types[index / 8];
                SaisType type = (SaisType)((typeByte >> (int)(index % 8)) & 0x1);
                return type;
            }

            set
            {
                byte mask = (byte)(1 << (int)(index % 8));

                // Clear to 0 if L, set to 1 if S
                // Type S is considered bigger than type L
                if (value == SaisType.L)
                    _types[index / 8] &= (byte)~mask;
                else
                    _types[index / 8] |= mask;
            }
        }
    }
}