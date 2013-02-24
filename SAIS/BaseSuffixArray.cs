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

        public void CreateSuffixArray()
        {
            Text.Types = new TypeArray(Text);

            /* Scan for LMS-characters and get LMS-Strings */
            List<LmsString> lmsStrings = LmsString.GetLmsStrings(Text);

            uint[] lmsCharacterIndices = lmsStrings.Select(l => (uint)l.FirstIndex).ToArray();

            lmsStrings.Sort(LmsString.CompareValues);

            /* Assign values to LMS-Strings */
            uint lmsValue = 0;
            lmsStrings[0].Value = 0;
            for (int index = 1; index < lmsStrings.Count; index++) /* assuming there are less than 2^31 LMS-Strings */
            {
                if (LmsString.CompareValues(lmsStrings[index], lmsStrings[index - 1]) == 0)
                    lmsStrings[index].Value = lmsValue;
                else
                    lmsStrings[index].Value = ++lmsValue;
            }

            /* Do a recursive call if the LMS-Strings are not unique */
            ISuffixArray inducedSufArray;
            lmsStrings.Sort((l1, l2) => l1.FirstIndex.CompareTo(l2.FirstIndex));
            LevelNString levelNString = new LevelNString(lmsStrings.Select(l => l.Value).ToArray());
            if (lmsValue < lmsStrings.Count - 1)
            {
                inducedSufArray = new LevelNSuffixArray(levelNString, false);
            }
            else
            {
                inducedSufArray = new LevelNSuffixArray(levelNString, true);
            }

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
                var characterIndex = lmsCharacterIndices[inducedCharIndex];
                var bucketIndex = Text[characterIndex];
                var bucketHead = bucketHeads[bucketIndex];
                this[bucketHead] = characterIndex;
                bucketHeads[bucketIndex] = bucketHeads[bucketIndex] - 1;
            }

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
