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

            string testString = File.ReadAllText("small.dna");
            //const string testString = "ccaattaattaaggaa";
            DnaSequence genome = DnaSequence.CreateGenomeFromString(testString);
            ISaisString text = new Level0String(genome);
            Level0MemorySuffixArray suffixArray = new Level0MemorySuffixArray(text);

            using (var output = File.Open("bwt.bwt", FileMode.Create))
            {
                for (uint i = 0; i < suffixArray.Length; i++)
                {
                    var textIndex = suffixArray[i] - 1 != uint.MaxValue ? suffixArray[i] - 1 : text.Length - 1;
                    var character = text[textIndex];
                    switch (character)
                    {
                        case 1:
                            output.Write(BitConverter.GetBytes('A'), 0, 1);
                            break;
                        case 2:
                            output.Write(BitConverter.GetBytes('C'), 0, 1);
                            break;
                        case 3:
                            output.Write(BitConverter.GetBytes('G'), 0, 1);
                            break;
                        case 4:
                            output.Write(BitConverter.GetBytes('T'), 0, 1);
                            break;
                    }
                }
            }

            //suffixArray.WriteToFile("output.sa");
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
