using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CS124Project.Sequencing;
using CS124Project.Dna;
using CS124Project.Simulation;
using NDesk.Options;

namespace CS124Project
{
    class Program
    {
        static int Main(string[] args)
        {
            string baseFile = "default",
                rawReferenceFile = "defaultraw.dna";
            double coverage = 1;
            long readLimit = long.MaxValue;
            int readLength = 30;
            bool generateRefGenome = false,
                 generateDonorGenome = false,
                 generateShortReads = false,
                 createForwardPrecomputedData = false,
                 createReversePrecomputedData = false,
                 alignReads = false,
                 constructGenome = false,
                 computeAccuracy = false;
            var p = new OptionSet {
            { "basefile=", v => baseFile = v },
            { "rawref=", v => rawReferenceFile = v },
            { "coverage=", v => coverage = Double.Parse(v) },
            { "readlimit=", v => readLimit = Int64.Parse(v) },
            { "readlength=", v =>readLength = Int32.Parse(v) },
            { "R|reference",  v => { generateRefGenome = true; } },
            { "D|donor",  v => { generateDonorGenome = true; } },
            { "s|shortreads",  v => { generateShortReads = true; } },
            { "f|forward",  v => { createForwardPrecomputedData = true; } },
            { "r|reverse",  v => { createReversePrecomputedData = true; } },
            { "a|align",  v => { alignReads = true; } },
            { "c|construct",  v => { constructGenome = true; } },
            { "A|accuracy",  v => { computeAccuracy = true; } },
            //{ "h|?|help",   v => help = v != null },
            };
            List<string> extra = p.Parse (args);

            if (extra.Any())
                Console.WriteLine("Unused arguments: {0}", String.Join(" ", extra));

            string refFile = baseFile + ".dna";
            string donorFile = baseFile + "_donor.dna";
            string readsFile = baseFile + ".reads";
            string binaryForwardRef = baseFile + ".dna.bin";
            string binaryReverseRef = baseFile + "_rev.dna.bin";
            string outputFile = baseFile + "_output.dna";

            if (generateRefGenome)
            {
                if (String.Compare(refFile, rawReferenceFile, StringComparison.Ordinal) == 0)
                {
                    Console.WriteLine("The constructed reference genome will be named {0}, which conflicts with the raw reference file. Please rename the raw reference genome", refFile);
                    return -1;
                }
                Console.WriteLine("Generating reference genome.\n\tRaw Reference file = {0}\n\tReference file = {1}", rawReferenceFile, donorFile);
                Simulator.GenerateReferenceGenomeTextFile(rawReferenceFile, refFile);
            }
            if (generateDonorGenome)
            {
                GC.Collect();
                Console.WriteLine("Generating donor genome.\n\tReference file = {0}\n\tDonor file = {1}", refFile, donorFile);
                Simulator.GenerateDonorGenomeFromReferenceGenome(refFile, donorFile);
            }
            if (generateShortReads)
            {
                GC.Collect();
                Console.WriteLine("Generating short reads.\n\tDonor genome = {0}\n\tLength = {1}\n\tCoverage = {2}\n\tLimit = {3}", donorFile, readLength, coverage, readLimit);
                Simulator.GenerateShortReadsFromDonorGenome(donorFile, readsFile, readLength, coverage, readLimit);
            }
            if (createForwardPrecomputedData)
            {
                GC.Collect();
                DnaSequence refGenome;
                if (!File.Exists(binaryForwardRef))
                {
                    Console.WriteLine("Generating forward binary reference genome from {0}", refFile);
                    refGenome = DnaSequence.CreateGenomeFromTextFile(refFile);
                    refGenome.WriteToBinaryFile(binaryForwardRef);
                }
                else
                {
                    refGenome = DnaSequence.CreateFromBinaryFile(binaryForwardRef);
                }

                Console.WriteLine("Generating forward precomputed datastructures from {0}", binaryForwardRef);
                Sequencer.SavePrecomputedDataToFiles(baseFile, refGenome);
            }
            if (createReversePrecomputedData)
            {
                GC.Collect();
                DnaSequence refGenomeRev;
                if (!File.Exists(binaryReverseRef))
                {
                    if (!File.Exists(binaryForwardRef))
                    {
                        Console.WriteLine("Either forward binary genome {0} or reverse binary genome {1} must exist to create reverse precomputed data structures", binaryForwardRef, binaryReverseRef);
                        return -1;
                    }

                    Console.WriteLine("Generating reverse binary reference genome {0} from forward binary genome {1}", binaryReverseRef, binaryForwardRef);
                    refGenomeRev = DnaSequence.CreateReverseGenomeFromBinaryFile(binaryForwardRef);
                    refGenomeRev.WriteToBinaryFile(binaryReverseRef);
                }
                else
                {
                    refGenomeRev = DnaSequence.CreateFromBinaryFile(binaryReverseRef);
                }

                Console.WriteLine("Generating reverse precomputed datastructures from {0}", binaryReverseRef);
                Sequencer.SaveReversePrecomputedDataToFiles(baseFile, refGenomeRev);
            }
            if (alignReads || constructGenome)
            {
                GC.Collect();
                Sequencer aligner = Sequencer.CreateBwtAlignerFromFiles(baseFile, 30);
                TimeSpan alignTime = TimeSpan.MinValue, constructTime = TimeSpan.MinValue;
                if (alignReads)
                {
                    Console.WriteLine("Aligning reads");
                    DateTime alignStart = DateTime.Now;
                    aligner.AlignReads(readsFile);
                    alignTime = DateTime.Now.Subtract(alignStart);
                }
                if (constructGenome)
                {
                    Console.WriteLine("Constructing genome");
                    DateTime constructStart = DateTime.Now;
                    aligner.ConstructGenome(outputFile);
                    constructTime = DateTime.Now.Subtract(constructStart);
                }

                if (alignReads)
                    Console.WriteLine("Took {0} seconds to align reads", alignTime.TotalSeconds);
                if (constructGenome)
                    Console.WriteLine("Took {0} seconds to construct genome", constructTime.TotalSeconds);
            }
            if (computeAccuracy)
            {
                GC.Collect();
                Console.WriteLine("Calculating accuracy");
                var accuracy = Simulator.ComputeAccuracy(donorFile, outputFile);
                Console.WriteLine("Accuracy of {0} compared to {1}: {2}", outputFile, donorFile, accuracy);
            }

            return 0;
        }
    }
}
