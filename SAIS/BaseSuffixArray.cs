using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.SAIS
{
    abstract class BaseSuffixArray : ISuffixArray
    {
        private readonly ISaisString _text;
        private readonly uint _length;

        protected ISaisString Text { get { return _text; } }
        public uint Length { get { return _length; } }

        protected BaseSuffixArray(ISaisString text)
        {
            _text = text;
            _length = text.Length;
        }

        public void CreateSuffixArray(int recursionLevel)
        {

            DateTime determineTypesStartTime = DateTime.Now;
            TypeArray types = new TypeArray(Text);
            Text.Types = types;
            Console.WriteLine("{0} characters at level {1}. Took {2} seconds determine types.", Text.Length,
                              recursionLevel, DateTime.Now.Subtract(determineTypesStartTime).TotalSeconds);

            var bucketIndices = Text.BucketIndices;
            uint[] bucketHeads = new uint[bucketIndices.Length];

            // Sort S substrings
            SetBucketHeads(bucketIndices, bucketHeads, false);

            DateTime lmsStringsSortTimeStart = DateTime.Now;

            uint lmsCharacterCount = 0;
            for (uint textIndex = 1; textIndex < Text.Length; textIndex++)
            {
                if (IsLmsCharacter(textIndex))
                {
                    var bucket = Text[textIndex];
                    var bucketHead = bucketHeads[bucket];
                    this[bucketHead] = textIndex;
                    bucketHeads[bucket] = bucketHeads[bucket] - 1;
                    lmsCharacterCount++;
                }
            }

            InduceSAl(bucketIndices, bucketHeads);
            InduceSAs(bucketIndices, bucketHeads);

            Console.WriteLine("{0} LMS strings at level {1}. Took {2} seconds to sort.", lmsCharacterCount,
                              recursionLevel, DateTime.Now.Subtract(lmsStringsSortTimeStart).TotalSeconds);

            uint[] P = new uint[lmsCharacterCount];
            for (uint textIndex = 1, pIndex = 0; textIndex < Text.Length; textIndex++)
            {
                if (IsLmsCharacter(textIndex))
                    P[pIndex++] = textIndex;
            }

            // Assign names to LMS substrings
            uint[] S1 = new uint[P.Length];
            S1[S1.Length - 1] = 0;
            uint name = 0, prevLmsIndex = uint.MaxValue;
            for (uint saIndex = 1; saIndex < Length; saIndex++)
            {
                uint textIndex = this[saIndex];
                if (!IsLmsCharacter(textIndex)) 
                    continue;

                uint pIndex = (uint)Array.BinarySearch(P, textIndex);
                if (LmsStringsAreEqual(textIndex, prevLmsIndex))
                    S1[pIndex] = name;
                else
                    S1[pIndex] = ++name;
                prevLmsIndex = textIndex;
            }

            LevelNSuffixArray SA1;
            LevelNString levelNString = new LevelNString(S1);
            if(name + 1 < P.Length)
                SA1 = new LevelNSuffixArray(levelNString, false, recursionLevel + 1);
            else
                SA1 = new LevelNSuffixArray(levelNString, true, recursionLevel + 1);

            DateTime step1Start = DateTime.Now;
            Step1_2(bucketIndices, bucketHeads, SA1, P);
            Console.WriteLine("Took {0} seconds to finish step 1 at level {1}.",
                              DateTime.Now.Subtract(step1Start).TotalSeconds, recursionLevel);

            DateTime step2Start = DateTime.Now;
            InduceSAl(bucketIndices, bucketHeads);
            Console.WriteLine("Took {0} seconds to finish step 2 at level {1}.",
                              DateTime.Now.Subtract(step2Start).TotalSeconds, recursionLevel);

            DateTime step3Start = DateTime.Now;
            InduceSAs(bucketIndices, bucketHeads);
            Console.WriteLine("Took {0} seconds to finish step 3 at level {1}.",
                              DateTime.Now.Subtract(step3Start).TotalSeconds, recursionLevel);
        }

        private bool LmsStringsAreEqual(uint lmsIndex1, uint lmsIndex2)
        {
            if ((lmsIndex1 == Text.Length - 1 || lmsIndex2 == Text.Length - 1)
                && lmsIndex1 != lmsIndex2)
                return false;
            int result = 0;
            lmsIndex1++;
            lmsIndex2++;
            while (result == 0)
            {
                if (IsLmsCharacter(lmsIndex1))
                {
                    return IsLmsCharacter(lmsIndex2);
                }
                if (IsLmsCharacter(lmsIndex2))
                    return false;
                
                result = Text[lmsIndex1].CompareTo(Text[lmsIndex2]);
                if (result == 0)
                    result = Text.Types[lmsIndex1].CompareTo(Text.Types[lmsIndex2]);
                lmsIndex1++;
                lmsIndex2++;
            }

            return false;
        }

        private bool IsLmsCharacter(uint textIndex)
        {
            return textIndex != DefaultValue && textIndex > 0 && Text.Types[textIndex] == SaisType.S && Text.Types[textIndex - 1] == SaisType.L
                || textIndex == Text.Length-1;
        }

        void Step1_2(uint[] bucketIndices, uint[] bucketHeads, ISuffixArray SA1, uint[] P)
        {
            /* Step 1. Start by setting heads to end of buckets */
            SetBucketHeads(bucketIndices, bucketHeads, false);

            /* Clear SA */
            for (uint i = 0; i < Length; i++)
                this[i] = DefaultValue;

            /* Scan SA1 once from right to left, put P[SA1[i]] to the current end of the bucket for suf (S; P1[SA1[i]]) in
                * SA and forward the bucket’s end one item to the left. P = lmsCharacterIndices*/
            for (long SA1Index = SA1.Length - 1; SA1Index >= 0; SA1Index--)
            {
                var SA1Value = SA1[(uint)SA1Index];
                var characterIndex = P[SA1Value];
                var bucketIndex = Text[characterIndex];
                var bucketHead = bucketHeads[bucketIndex];
                this[bucketHead] = characterIndex;
                bucketHeads[bucketIndex] = bucketHeads[bucketIndex] - 1;
            }
        }

        void InduceSAl(uint[] bucketIndices, uint[] bucketHeads)
        {
            SetBucketHeads(bucketIndices, bucketHeads, true);

            for (uint saIndex = 0; saIndex < Length; saIndex++)
            {
                if (this[saIndex] == DefaultValue) 
                    continue;

                var textPos = this[saIndex] - 1 != DefaultValue ? this[saIndex] - 1 : Text.Length-1;
                    
                if (textPos < DefaultValue && Text.Types[textPos] == SaisType.L)
                {
                    var bucket = Text[textPos];
                    var bucketHead = bucketHeads[bucket];
                    this[bucketHead] = textPos;
                    bucketHeads[bucket] = bucketHeads[bucket] + 1;
                }
            }
        }

        void InduceSAs(uint[] bucketIndices, uint[] bucketHeads)
        {
            SetBucketHeads(bucketIndices, bucketHeads, false);

            for(uint saIndex = Length - 1; saIndex != DefaultValue; saIndex--) 
            {
                if (this[saIndex] == DefaultValue) 
                    continue;

                uint textPos = this[saIndex] - 1 != DefaultValue ? this[saIndex] - 1 : Text.Length - 1;
                if (textPos != uint.MaxValue && Text.Types[textPos] == SaisType.S)
                {
                    var bucket = Text[textPos];
                    var bucketHead = bucketHeads[bucket];
                    this[bucketHead] = textPos;
                    bucketHeads[bucket] = bucketHeads[bucket] - 1;
                }
            }
        }

        uint DefaultValue
        {
            get { return uint.MaxValue; }
        }

        public abstract uint this[uint i] { get; protected set; }

        private void SetBucketHeads(uint[] bucketIndices, uint[] bucketHeads, bool setToBeginning)
        {
            if (setToBeginning)
            {
                for (int bucketIndex = 0, bucketCount = bucketIndices.Length; bucketIndex < bucketCount; bucketIndex++)
                {
                    bucketHeads[bucketIndex] = bucketIndices[bucketIndex];
                }
            }
            else
            {

                for (int bucketIndex = 0, bucketCount = bucketIndices.Length; bucketIndex < bucketCount - 1; bucketIndex++)
                {
                    bucketHeads[bucketIndex] = bucketIndices[bucketIndex + 1] - 1;
                }
                bucketHeads[bucketIndices.Length-1] = (uint)(Length - 1);
            }
        }
    }
}
