using System.Collections.Generic;

namespace CS124Project.SAIS
{
    class LmsString
    {
        private uint _value;
        private readonly ISaisString _parentString;
        private readonly long _firstCharacterIndex;
        private long _lastCharacterIndex;

        public LmsString(ISaisString parentString, long firstCharacterIndex, long lastCharacterIndex)
        {
            _parentString = parentString;
            _firstCharacterIndex = firstCharacterIndex;
            _lastCharacterIndex = lastCharacterIndex;
        }

        public static List<LmsString> GetLmsStrings(ISaisString text, TypeArray types)
        {
            List<LmsString> lmsStrings = new List<LmsString>();

            lmsStrings.Add(new LmsString(text, text.Length, text.Length));
            SaisType succeedingType = SaisType.S; /* last character is always S type */
            long succeedingLmsCharIndex = text.Length;
            for (long index = text.Length-1; index >= 0; index--)
            {
                SaisType currentType = types.GetType(index);
                if (succeedingType == SaisType.L && currentType == SaisType.S)
                {
                    lmsStrings.Insert(0, new LmsString(text, index, succeedingLmsCharIndex));
                    succeedingLmsCharIndex = index;
                }
                succeedingType = currentType;
            }

            return lmsStrings;
        }

        public static int CompareValues(LmsString x, LmsString y)
        {
            long index = 0;
            int result = 0;
            while (result == 0)
            {
                if (index == x.Length && x.Length == y.Length)
                    break;
                result = x.GetCharacter(index).CompareTo(y.GetCharacter(index));
                if (result == 0)
                {
                    result = x.GetCharacterType(index).CompareTo(y.GetCharacterType(index));
                }
                index++;
            }

            return result;
        }

        protected long Length
        {
            get { return _lastCharacterIndex - _firstCharacterIndex + 1; }
        }

        public uint Value { get; set; }

        public long FirstIndex
        {
            get { return _firstCharacterIndex; }
        }

        private SaisType GetCharacterType(long index)
        {
            SaisType characterType = _parentString.GetCharacterType(index + _firstCharacterIndex);
            return characterType;
        }

        private long GetCharacter(long index)
        {
            long character = _parentString.GetCharacter(index + _firstCharacterIndex);
            return character;
        }
    }
}