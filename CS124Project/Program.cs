using System;
using CS124Project.Bwt;
using CS124Project.Dna;
using CS124Project.Simulation;

namespace CS124Project
{
    class Program
    {
        static void Main()
        {
            //string refFile = "chr22.dna", donorFile = "chr22_donor.dna", readsFile = "chr22.reads", baseFile = "chr22";
            string refFile = "combined.dna", donorFile = "combined_donor.dna", readsFile = "combined.reads", baseFile = "combined";
            //string refFile = "test.dna", donorFile = "test_donor.dna", readsFile = "test.reads", baseFile="test";

            //Simulator.GenerateReferenceGenomeTextFile("combined_N.dna", refFile);
            //Simulator.GenerateDonorGenomeFromReferenceGenome(refFile, donorFile);
            Simulator.ReadLength = 30;
            //Simulator.GenerateShortReadsFromDonorGenome(donorFile, readsFile, 5, long.MaxValue);

            //var refGenome = DnaSequence.CreateFromBinaryFile(baseFile + ".dna.bin");
            //var refGenome = DnaSequence.CreateGenomeFromTextFile(refFile);
            //refGenome.WriteToBinaryFile(baseFile+".dna.bin");
            //BwtAligner.SavePrecomputedDataToFiles(baseFile, refGenome);
            //GC.Collect();
            //var refGenomeRev = DnaSequence.CreateGenomeFromReverseTextFile(refFile);
            //var refGenomeRev = DnaSequence.CreateReverseGenomeFromBinaryFile(baseFile + ".dna.bin");
            //var refGenomeRev = DnaSequence.CreateFromBinaryFile(baseFile+"_rev.dna.bin");
            //refGenomeRev.WriteToBinaryFile(baseFile+"_rev.dna.bin");
            //BwtAligner.SaveReversePrecomputedDataToFiles(baseFile, refGenomeRev);

            var aligningStart = DateTime.Now;
            BwtAligner aligner = BwtAligner.CreateBwtAlignerFromFiles(baseFile, 30);
            aligner.AlignReadsAndConstructGenome(readsFile, baseFile + "_output.dna", true);
            Console.WriteLine("Took {0} seconds to align reads", DateTime.Now.Subtract(aligningStart).TotalSeconds);

            var accuracy = Simulator.ComputeAccuracy(donorFile, baseFile + "_output.dna");
            Console.WriteLine(accuracy);
        }
    }
}
