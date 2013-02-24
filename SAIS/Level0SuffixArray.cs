using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project.SAIS
{
    class Level0SuffixArray : ISuffixArray
    {
        private readonly FileStream _suffixArray;
        private readonly long _length;
        private readonly ISaisString _text;

        public Level0SuffixArray(string filePath, ISaisString text)
        {
            _text = text;
            _suffixArray = File.OpenWrite(filePath);
            byte[] buffer = new byte[1024];
            for (int bufferIndex = 0; bufferIndex < buffer.Length; bufferIndex++)
            {
                buffer[bufferIndex] = byte.MaxValue;
            }

            for (int writeIndex = 0, totalWrites = (int) (text.Length/buffer.Length + 1);
                 writeIndex < totalWrites;
                 writeIndex++)
                _suffixArray.Write(buffer, writeIndex*buffer.Length, buffer.Length);

            _length = text.Length + 1; // Extra spot for sentinel

            CreateSuffixArray();
        }

        private void CreateSuffixArray()
        {
            /* Create a TypeArray */
            TypeArray types = new TypeArray(_text);

            /* Scan for LMS-characters and get LMS-Strings */
            List<LmsString> lmsStrings = LmsString.GetLmsStrings(_text, types);

            uint[] lmsCharacterIndices = new uint[lmsStrings.Count];
            for (int lmsStringIndex = 0, lmsStringCount = lmsStrings.Count; 
                lmsStringIndex < lmsStringCount; 
                lmsStringIndex++)
            {
                lmsCharacterIndices[lmsStringIndex] = (uint) lmsStrings[lmsStringIndex].FirstIndex;
            }


            lmsStrings.Sort(LmsString.CompareValues);

            /* Assign values to LMS-Strings */
            uint value = 0;
            lmsStrings.First().Value = value;
            for (int index = 1; index < lmsStrings.Count; index++) /* assuming there are less than 2^31 LMS-Strings */
            {
                if (LmsString.CompareValues(lmsStrings[index], lmsStrings[index - 1]) == 0)
                    lmsStrings[index].Value = value;
                else
                    lmsStrings[index].Value = ++value;
            }

            /* Do a recursive call if the LMS-Strings are not unique */
            ISuffixArray inducedSA;
            if (value < lmsStrings.Count - 1)
                inducedSA = new LevelNSuffixArray(lmsStrings.Sort((l1, l2) => l1.FirstIndex.CompareTo(l2.FirstIndex)),
                                                  false);
            else
                inducedSA = new LevelNSuffixArray(lmsStrings, true);

            /* Set up buckets so we can set the head in right spot for each pass */
            uint[] bucketIndices = new uint[4];
            bucketIndices[0] = 1;
            bucketIndices[1] = 1 + _text.GetBucketSize(Base.A);
            bucketIndices[2] = bucketIndices[1] + _text.GetBucketSize(Base.C);
            bucketIndices[3] = bucketIndices[2] + _text.GetBucketSize(Base.G);

            /* Step 1. Start by setting heads to end of buckets */
            uint[] bucketHeads = { bucketIndices[1]-1, bucketIndices[2]-1, bucketIndices[3]-1, (uint) (_length-1) };

            /* Scan inducedSA once from right to left, put P[inducedSA[i]] to the current end of the bucket for suf (S; P1[SA1[i]]) in
             * SA and forward the bucket’s end one item to the left. P = lmsCharacterIndices*/
            for (long inducedSaIndex = inducedSA.Length - 1; inducedSaIndex >= 0; inducedSaIndex--)
            {
                var inducedChar = inducedSA.GetCharacter(inducedSaIndex);
                var characterIndex = lmsCharacterIndices[inducedChar];
                var bucketIndex = _text.GetCharacter(characterIndex);
                var bucketHead = bucketHeads[bucketIndex];
                SetCharacterIndex(bucketHead, characterIndex);
                bucketHeads[bucketIndex] = bucketHeads[bucketIndex] - 1;
            }
        }

        private void SetCharacterIndex(uint sufArrayIndex, uint textIndex)
        {
            byte[] buffer = BitConverter.GetBytes(textIndex);
            _suffixArray.Seek(sufArrayIndex * 4, SeekOrigin.Begin);
            _suffixArray.Write(buffer, 0, 4);
        }

        public uint GetCharacterIndex(long index)
        {
            _suffixArray.Seek(index*4, SeekOrigin.Begin);
            byte[] buffer = new byte[4];
            _suffixArray.Read(buffer, 0, 4);
            uint characterIndex = BitConverter.ToUInt32(buffer, 0);
            return characterIndex;
        }

        public long Length { get { return _length; } }
    }
}
