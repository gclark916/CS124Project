using System;
using System.Text;

namespace CS124Project.Sais
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class LongLevelNString : ISaisString
    {
        private ulong[] ParentArray { get; set; }
        private long Offset { get; set; }
        public long Length { get; private set; }
        public TypeArray Types { get; set; }

        public long this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                var adjustedIndex = Offset + index;
                long ulongIndex = adjustedIndex / 2;
                ulong ulongValue = ParentArray[ulongIndex];
                var unmasked = ulongValue >> (int)(32 * (adjustedIndex % 2));
                var uintValue = (uint)(unmasked & (ulong)0xFFFFFFFF);

                if (uintValue == uint.MaxValue)
                    return -1;

                return uintValue;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                uint uintValue = (uint)value;
                if (value < 0)
                    uintValue = uint.MaxValue;

                var adjustedIndex = Offset + index;
                long ulongIndex = adjustedIndex / 2;
                ulong ulongValue = ParentArray[ulongIndex];
                var shiftedValue = ((ulong)uintValue) << (int)(32 * (adjustedIndex % 2));
                ulongValue &= ~((ulong)0xFFFFFFFF << (int)(32 * (adjustedIndex % 2)));
                ulongValue |= shiftedValue;
                ParentArray[ulongIndex] = ulongValue;
            }
        }

        public LongLevelNString(ulong[] parentArray, long offset, long length)
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