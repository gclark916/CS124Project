using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project.SAIS
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class Level0String : ISaisString
    {
        private readonly byte[] _text;
        private readonly uint _length;
        public uint Length { get { return _length; } }
        public uint[] BucketIndices { get { return Types.BucketIndices; } }
        public TypeArray Types { get; set; }

        public uint this[uint index]
        {
            get
            {
                if (index >= _length)
                    throw new IndexOutOfRangeException("index");

                if (index == _length - 1)
                    return 0;

                byte charByte = _text[index / 4];
                uint unmaskedChar = (uint) ((charByte >> (int)(2 * (index % 4))));
                var maskedChar = unmaskedChar & 3;
                return maskedChar + 1; // Add one because sentinel takes value 0 and dnaBases are set to use 0..3
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length">Number of characters, not including sentinel</param>
        public Level0String(byte[] text, uint length)
        {
            _text = text;
            _length = length+1; // Add one for sentinel
        }

        public Level0String(GenomeText text)
        {
            _text = text.Bytes;
            _length = (uint) (text.Length + 1);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (uint i = 0; i < _length && i < 20; i++)
            {
                string character;
                var value = this[i];
                switch (value)
                {
                    case 0:
                        character = "$";
                        break;
                    case 1:
                        character = "A";
                        break;
                    case 2:
                        character = "C";
                        break;
                    case 3:
                        character = "G";
                        break;
                    case 4:
                        character = "T";
                        break;
                    default:
                        character = i.ToString();
                        break;
                }

                builder.Append(character);
            }
            
            return builder.ToString();
        }
    }
}
