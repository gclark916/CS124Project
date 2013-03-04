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
        private const int ReadLength = 30;

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

        public static void GenerateShortReadsFromDonorGenome(string donorGenomeFile, string readsFile, double coverage)
        {
            Poisson poisson = new Poisson(coverage/ReadLength) {RandomSource = new MersenneTwister()};

            byte[] readSequence = new byte[9];  // reads are only 60 bits, but can get 4 at a time by reading in an extra byte. Reads are bit arrays [0..60], [2..62], [4..64], [6..66]
            byte[] bitmaskBytes = new byte[8] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF};
            BigInteger bitmask = new BigInteger(bitmaskBytes);
            BigInteger[] bitmasks = new BigInteger[4] {bitmask, bitmask << 2, bitmask << 4, bitmask << 6};


            string genome = File.ReadAllText(donorGenomeFile);
            using (var writer = new StreamWriter(File.Open(readsFile, FileMode.Create)))
            {
                for (int donorIndex = 0; donorIndex < genome.Length-29; donorIndex++)
                {
                    //var numReadsAtPos = poisson.Sample();
                    var numReadsAtPos = 1;

                    if (numReadsAtPos > 0)
                    {
                        string read = genome.Substring(donorIndex, 30) + '\n';
                        for (int i = 0; i < numReadsAtPos; i++)
                            writer.Write(read);
                    }
                }
            }
        }
    }
}
