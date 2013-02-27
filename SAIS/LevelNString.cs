using System;
using System.Text;

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
            get
            {
                return _text[index];
            }
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