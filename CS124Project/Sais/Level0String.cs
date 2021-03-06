﻿using System;
using System.Globalization;
using System.Text;
using CS124Project.Dna;

namespace CS124Project.Sais
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class Level0String : ISaisString
    {
        private readonly byte[] _text;
        private readonly uint _length;
        public long Length { get { return _length; } }
        public TypeArray Types { get; set; }

        public long this[long index]
        {
            get
            {
                if (index >= _length)
                    throw new IndexOutOfRangeException("index");

                if (index == _length - 1)
                    return 0;

                byte charByte = _text[index / 4];
                int unmaskedChar = ((charByte >> (int)(2 * (index % 4))));
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
            _length = length + 1; // Add one for sentinel
        }

        public Level0String(DnaSequence text)
        {
            _text = text.Bytes;
            _length = (uint)(text.Length + 1);
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
                        character = i.ToString(CultureInfo.InvariantCulture);
                        break;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }
    }
}
