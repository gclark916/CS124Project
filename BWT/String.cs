using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.BWT
{
    class String
    {
        private ulong _mask;
        private byte[] _string; 
        private byte[] _types;  // Indicates whether each character is S-Type or L-Type
        private byte _bitsPerCharacter;
        private uint _length;
        private List<LmsString> LmsStrings;

        public String(byte[] bitstring, byte bitsPerCharacter, uint length)
        {
            _string = bitstring;
            _bitsPerCharacter = bitsPerCharacter;
            _length = length;
            _mask = 0xFFFFFFFFFFFFFFFF >> (64 - _bitsPerCharacter);

            uint typesLengthInBytes = (uint) ((_length/8) + (_length%8 > 0 ? 1 : 0));
            _types = new byte[typesLengthInBytes];

            long lastValue = long.MaxValue;
            var lmsCharacterIndices = new List<uint>();
            for (uint i = _length - 1; i != long.MaxValue; i--) // TODO should probably stop at 0
            {
                long value = GetCharacterValue(i);
                if (value < lastValue || (value == lastValue && GetCharacterType(i+1) == Type.S))
                {
                    SetCharacterType(i, Type.S);
                    if (i != 0 && GetCharacterValue(i - 1) > value)
                        lmsCharacterIndices.Add(i);
                }
                else
                    SetCharacterType(i, Type.L);
                lastValue = value;
            }

            LmsStrings = new List<LmsString>();
            for (int i = 0; i < lmsCharacterIndices.Count - 1; i++)
            {
                LmsString lmsString = new LmsString(this, lmsCharacterIndices[i], lmsCharacterIndices[i+1]);
                LmsStrings.Add(lmsString);
            }
        }

        public Character GetCharacter(uint index)
        {
            return new Character(this, index);
        }

        public long GetCharacterValue(uint index)
        {
            if (index == _length)
                return -1;

            ulong bitIndex = index * _bitsPerCharacter;
            int firstByteIndex = (int)(bitIndex / 8);
            int shift = (int)(bitIndex%8);
            ulong value = BitConverter.ToUInt64(_string, firstByteIndex);
            value >>= shift;
            value &= _mask;
            return (long)value;
        }

        public Type GetCharacterType(uint index)
        {
            if (index == _length)
                return Type.Sentinel;

            int byteIndex = (int)(index/8);
            int shift = (int) (byteIndex%8);
            byte value = (byte)(_types[byteIndex] >> shift);
            value &= 0x1;
            Type type = value == 1 ? Type.S : Type.L;
            return type;
        }

        private void SetCharacterType(uint index, Type type)
        {
            int byteIndex = (int)(index / 8);
            int shift = (int) (byteIndex % 8);
            byte setMask = (byte)(0x1 << shift);

            // Clear to 0 for L-type, set to 1 if S-type
            if (type == Type.L)
                _types[byteIndex] &= (byte)~setMask;
            else
                _types[byteIndex] |= setMask;
        }
    }

    internal class LmsString
    {
        private String _string;
        private uint _beginIndex;
        private uint _endIndex;

        public LmsString(String parentString, uint beginIndex, uint endIndex)
        {
            _string = parentString;
            _beginIndex = beginIndex;
            _endIndex = endIndex;
        }
    }

    internal class Character
    {
        private readonly String _string;
        private readonly uint _index;

        public Character(String parentString, uint index)
        {
            _string = parentString;
            _index = index;
        }
        public Type Type
        {
            get { return _string.GetCharacterType(_index); }
        }

        public long Value
        {
            get { return _string.GetCharacterValue(_index); }
        }
    }

    internal enum Type
    {
        Sentinel, S, L
    }
}
