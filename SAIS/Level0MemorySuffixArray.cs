using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.SAIS
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class Level0MemorySuffixArray : BaseSuffixArray
    {
        private readonly uint[] _suffixArray;

        // TODO: probaly want the buffer to use DefaultValue
        public Level0MemorySuffixArray(ISaisString text)
            : base(text)
        {
            _suffixArray = new uint[text.Length];

            for(uint i = 0; i < _suffixArray.Length; i++)
            {
                this[i] = uint.MaxValue;
            }

            CreateSuffixArray(0);
        }

        public override uint this[uint index]
        {
            get
            {
                return _suffixArray[index];
            }
            protected set { _suffixArray[index] = value; }
        }

        public void WriteToFile(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                foreach (uint index in _suffixArray)
                {
                    writer.Write(index);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(50);
            for (uint i = 0; i < 20 && i < Length; i++)
            {
                builder.Append(this[i]);
                builder.Append(' ');
            }
            return builder.ToString();
        }
    }
}
