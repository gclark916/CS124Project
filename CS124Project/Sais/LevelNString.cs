using System;
using System.Text;

namespace CS124Project.Sais
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class LevelNString : ISaisString
    {
        private readonly int[] _parentArray;
        private readonly long _offset;
        public long Length { get; private set; }
        public TypeArray Types { get; set; }

        public long this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                return _parentArray[_offset + index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                _parentArray[_offset + index] = (int)value;
            }
        }

        public LevelNString(int[] parentArray, long offset, long length)
        {
            _parentArray = parentArray;
            _offset = offset;
            Length = length;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (uint i = 0; i < 20 && i < Length; i++)
            {
                builder.Append(this[i]);
                builder.Append(' ');
            }
            return builder.ToString();
        }
    }
}
