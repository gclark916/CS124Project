using CS124Project.Genome;

namespace CS124Project.Trie
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    class TrieNode
    {
        public long Max { get; set; }
        public long Min { get; set; }
        private readonly TrieNode[] _children;

        public TrieNode(TrieNode[] children)
        {
            _children = children;
        }

        public TrieNode GetChild(Base edgeBase)
        {
            return _children[(int)edgeBase];
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}) [({2}, {3}), ({4}, {5}), ({6}, {7}), ({8}, {9})] ",
                                 Min, Max,
                                 _children[0] == null ? -1 : _children[0].Min, _children[0] == null ? -1 : _children[0].Max,
                                 _children[1] == null ? -1 : _children[1].Min, _children[1] == null ? -1 : _children[1].Max,
                                 _children[2] == null ? -1 : _children[2].Min, _children[2] == null ? -1 : _children[2].Max,
                                 _children[3] == null ? -1 : _children[3].Min, _children[3] == null ? -1 : _children[3].Max);
        }

        public static TrieNode CreateTrie(IGenomeText text, IGenomeText fullText, uint[] suffixArray, Base incomingEdge, long minSaIndex, long maxSaIndex)
        {
            if (text == null || text.Length == 0)
                return null;

            // First find valid text range
            var minTextIndex = long.MaxValue;
            var maxTextIndex = long.MinValue;
            long saIndex = minSaIndex;
            while (saIndex <= maxSaIndex)
            {
                if (suffixArray[saIndex] < minTextIndex)
                    minTextIndex = suffixArray[saIndex];
                if (suffixArray[saIndex] > maxTextIndex)
                    maxTextIndex = suffixArray[saIndex];
                saIndex++;
            }

            TrieNode[] children = new TrieNode[4];

            // $ is at index 0, can skip it
            for (int childIndex = 0; childIndex < 4; childIndex++)
            {
                Base childBase = (Base) childIndex;
                long childMinSaIndex, childMaxSaIndex;
                if (GetSuffixArrayRange(text, fullText, suffixArray, childBase, incomingEdge, minTextIndex, maxTextIndex, out childMinSaIndex, out childMaxSaIndex))
                {
                    /*long subTextLength = suffixArray[childMinSaIndex] >= suffixArray[childMaxSaIndex]
                                          ? suffixArray[childMinSaIndex]
                                          : suffixArray[childMaxSaIndex]; */////////!!
                    var maxChildTextIndex = long.MinValue;
                    saIndex = 0;
                    while (saIndex <= maxSaIndex)
                    {
                        if (suffixArray[saIndex] > maxChildTextIndex)
                            maxChildTextIndex = suffixArray[saIndex];
                        saIndex++;
                    }

                    if (maxChildTextIndex == 0)
                        children[childIndex] = new TrieNode(new TrieNode[4]);
                    else
                    {
                        var subText = text.SubString(0, maxChildTextIndex);
                        children[childIndex] = CreateTrie(subText, fullText, suffixArray, childBase, childMinSaIndex, childMaxSaIndex);
                    }
                    children[childIndex].Min = childMinSaIndex;
                    children[childIndex].Max = childMaxSaIndex;
                    
                }
            }

            TrieNode root = new TrieNode(children);
            return root;
        }

        private static bool GetSuffixArrayRange(IGenomeText text, IGenomeText fullText, uint[] suffixArray, Base dnaBase, Base incomingEdge, long minTextIndex, long maxTextIndex, out long min, out long max)
        {
            min = long.MaxValue;
            max = long.MinValue;

            for (int sufIndex = 1; sufIndex < suffixArray.Length; sufIndex++)
            {
                if (text.GetBase(suffixArray[sufIndex]) == dnaBase
                    && ((incomingEdge == Base.Sentinel) || fullText.GetBase(suffixArray[sufIndex] + 1) == incomingEdge)
                    && suffixArray[sufIndex] + 1 >= minTextIndex
                    && suffixArray[sufIndex] + 1 <= maxTextIndex)
                {
                    if (sufIndex < min)
                        min = sufIndex;
                    if (sufIndex > max)
                        max = sufIndex;
                }
            }

            return min <= max;
        }

        public static TrieNode CreateTrie(GenomeText text, uint[] suffixArray)
        {
            var root = CreateTrie(text, text, suffixArray, Base.Sentinel, 0, suffixArray.Length-1);
            root.Min = 0;
            root.Max = suffixArray.Length - 1;
            return root;
        }
    }
}
