using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.SAIS
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class LevelNString : ISaisString
    {
        private int[] ParentArray { get; set; }
        private long Offset { get; set; }
        public long Length { get; private set; }
        public TypeArray Types { get; set; }

        public int this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                return ParentArray[Offset + index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                ParentArray[Offset + index] = value;
            }
        }

        public LevelNString(int[] parentArray, long offset, long length)
        {
            ParentArray = parentArray;
            Offset = offset;
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
