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
        private readonly long _length;

        protected ISaisString Text { get { return _text; } }
        public long Length { get { return _length; } }

        protected BaseSuffixArray(ISaisString text)
        {
            _text = text;
            _length = text.Length;
        }

        public void CreateSuffixArray(int recursionLevel)
        {
            DateTime determineTypesStartTime = DateTime.Now;

            Text.Types = new TypeArray(Text);

            Console.WriteLine("{0} characters at level {1}. Took {2} seconds determine types.", Text.Length,
                              recursionLevel, DateTime.Now.Subtract(determineTypesStartTime).TotalSeconds);

            /* Scan for LMS-characters and get LMS-Strings */
            LmsStringStruct[] lmsStringStructs = LmsString.GetLmsStrings(Text);

            DateTime lmsStringsSortTimeStart = DateTime.Now;

            LmsStringStruct.ValueComparer lmsComparer = new LmsStringStruct.ValueComparer(Text);
            Array.Sort(lmsStringStructs, lmsComparer);

            Console.WriteLine("{0} LMS strings at level {1}. Took {2} seconds to sort.", lmsStringStructs.Length,
                              recursionLevel, DateTime.Now.Subtract(lmsStringsSortTimeStart).TotalSeconds);

            DateTime assignNamesStartTime = DateTime.Now;

            /* Assign values to LMS-Strings */
            uint lmsValue = 0;
            lmsStringStructs[0].Value = 0;
            for (int index = 1; index < lmsStringStructs.Length; index++) /* assuming there are less than 2^31 LMS-Strings */
            {
                if (lmsComparer.Compare(lmsStringStructs[index], lmsStringStructs[index - 1]) == 0)
                    lmsStringStructs[index].Value = lmsValue;
                else
                    lmsStringStructs[index].Value = ++lmsValue;
            }

            Console.WriteLine("{0} buckets at level {1}. Name assigning took {2} seconds", lmsValue, recursionLevel, System.DateTime.Now.Subtract(assignNamesStartTime).TotalSeconds);

            /* Do a recursive call if the LMS-Strings are not unique */
            ISuffixArray inducedSufArray;
            Array.Sort(lmsStringStructs, (l1, l2) => l1.FirstCharacterIndex.CompareTo(l2.FirstCharacterIndex));
            LevelNString levelNString = new LevelNString(lmsStringStructs);
            if (lmsValue < lmsStringStructs.Length - 1)
            {
                inducedSufArray = new LevelNSuffixArray(levelNString, false, recursionLevel+1);
            }
            else
            {
                inducedSufArray = new LevelNSuffixArray(levelNString, true, recursionLevel+1);
            }

            DateTime step1PassStart = DateTime.Now;

            /* Set up buckets so we can set the head in right spot for each pass */
            var bucketIndices = Text.BucketIndices;
            uint[] bucketHeads = new uint[bucketIndices.Length];

            /* Step 1. Start by setting heads to end of buckets */
            SetBucketHeads(bucketIndices, bucketHeads, false);

            /* Scan inducedSA once from right to left, put P[inducedSA[i]] to the current end of the bucket for suf (S; P1[SA1[i]]) in
             * SA and forward the bucket’s end one item to the left. P = lmsCharacterIndices*/
            for (long inducedSaIndex = inducedSufArray.Length - 1; inducedSaIndex >= 0; inducedSaIndex--)
            {
                var inducedCharIndex = inducedSufArray[(uint) inducedSaIndex];
                var characterIndex = lmsStringStructs[inducedCharIndex].FirstCharacterIndex;
                var bucketIndex = Text[characterIndex];
                var bucketHead = bucketHeads[bucketIndex];
                this[bucketHead] = characterIndex;
                bucketHeads[bucketIndex] = bucketHeads[bucketIndex] - 1;
            }

            Console.WriteLine("Took {0} seconds to finish step 1 at level {1}.",
                              DateTime.Now.Subtract(step1PassStart).TotalSeconds, recursionLevel);

            DateTime step2Start = DateTime.Now;

            /* Step 2. Start by setting heads to beginning of buckets */
            SetBucketHeads(bucketIndices, bucketHeads, true);

            /* Scan SA from left to right, for each non-negative item SA[i], if S[SA[i]−1] is L-type, then put 
             * SA[i] − 1 to the current head of the bucket for suf (S; SA[i] − 1) and forward that bucket’s head
             * one item to the right */
            for (uint saIndex = 0; saIndex < Length; saIndex++)
            {
                var textIndex = this[saIndex];
                if (textIndex != DefaultValue)
                {
                    /* SaOfiMinus1 = SA[i]-1. Have to check that if it needs to wrap around */
                    var SaOfiMinus1 = textIndex == 0 ? Text.Length - 1 : textIndex - 1;
                    SaisType characterType = Text.Types[SaOfiMinus1];
                    if (characterType == SaisType.L)
                    {
                        var bucketIndex = Text[SaOfiMinus1];
                        var bucketHead = bucketHeads[bucketIndex];
                        this[bucketHead] = SaOfiMinus1;
                        bucketHeads[bucketIndex] = bucketHeads[bucketIndex] + 1;
                    }
                }
            }

            Console.WriteLine("Took {0} seconds to finish step 2 at level {1}.",
                              DateTime.Now.Subtract(step2Start).TotalSeconds, recursionLevel);

            DateTime step3Start = DateTime.Now;

            /* Step 3. Start by setting heads to end of buckets */
            SetBucketHeads(bucketIndices, bucketHeads, false);

            /* Scan SA from right to left, for each non-negative item SA[i], if S[SA[i]−1] is S-type, then
             * put SA[i] − 1 to the current end of the bucket for suf (S; SA[i] −1) and forward that bucket’s end one
             * item to the left */
            for (long saIndex = Length - 1; saIndex >= 0; saIndex--)
            {
                var textIndex = this[(uint) saIndex];
                if (textIndex != DefaultValue)
                {
                    var SaOfiMinus1 = textIndex == 0 ? Text.Length - 1 : textIndex - 1;
                    var characterType = Text.Types[SaOfiMinus1];
                    if (characterType == SaisType.S)
                    {
                        var bucketIndex = Text[SaOfiMinus1];
                        var bucketHead = bucketHeads[bucketIndex];
                        this[bucketHead] = SaOfiMinus1;
                        bucketHeads[bucketIndex] = bucketHeads[bucketIndex] - 1;
                    }
                }
            }

            Console.WriteLine("Took {0} seconds to finish step 3 at level {1}.",
                              DateTime.Now.Subtract(step3Start).TotalSeconds, recursionLevel);
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
