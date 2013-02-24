using System.Collections.Generic;

namespace CS124Project.SAIS
{
    internal class LmsString
    {
        private readonly uint _firstCharacterIndex;
        private readonly uint _lastCharacterIndex;
        private readonly ISaisString _parentString;

        public LmsString(ISaisString parentString, uint firstCharacterIndex, uint lastCharacterIndex)
        {
            _parentString = parentString;
            _firstCharacterIndex = firstCharacterIndex;
            _lastCharacterIndex = lastCharacterIndex;
        }

        public uint Length
        {
            get { return _lastCharacterIndex - _firstCharacterIndex + 1; }
        }

        public uint Value { get; set; }

        public long FirstIndex
        {
            get { return _firstCharacterIndex; }
        }

        public static List<LmsString> GetLmsStrings(ISaisString text)
        {
            List<LmsString> lmsStrings = new List<LmsString>();

            SaisType previousType = text.Types[0];
            uint previousLmsCharacterIndex = 0;
            for (uint index = 0; index < text.Length; index++)
            {
                SaisType currentType = text.Types[index];
                if (previousType == SaisType.L && currentType == SaisType.S)
                {
                    if (previousLmsCharacterIndex > 0)
                        lmsStrings.Add(new LmsString(text, previousLmsCharacterIndex, index));
                    previousLmsCharacterIndex = index;
                }
                previousType = currentType;
            }

            //TODO: need to check if adding an LMS String for $ is necessary for LevelNSuffixArrays
            lmsStrings.Add(new LmsString(text, text.Length-1, text.Length-1));

            return lmsStrings;
        }

        public static int CompareValues(LmsString x, LmsString y)
        {
            uint index = 0;
            int result = 0;
            while (result == 0)
            {
                if (index == x.Length && x.Length == y.Length)
                    break;
                result = x[index].CompareTo(y[index]);
                if (result == 0)
                {
                    result = x.GetCharacterType(index).CompareTo(y.GetCharacterType(index));
                }
                index++;
            }

            return result;
        }

        protected uint this[uint index]
        {
            get 
            { 
                var character = _parentString[index + _firstCharacterIndex];
                return character; 
            }
        }

        private SaisType GetCharacterType(long index)
        {
            SaisType characterType = _parentString.Types[index + _firstCharacterIndex];
            return characterType;
        }
    }
}