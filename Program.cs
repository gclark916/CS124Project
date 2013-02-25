using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;
using CS124Project.SAIS;
using CS124Project.Trie;

namespace CS124Project
{
    class Program
    {
        static void Main(string[] args)
        {
            /*const string genomeFilePath = "genome.bin";
            const string shortreadsFilePath = "reads.bin";
            Generator randomGenerator = new Generator();
            Reader shortReadReader = new Reader();
            randomGenerator.GenerateRandomGenome(genomeFilePath);
            shortReadReader.GenerateReads(genomeFilePath, shortreadsFilePath, 0.1, 4, 1.0);*/

            string testString = File.ReadAllText("chr22NoN.dna");
            //const string testString = "ccaattaattaaggaa";
            GenomeText genome = GenomeText.CreateGenomeFromString(testString);
            ISaisString text = new Level0String(genome);
            Level0MemorySuffixArray suffixArray = new Level0MemorySuffixArray(text);

            
            var bwtBuilder = new StringBuilder();
            for (uint i = 0; i < suffixArray.Length; i++)
            {
                var textIndex = suffixArray[i] - 1 != uint.MaxValue ? suffixArray[i] - 1 : text.Length-1;
                var character = text[textIndex];
                switch (character)
                {
                    case 1:
                        bwtBuilder.Append('A');
                        break;
                    case 2:
                        bwtBuilder.Append('C');
                        break;
                    case 3:
                        bwtBuilder.Append('G');
                        break;
                    case 4:
                        bwtBuilder.Append('T');
                        break;
                }
            }
            File.WriteAllText("bwt.bwt", bwtBuilder.ToString());

            suffixArray.WriteToFile("output.sa");
            /*string testString = "aggagc";
            uint[] suffixArray = new uint[] {6, 3, 0, 5, 2, 4, 1};
            uint[] inverseSA = new uint[suffixArray.Length];
            for (uint i = 0; i < suffixArray.Length; i++)
                inverseSA[suffixArray[i]] = i;
            GenomeText genome = GenomeText.CreateGenomeFromString(testString);
            TrieNode root = new TrieNode(0, testString.Length, suffixArray, inverseSA, genome);
            TrieAligner aligner = new TrieAligner(root, suffixArray, genome);
            var alignments = aligner.GetAlignments(GenomeText.CreateGenomeFromString("g")); 

            foreach (var alignment in alignments)
            {
                Console.WriteLine("{0} {1}", alignment, testString.Substring((int) alignment));
            }*/
        }
    }
}
