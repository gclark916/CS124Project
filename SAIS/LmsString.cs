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

        public static LmsStringStruct[] GetLmsStrings(ISaisString text)
        {
            SaisType previousType = text.Types[0];
            int lmsStringCount = 0;
            for (uint index = 0, previousLmsCharacterIndex = 0; index < text.Length; index++)
            {
                SaisType currentType = text.Types[index];
                if (previousType == SaisType.L && currentType == SaisType.S)
                {
                    if (previousLmsCharacterIndex > 0)
                        lmsStringCount++;
                    previousLmsCharacterIndex = index;
                }
                previousType = currentType;
            }

            lmsStringCount++;

            LmsStringStruct[] lmsStringStructs = new LmsStringStruct[lmsStringCount];
            uint lmsStringIndex = 0;
            for (uint index = 0, previousLmsCharacterIndex = 0; index < text.Length; index++)
            {
                SaisType currentType = text.Types[index];
                if (previousType == SaisType.L && currentType == SaisType.S)
                {
                    if (previousLmsCharacterIndex > 0)
                    {
                        lmsStringStructs[lmsStringIndex].FirstCharacterIndex = previousLmsCharacterIndex;
                        lmsStringStructs[lmsStringIndex].Length = index - previousLmsCharacterIndex + 1;
                        lmsStringStructs[lmsStringIndex].Value = uint.MaxValue;
                        lmsStringIndex++;
                    }
                    previousLmsCharacterIndex = index;
                }
                previousType = currentType;
            }

            //TODO: need to check if adding an LMS String for $ is necessary for LevelNSuffixArrays
            lmsStringStructs[lmsStringIndex].FirstCharacterIndex = text.Length - 1;
            lmsStringStructs[lmsStringIndex].Length = 1;
            lmsStringStructs[lmsStringIndex].Value = uint.MaxValue;

            return lmsStringStructs;
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

    struct LmsStringStruct
    {
        public uint FirstCharacterIndex;
        public uint Length;
        public uint Value;

        public static LmsStringStruct[] GetLmsStrings(ISaisString text)
        {
            SaisType previousType = text.Types[0];
            uint previousLmsCharacterIndex = 0;
            int lmsStringCount = 0;
            for (uint index = 0; index < text.Length; index++)
            {
                SaisType currentType = text.Types[index];
                if (previousType == SaisType.L && currentType == SaisType.S)
                {
                    if (previousLmsCharacterIndex > 0)
                        lmsStringCount++;
                    previousLmsCharacterIndex = index;
                }
                previousType = currentType;
            }

            lmsStringCount++;

            LmsStringStruct[] lmsStringStructs = new LmsStringStruct[lmsStringCount];
            for (uint index = 0; index < text.Length; index++)
            {
                SaisType currentType = text.Types[index];
                if (previousType == SaisType.L && currentType == SaisType.S)
                {
                    if (previousLmsCharacterIndex > 0)
                    {
                        lmsStringStructs[index].FirstCharacterIndex = previousLmsCharacterIndex;
                        lmsStringStructs[index].Length = index - previousLmsCharacterIndex + 1;
                        lmsStringStructs[index].Value = uint.MaxValue;
                    }
                    previousLmsCharacterIndex = index;
                }
                previousType = currentType;
            }

            //TODO: need to check if adding an LMS String for $ is necessary for LevelNSuffixArrays
            lmsStringStructs[lmsStringStructs.Length - 1].FirstCharacterIndex = text.Length - 1;
            lmsStringStructs[lmsStringStructs.Length - 1].Length = 1;
            lmsStringStructs[lmsStringStructs.Length - 1].Value = uint.MaxValue;

            return lmsStringStructs;
        }

        public class ValueComparer : IComparer<LmsStringStruct>
        {
            public ValueComparer(ISaisString parentString)
            {
                ParentString = parentString;
            }

            protected ISaisString ParentString { get; set; }

            public int Compare(LmsStringStruct x, LmsStringStruct y)
            {
                uint index = 0;
                int result = 0;
                while (result == 0)
                {
                    if (index == x.Length && x.Length == y.Length)
                        break;
                    result = ParentString[index + x.FirstCharacterIndex].CompareTo(ParentString[index + y.FirstCharacterIndex]);
                    if (result == 0)
                    {
                        result = ParentString.Types[index + x.FirstCharacterIndex].CompareTo(ParentString.Types[index+ y.FirstCharacterIndex]);
                    }
                    index++;
                }

                return result;
            }
        }
    }
}