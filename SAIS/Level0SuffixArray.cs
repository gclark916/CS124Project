using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.SAIS
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class Level0SuffixArray : BaseSuffixArray
    {
        private readonly FileStream _suffixArray;

        // TODO: probaly want the buffer to use DefaultValue
        public Level0SuffixArray(string filePath, ISaisString text) : base(text)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            _suffixArray = File.Open(filePath, FileMode.CreateNew);
            byte[] buffer = new byte[1024];
            for (int bufferIndex = 0; bufferIndex < buffer.Length; bufferIndex++)
            {
                buffer[bufferIndex] = byte.MaxValue;
            }

            for (int writeIndex = 0, totalWrites = (int) (text.Length/buffer.Length + 1);
                 writeIndex < totalWrites;
                 writeIndex++)
                _suffixArray.Write(buffer, 0, buffer.Length);

            CreateSuffixArray2(0);
        }

        public override uint this[uint index]
        {
            get 
            {
                _suffixArray.Seek(index * 4, SeekOrigin.Begin);
                byte[] buffer = new byte[4];
                _suffixArray.Read(buffer, 0, 4);
                uint characterIndex = BitConverter.ToUInt32(buffer, 0);
                return characterIndex;
            }
            protected set
            {
                byte[] buffer = BitConverter.GetBytes(value);
                _suffixArray.Seek(index * 4, SeekOrigin.Begin);
                _suffixArray.Write(buffer, 0, 4);
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
