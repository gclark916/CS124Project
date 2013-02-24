using System;
using System.Collections.Generic;
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

            const string testString = "ccaattaattaaggaa";
            GenomeText genome = GenomeText.CreateGenomeFromString(testString);
            ISaisString text = new Level0String(genome);
            Level0SuffixArray suffixArray = new Level0SuffixArray("text.sa", text);


            /*uint[] suffixArray = new uint[] {6, 3, 0, 5, 2, 4, 1};
            TrieNode root = TrieNode.CreateTrie(text, suffixArray);
            TrieAligner aligner = new TrieAligner(root, suffixArray, text);
            var alignments = aligner.GetAlignments(GenomeText.CreateGenomeFromString("g")); 

            foreach (var alignment in alignments)
            {
                Console.WriteLine("{0} {1}", alignment, testString.Substring((int) alignment));
            }*/
        }
    }
}
