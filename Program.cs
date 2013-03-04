using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.BWT;
using CS124Project.Genome;
using CS124Project.SAIS;
using CS124Project.Trie;

namespace CS124Project
{
    class Program
    {
        static void Main(string[] args)
        {
            string refFile = "chr22.dna", donorFile = "chr22_donor.dna", readsFile = "chr22.reads", baseFile = "chr22";
            //string refFile = "subtotal.dna", donorFile = "subtotal_donor.dna", readsFile = "subtotal.reads", baseFile = "subtotal";
            //string refFile = "test.dna", donorFile = "test_donor.dna", readsFile = "test.reads", baseFile="test";

            //Simulator.GenerateReferenceGenomeTextFile("subtotal_N.dna", refFile);
            //Simulator.GenerateDonorGenomeFromReferenceGenome(refFile, donorFile);
            //Simulator.GenerateShortReadsFromDonorGenome(donorFile, readsFile, 15);

            var refString = File.ReadAllText(refFile);
            var refGenome = DnaSequence.CreateGenomeFromString(refString);
            var refCharArray = refString.ToCharArray();
            Array.Reverse(refCharArray);
            refString = new String(refCharArray);
            var refGenomeRev = DnaSequence.CreateGenomeFromString(refString);
            BwtAligner.SavePrecomputedDataToFiles(baseFile, refGenome, refGenomeRev);

            
            var aligningStart = DateTime.Now;
            BwtAligner aligner = BwtAligner.CreateBwtAlignerFromFiles(baseFile);
            aligner.AlignReadsAndConstructGenome(readsFile, baseFile+"_output.dna");
            Console.WriteLine("Took {0} seconds to align reads", DateTime.Now.Subtract(aligningStart).TotalSeconds);

            var accuracy = Simulator.ComputeAccuracy(donorFile, baseFile + "_output.dna");
            Console.WriteLine(accuracy);
        }
    }
}
