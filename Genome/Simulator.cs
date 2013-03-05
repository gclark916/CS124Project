using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;

namespace CS124Project.Genome
{
    class Simulator
    {
        private const double SnpDensity = 1.0/1000.0;
        public static int ReadLength = 30;

        public static void GenerateReferenceGenomeTextFile(string infile, string outfile)
        {
            var rng = new Random();
            byte[] genome = File.ReadAllBytes(infile);
            for (int i = 0; i < genome.Length; i++ )
            {
                if (genome[i] == 'N')
                {
                    var randomNumber = rng.NextDouble();
                    if (randomNumber < .25)
                    {
                        genome[i] = BitConverter.GetBytes('A')[0];
                        continue;
                    }
                    if (randomNumber < .5)
                    {
                        genome[i] = BitConverter.GetBytes('C')[0];
                        continue;
                    }
                    if (randomNumber < .75)
                    {
                        genome[i] = BitConverter.GetBytes('G')[0];
                        continue;
                    }
                    genome[i] = BitConverter.GetBytes('T')[0];
                }
            }

            File.WriteAllBytes(outfile, genome);
        }

        public static void GenerateDonorGenomeFromReferenceGenome(string refGenomeFile, string donorGenomeFile)
        {
            var rng = new Random();
            byte[] genome = File.ReadAllBytes(refGenomeFile);
            for (int i = 0; i < genome.Length; i++ )
            {
                var randomNumber = rng.NextDouble();
                if (randomNumber < SnpDensity)
                {
                    if (genome[i] == 'A')
                        genome[i] = BitConverter.GetBytes('C')[0];
                    if (genome[i] == 'C')
                        genome[i] = BitConverter.GetBytes('G')[0];
                    if (genome[i] == 'G')
                        genome[i] = BitConverter.GetBytes('T')[0];
                    if (genome[i] == 'T')
                        genome[i] = BitConverter.GetBytes('A')[0];
                }
            }

            File.WriteAllBytes(donorGenomeFile, genome);
        }

        public static void GenerateShortReadsTextFromDonorGenome(string donorGenomeFile, string readsFile, double coverage)
        {
            Poisson poisson = new Poisson(coverage/ReadLength) {RandomSource = new MersenneTwister()};

            string genome = File.ReadAllText(donorGenomeFile);
            using (var writer = new StreamWriter(File.Open(readsFile, FileMode.Create)))
            {
                for (int donorIndex = 0; donorIndex <= genome.Length-ReadLength; donorIndex++)
                {
                    var numReadsAtPos = poisson.Sample();

                    if (numReadsAtPos > 0)
                    {
                        string read = genome.Substring(donorIndex, ReadLength) + '\n';
                        for (int i = 0; i < numReadsAtPos; i++)
                            writer.Write(read);
                    }
                }
            }
        }

        public static void GenerateShortReadsFromDonorGenome(string donorGenomeFile, string readsFile, double coverage)
        {
            Poisson poisson = new Poisson(coverage / ReadLength) { RandomSource = new MersenneTwister() };

            string genome = File.ReadAllText(donorGenomeFile);
            using (var file = File.Open(readsFile, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                for (int donorIndex = 0; donorIndex <= genome.Length - ReadLength; donorIndex++)
                {
                    var numReadsAtPos = poisson.Sample();

                    if (numReadsAtPos > 0)
                    {
                        string read = genome.Substring(donorIndex, ReadLength);
                        DnaSequence dna = DnaSequence.CreateGenomeFromString(read);
                        for (int i = 0; i < numReadsAtPos; i++)
                            writer.Write(dna.Bytes);
                    }
                }
            }
        }

        public static decimal ComputeAccuracy(string donorFile, string constructedFile)
        {
            using(var donor = File.OpenRead(donorFile))
            using (var constructed = File.OpenRead(constructedFile))
            {
                var donorReader = new BinaryReader(donor);
                var constructedReader = new BinaryReader(constructed);

                long totalBases = donor.Length;
                long differences = 0;
                for (long i = 0; i < totalBases; i++)
                {
                    if (donorReader.ReadByte() != constructedReader.ReadByte())
                        differences++;
                }

                return ((decimal)(totalBases - differences))/(decimal)totalBases;
            }
        }
    }
}
