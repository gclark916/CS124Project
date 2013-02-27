using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CS124Project.SAIS
{
    internal class LevelNSuffixArray : BaseSuffixArray
    {
        private readonly uint[] _suffixArray;

        public LevelNSuffixArray(ISaisString text, bool skipRecursion, int level) : base(text)
        {
            _suffixArray = new uint[text.Length];

            if (skipRecursion)
            {
                for (uint i = 0; i < Length; i++)
                    _suffixArray[i] = i;
                return;
            }

            for (uint i = 0; i < Length; i++)
                _suffixArray[i] = uint.MaxValue;

            CreateSuffixArray(level);
        }

        public override uint this[uint index]
        {
            get { return _suffixArray[index]; }
            protected set { _suffixArray[index] = value; }
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