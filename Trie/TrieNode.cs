using System.Diagnostics;
using CS124Project.Genome;

namespace CS124Project.Trie
{
    [DebuggerDisplay("{ToString()}")]
    internal class TrieNode
    {
        private readonly TrieNode[] _children;

        public TrieNode(long minSaIndex, long maxSaIndex, uint[] suffixArray, uint[] inverseSA, GenomeText genome)
        {
            Min = minSaIndex;
            Max = maxSaIndex;

            long[] childMins = new[] {long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue};
            long[] childMaxs = new[] {long.MinValue, long.MinValue, long.MinValue, long.MinValue};

            for (long saIndex = minSaIndex; saIndex <= maxSaIndex; saIndex++)
            {
                if (suffixArray[saIndex] == 0)
                    continue;

                var childTextIndex = suffixArray[saIndex] - 1;
                var childValue = genome.GetBase(childTextIndex);
                var childSaIndex = inverseSA[childTextIndex];

                if (childSaIndex > childMaxs[(int) childValue])
                    childMaxs[(int) childValue] = childSaIndex;
                if (childSaIndex < childMins[(int) childValue])
                    childMins[(int) childValue] = childSaIndex;
            }

            TrieNode[] children = null;
            for (int i = 0; i < 4; i++)
            {
                if (childMaxs[i] >= childMins[i])
                {
                    var child = new TrieNode(childMins[i], childMaxs[i], suffixArray, inverseSA, genome);
                    if (children == null)
                        children = new TrieNode[4];
                    children[i] = child;
                }
            }

            _children = children;
        }

        public long Max { get; set; }
        public long Min { get; set; }

        public TrieNode GetChild(Base edgeBase)
        {
            return _children[(int) edgeBase];
        }

        public override string ToString()
        {
            return _children == null
                       ? string.Format("({0}, {1}) leaf", Min, Max)
                       : string.Format("({0}, {1}) [({2}, {3}), ({4}, {5}), ({6}, {7}), ({8}, {9})] ",
                                       Min, Max,
                                       _children[0] == null ? -1 : _children[0].Min,
                                       _children[0] == null ? -1 : _children[0].Max,
                                       _children[1] == null ? -1 : _children[1].Min,
                                       _children[1] == null ? -1 : _children[1].Max,
                                       _children[2] == null ? -1 : _children[2].Min,
                                       _children[2] == null ? -1 : _children[2].Max,
                                       _children[3] == null ? -1 : _children[3].Min,
                                       _children[3] == null ? -1 : _children[3].Max);
        }
    }
}