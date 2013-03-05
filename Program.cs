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
            //string refFile = "chr22.dna", donorFile = "chr22_donor.dna", readsFile = "chr22.reads", baseFile = "chr22";
            string refFile = "combined.dna", donorFile = "combined_donor.dna", readsFile = "combined.reads", baseFile = "combined";
            //string refFile = "test.dna", donorFile = "test_donor.dna", readsFile = "test.reads", baseFile="test";

            //Simulator.GenerateReferenceGenomeTextFile("combined_N.dna", refFile);
            //Simulator.GenerateDonorGenomeFromReferenceGenome(refFile, donorFile);
            Simulator.ReadLength = 30;
            //Simulator.GenerateShortReadsFromDonorGenome(donorFile, readsFile, 0.3);

            var refGenome = DnaSequence.CreateGenomeFromTextFile(refFile);
            refGenome.WriteToBinaryFile(baseFile+".dna.bin");
            BwtAligner.SavePrecomputedDataToFiles(baseFile, refGenome);
            var refGenomeRev = DnaSequence.CreateGenomeFromTextFile(refFile);
            refGenomeRev.WriteToBinaryFile(baseFile+"_rev.dna.bin");
            BwtAligner.SaveReversePrecomputedDataToFiles(baseFile, refGenomeRev);

            /*var aligningStart = DateTime.Now;
            BwtAligner aligner = BwtAligner.CreateBwtAlignerFromFiles(baseFile, 30);
            aligner.AlignReadsAndConstructGenome(readsFile, baseFile+"_output.dna", false);
            Console.WriteLine("Took {0} seconds to align reads", DateTime.Now.Subtract(aligningStart).TotalSeconds);

            var accuracy = Simulator.ComputeAccuracy(donorFile, baseFile + "_output.dna");
            Console.WriteLine(accuracy);*/
        }
    }
}
