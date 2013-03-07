using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.Sais
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class SuffixArray
    {
        //TODO: change to uint
        public int[] ParentArray { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }

        public static SuffixArray CreateSuffixArray(ISaisString text)
        {
            SuffixArray suffixArray = new SuffixArray(new int[text.Length], 0, text.Length);
            SA_IS(text, suffixArray, (int) suffixArray.Length, 4);
            return suffixArray;
        }

        protected SuffixArray(int[] parentArray, long offset, long length)
        {
            ParentArray = parentArray;
            Offset = offset;
            Length = length;
        }

        public void WriteToFile(string fileName, int compressionFactor)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                for (int i = 0; i < Length; i += compressionFactor)
                {
                    file.Write(BitConverter.GetBytes((uint)this[i]), 0, sizeof(uint));
                }
            }
        }

        public int this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                return ParentArray[Offset + index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();
                ParentArray[Offset + index] = value;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (uint i = 0; i < 20 && i < Length; i++)
            {
                builder.Append(this[i]);
                builder.Append(' ');
            }
            return builder.ToString();
        }

        private static bool IsLmsCharacter(TypeArray T, int i)
        {
            return i > 0 && T[i] == SaisType.S && T[i - 1] == SaisType.L;
        }


        private static void GetBucketHeads(ISaisString s, int[] bkt, int alphabetSize, bool end)
        {
            for (int i = 0; i <= alphabetSize; i++)
                bkt[i] = 0;
            for (int i = 0; i < s.Length; i++)
                bkt[s[i]]++;
            for (int i = 0, sum = 0; i <= alphabetSize; i++)
            {
                sum += bkt[i];
                bkt[i] = end ? sum : sum - bkt[i];
            }
        }

        private static void InduceSaL(TypeArray T, SuffixArray SA, ISaisString s, int[] bucket, int n, int alphabetSize, bool end)
        {
            GetBucketHeads(s, bucket, alphabetSize, end);
            for (int i = 0; i < n; i++)
            {
                int textPos = SA[i] - 1;
                if (textPos >= 0 && T[textPos] == SaisType.L)
                {
                    var character = s[textPos];
                    var saIndex = bucket[character];
                    SA[saIndex] = textPos;
                    bucket[character] = bucket[character] + 1;
                }
            }
        }

        private static void InduceSaS(TypeArray T, SuffixArray SA, ISaisString s, int[] bucket, int n, int alphabetSize, bool end)
        {
            GetBucketHeads(s, bucket, alphabetSize, end);
            for (int i = n - 1; i >= 0; i--)
            {
                int textPos = SA[i] - 1;
                if (textPos >= 0 && T[textPos] == SaisType.S)
                {
                    var character = s[textPos];
                    bucket[character] = bucket[character] - 1;
                    var saIndex = bucket[character];
                    SA[saIndex] = textPos;
                }
            }
        }

        private static void SA_IS(ISaisString s, SuffixArray SA, int n, int alphabetSize)
        {
            DateTime typeStart = DateTime.Now;
            TypeArray T = new TypeArray(n);

            T[n - 2] = SaisType.L;
            T[n - 1] = SaisType.S;
            for (int textIndex = n - 3; textIndex >= 0; textIndex--)
            {
                T[textIndex] = s[textIndex] < s[textIndex + 1] ||
                       (s[textIndex] == s[textIndex + 1] && T[textIndex + 1] == SaisType.S)
                           ? SaisType.S
                           : SaisType.L;
            }
            Console.WriteLine("Type assigning took {0} seconds", DateTime.Now.Subtract(typeStart).TotalSeconds);

            int[] bucket = new int[alphabetSize + 1];

            DateTime lmsSortStart = DateTime.Now;
            // Set bucket pointers to ends
            GetBucketHeads(s, bucket, alphabetSize, true);
            for (long saIndex = 0; saIndex < n; saIndex++)
                SA[saIndex] = -1;

            for (int textIndex = 1; textIndex < n; textIndex++)
                if (IsLmsCharacter(T, textIndex))
                {
                    var textChar = s[textIndex];
                    bucket[textChar] = bucket[textChar] - 1;
                    var saIndex = bucket[textChar];
                    SA[saIndex] = textIndex;
                }
            InduceSaL(T, SA, s, bucket, n, alphabetSize, false);
            InduceSaS(T, SA, s, bucket, n, alphabetSize, true);
            Console.WriteLine("LMS-substring sorting took {0} seconds", DateTime.Now.Subtract(lmsSortStart).TotalSeconds);

            // Move LMS characters into first half of SA array
            int n1 = 0;
            for (int saIndex = 0; saIndex < n; saIndex++)
                if (IsLmsCharacter(T, SA[saIndex]))
                    SA[n1++] = SA[saIndex];

            // Name the LMS-substrings according to their order
            DateTime namingStart = DateTime.Now;
            for (int saIndex = n1; saIndex < n; saIndex++)
                SA[saIndex] = -1;
            int name = 0, prev = -1;
            for (int saIndex = 0; saIndex < n1; saIndex++)
            {
                int pos = SA[saIndex];
                bool diff = false;
                for (int d = 0; d < n; d++)
                    if (prev == -1 || s[pos + d] != s[prev + d]
                        || T[pos + d] != T[prev + d])
                    {
                        diff = true;
                        break;
                    }
                    else if (d > 0 && (IsLmsCharacter(T, pos + d) || IsLmsCharacter(T, prev + d)))
                        break;
                if (diff)
                {
                    name++;
                    prev = pos;
                }
                pos = (pos % 2 == 0) ? pos / 2 : (pos - 1) / 2;
                SA[n1 + pos] = name - 1;
            }
            for (int i = n - 1, j = n - 1; i >= n1; i--)
                if (SA[i] >= 0)
                    SA[j--] = SA[i];
            Console.WriteLine("Naming took {0} seconds", DateTime.Now.Subtract(namingStart).TotalSeconds);

            // Recursive call if names are not unique
            SuffixArray SA1 = new SuffixArray(SA.ParentArray, 0, n1);
            LevelNString s1 = new LevelNString(SA.ParentArray, SA.Offset + n - n1, n1);
            if (name < n1)
                SA_IS(s1, SA1, n1, name - 1);
            else // Otherwise SA1 can be solved directly
                for (int i = 0; i < n1; i++)
                    SA1[s1[i]] = i;

            // Set bucket pointers to end of buckets
            bucket = new int[alphabetSize + 1];
            GetBucketHeads(s, bucket, alphabetSize, true);

            // Replace s1 with P
            for (int i = 1, j = 0; i < n; i++)
                if (IsLmsCharacter(T, i))
                    s1[j++] = i;

            // Replace SA1 with sorted P
            for (int i = 0; i < n1; i++)
                SA1[i] = (int)s1[SA1[i]];

            // Place sorted LMS characters
            DateTime step1Start = DateTime.Now;
            for (int i = n1; i < n; i++)
                SA[i] = -1;

            for (int i = n1 - 1; i >= 0; i--)
            {
                var textPos = SA[i];
                SA[i] = -1;
                var character = s[textPos];
                bucket[character] = bucket[character] - 1;
                var saIndex = bucket[character];
                SA[saIndex] = textPos;
            }
            Console.WriteLine("Step 1 took {0} seconds", DateTime.Now.Subtract(step1Start).TotalSeconds);

            // Induce sort L characters from LMS characters
            DateTime step2Start = DateTime.Now;
            InduceSaL(T, SA, s, bucket, n, alphabetSize, false);
            Console.WriteLine("Step 2 took {0} seconds", DateTime.Now.Subtract(step2Start).TotalSeconds);

            // Induce sort remaining S characters
            DateTime step3Start = DateTime.Now;
            InduceSaS(T, SA, s, bucket, n, alphabetSize, true);
            Console.WriteLine("Step 3 took {0} seconds", DateTime.Now.Subtract(step3Start).TotalSeconds);
        }
    }
}
