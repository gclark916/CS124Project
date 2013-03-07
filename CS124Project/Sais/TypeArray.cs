using System.Text;

namespace CS124Project.Sais
{
    class TypeArray
    {
        private readonly byte[] _types;

        public string TypesAsString
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < 10 && i < _types.Length * 8; i++)
                {
                    builder.Append(this[i] == SaisType.L ? 'L' : 'S');
                }

                return builder.ToString();
            }
        }

        public TypeArray(long length)
        {
            _types = new byte[length / 8 + 1];
        }

        public SaisType this[long index]
        {
            get
            {
                byte typeByte = _types[index / 8];
                SaisType type = (SaisType)((typeByte >> (int)(index % 8)) & 0x1);
                return type;
            }

            set
            {
                byte mask = (byte)(1 << (int)(index % 8));

                // Clear to 0 if L, set to 1 if S
                // Type S is considered bigger than type L
                if (value == SaisType.L)
                    _types[index / 8] &= (byte)~mask;
                else
                    _types[index / 8] |= mask;
            }
        }
    }
}
