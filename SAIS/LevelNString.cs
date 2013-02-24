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
        private readonly uint[] _text;
        public uint Length { get { return (uint) _text.Length; } }
        public uint[] BucketIndices { get { return Types.BucketIndices; } }
        public TypeArray Types { get; set; }

        public uint this[uint index]
        {
            get { return _text[(int) index]; }
        }

        public LevelNString(uint[] text)
        {
            _text = text;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (uint i = 0; i < 10 && i < Length; i++)
            {
                builder.Append(this[i]);
                builder.Append(' ');
            }
                return base.ToString();
        }
    }
}
