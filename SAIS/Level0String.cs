using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project.SAIS
{
    class Level0String : ISaisString
    {
        private readonly byte[] _text;
        private readonly long _length;
        private readonly TypeArray _types;
        public long Length { get { return _length; } }

        public Level0String(byte[] text, long length)
        {
            _text = text;
            _length = length;
            _types = new TypeArray(this);
        }

        public long GetCharacter(long index)
        {
            if (index >= _length)
                return -1;

            byte charByte = _text[index/4];
            long character = (charByte >> (int) (index%4)) & 0x11;
            return character;
        }

        public SaisType GetCharacterType(long index)
        {
            SaisType characterType = _types.GetType(index);
        }

        public uint GetBucketSize(Base dnaBase)
        {
            return _types.GetBucketSize(dnaBase);
        }
    }
}
