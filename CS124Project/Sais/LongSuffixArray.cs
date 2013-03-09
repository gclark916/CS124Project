using System;
using System.IO;
using System.Text;

namespace CS124Project.Sais
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class LongSuffixArray
    {
        public ulong[] ParentArray { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }

        public static LongSuffixArray CreateSuffixArray(ISaisString text)
        {
            LongSuffixArray suffixArray = new LongSuffixArray(new ulong[text.Length / 2 + (text.Length % 2 == 0 ? 0 : 1)], 0, text.Length);
            SA_IS(text, suffixArray, suffixArray.Length, 4);
            return suffixArray;
        }

        protected LongSuffixArray(ulong[] parentArray, long offset, long length)
        {
            ParentArray = parentArray;
            Offset = offset;
            Length = length;
        }

        public void WriteToFile(string fileName, int compressionFactor)
        {
            using (var file = File.Open(fileName, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                for (long i = 0; i < Length; i += compressionFactor)
                {
                    writer.Write((uint)this[i]);
                }
            }
        }

        public long this[long index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                var adjustedIndex = Offset + index;
                long ulongIndex = adjustedIndex / 2;
                ulong ulongValue = ParentArray[ulongIndex];
                var unmasked = ulongValue >> (int)(32 * (adjustedIndex % 2));
                var uintValue = (uint)(unmasked & 0xFFFFFFFF);

                if (uintValue == uint.MaxValue)
                    return -1;
                return uintValue;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException();

                if (value < 0)
                    value = uint.MaxValue;

                var adjustedIndex = Offset + index;
                long ulongIndex = adjustedIndex / 2;
                ulong ulongValue = ParentArray[ulongIndex];
                var shiftedValue = ((ulong) value) << (int) (32*(adjustedIndex%2));
                ulongValue &= ~((ulong)0xFFFFFFFF << (int) (32*(adjustedIndex%2)));
                ulongValue |= shiftedValue;
                ParentArray[ulongIndex] = ulongValue;
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

        private static bool IsLmsCharacter(TypeArray T, long i)
        {
            return i > 0 && T[i] == SaisType.S && T[i - 1] == SaisType.L;
        }


        private static void GetBucketHeads(ISaisString s, uint[] bkt, long alphabetSize, bool end)
        {
            for (int i = 0; i <= alphabetSize; i++)
                bkt[i] = 0;
            for (long i = 0; i < s.Length; i++)
                bkt[s[i]]++;
            for (uint i = 0, sum = 0; i <= alphabetSize; i++)
            {
                sum += bkt[i];
                bkt[i] = end ? sum : sum - bkt[i];
            }
        }

        private static void InduceSaL(TypeArray T, LongSuffixArray SA, ISaisString s, uint[] bucket, long n, long alphabetSize, bool end)
        {
            GetBucketHeads(s, bucket, alphabetSize, end);
            for (long i = 0; i < n; i++)
            {
                long textPos = SA[i] - 1;
                if (textPos >= 0 && T[textPos] == SaisType.L)
                {
                    var character = s[textPos];
                    var saIndex = bucket[character];
                    SA[saIndex] = textPos;
                    bucket[character] = bucket[character] + 1;
                }
            }
        }

        private static void InduceSaS(TypeArray T, LongSuffixArray SA, ISaisString s, uint[] bucket, long n, long alphabetSize, bool end)
        {
            GetBucketHeads(s, bucket, alphabetSize, end);
            for (long i = n - 1; i >= 0; i--)
            {
                long textPos = SA[i] - 1;
                if (textPos >= 0 && T[textPos] == SaisType.S)
                {
                    var character = s[textPos];
                    bucket[character] = bucket[character] - 1;
                    var saIndex = bucket[character];
                    SA[saIndex] = textPos;
                }
            }
        }

        private static void SA_IS(ISaisString s, LongSuffixArray SA, long n, long alphabetSize)
        {
            DateTime typeStart = DateTime.Now;
            TypeArray T = new TypeArray(n);

            T[n - 2] = SaisType.L;
            T[n - 1] = SaisType.S;
            for (long textIndex = n - 3; textIndex >= 0; textIndex--)
            {
                T[textIndex] = s[textIndex] < s[textIndex + 1] ||
                       (s[textIndex] == s[textIndex + 1] && T[textIndex + 1] == SaisType.S)
                           ? SaisType.S
                           : SaisType.L;
            }
            Console.WriteLine("Type assigning took {0} seconds", DateTime.Now.Subtract(typeStart).TotalSeconds);

            uint[] bucket = new uint[alphabetSize + 1];

            DateTime lmsSortStart = DateTime.Now;
            // Set bucket pointers to ends
            GetBucketHeads(s, bucket, alphabetSize, true);
            for (long saIndex = 0; saIndex < n; saIndex++)
                SA[saIndex] = -1;

            for (long textIndex = 1; textIndex < n; textIndex++)
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
            long n1 = 0;
            for (long saIndex = 0; saIndex < n; saIndex++)
                if (IsLmsCharacter(T, SA[saIndex]))
                    SA[n1++] = SA[saIndex];

            // Name the LMS-substrings according to their order
            DateTime namingStart = DateTime.Now;
            for (long saIndex = n1; saIndex < n; saIndex++)
                SA[saIndex] = -1;
            long name = 0, prev = -1; // TODO: change?
            for (long saIndex = 0; saIndex < n1; saIndex++)
            {
                long pos = SA[saIndex];
                bool diff = false;
                for (long d = 0; d < n; d++)
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
            for (long i = n - 1, j = n - 1; i >= n1; i--)
                if (SA[i] >= 0)
                    SA[j--] = SA[i];
            Console.WriteLine("Naming took {0} seconds", DateTime.Now.Subtract(namingStart).TotalSeconds);

            // Recursive call if names are not unique
            LongSuffixArray SA1 = new LongSuffixArray(SA.ParentArray, 0, n1);
            LongLevelNString s1 = new LongLevelNString(SA.ParentArray, SA.Offset + n - n1, n1);
            if (name < n1)
                SA_IS(s1, SA1, n1, name - 1);
            else // Otherwise SA1 can be solved directly
                for (long i = 0; i < n1; i++)
                    SA1[s1[i]] = i;

            // Set bucket pointers to end of buckets
            bucket = new uint[alphabetSize + 1];
            GetBucketHeads(s, bucket, alphabetSize, true);

            // Replace s1 with P
            for (long i = 1, j = 0; i < n; i++)
                if (IsLmsCharacter(T, i))
                    s1[j++] = i;

            // Replace SA1 with sorted P
            for (int i = 0; i < n1; i++)
                SA1[i] = s1[SA1[i]];

            // Place sorted LMS characters
            DateTime step1Start = DateTime.Now;
            for (long i = n1; i < n; i++)
                SA[i] = -1;

            for (long i = n1 - 1; i >= 0; i--)
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
