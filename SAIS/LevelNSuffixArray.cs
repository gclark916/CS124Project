using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CS124Project.SAIS
{
    internal class LevelNSuffixArray : BaseSuffixArray
    {
        private readonly int[] _suffixArray;

        public LevelNSuffixArray(ISaisString text, bool skipRecursion) : base(text)
        {
            if (skipRecursion)
            {
                _suffixArray = Enumerable.Range(0, (int) text.Length).ToArray();
                return;
            }

            _suffixArray = new int[text.Length];
            CreateSuffixArray();
        }

        public override uint this[uint index]
        {
            get { return (uint) _suffixArray[index]; }
            protected set { _suffixArray[index] = (int) value; }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (uint i = 0; i < Length & i < 20; i++)
            {
                builder.Append(this[i]);
                builder.Append(' ');
            }
            
            return builder.ToString();
        }
    }
}