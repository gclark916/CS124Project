using System;
using System.IO;
using CS124Project.Dna;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;

namespace CS124Project.Simulation
{
    class Simulator
    {
        private const double SnpDensity = 1.0/1000.0;

        public static void GenerateReferenceGenomeTextFile(string infile, string outfile)
        {
            var rng = new Random();
            using (var nFile = File.OpenRead(infile))
            using (var refFile = File.Open(outfile, FileMode.Create))
            {
                var reader = new BinaryReader(nFile);
                var writer = new BinaryWriter(refFile);
                long length = nFile.Length;
                for (long i = 0; i < length; i++)
                {
                    var character = reader.ReadChar();
                    switch (character)
                    {
                        case 'N':
                            var randomNumber = rng.NextDouble();
                            if (randomNumber < .25)
                            {
                                writer.Write('A');
                                break;
                            }
                            if (randomNumber < .5)
                            {
                                writer.Write('C');
                                break;
                            }
                            if (randomNumber < .75)
                            {
                                writer.Write('G');
                                break;
                            }
                            writer.Write('T');
                            break;
                        case 'A':
                        case 'a':
                            writer.Write('A');
                            break;
                        case 'C':
                        case 'c':
                            writer.Write('C');
                            break;
                        case 'G':
                        case 'g':
                            writer.Write('G');
                            break;
                        case 'T':
                        case 't':
                            writer.Write('T');
                            break;
                    }
                }
            }

        }

        public static void GenerateDonorGenomeFromReferenceGenome(string refGenomeFile, string donorGenomeFile)
        {
            var rng = new Random();
            using (var refFile = File.OpenRead(refGenomeFile))
            using (var donorFile = File.Open(donorGenomeFile, FileMode.Create))
            {
                var reader = new BinaryReader(refFile);
                var writer = new BinaryWriter(donorFile);
                for (long i = 0; i < refFile.Length; i++)
                {
                    var randomNumber = rng.NextDouble();
                    if (randomNumber < SnpDensity)
                    {
                        var readChar = reader.ReadChar();
                        if (readChar == 'A')
                            writer.Write('C');
                        else if (readChar == 'C')
                            writer.Write('G');
                        else if (readChar == 'G')
                            writer.Write('T');
                        else if (readChar == 'T')
                            writer.Write('A');
                        else throw new Exception();
                    }
                    else
                    {
                        writer.Write(reader.ReadChar());
                    }
                }
            }
        }

        public static void GenerateShortReadsFromDonorGenome(string donorGenomeFile, string readsFile, int readLength, double coverage, long limit)
        {
            Poisson poisson = new Poisson(coverage / readLength) { RandomSource = new MersenneTwister() };

            using (var donorFile = File.OpenRead(donorGenomeFile))
            using (var file = File.Open(readsFile, FileMode.Create))
            {
                var writer = new BinaryWriter(file);
                var reader = new BinaryReader(donorFile);

                writer.Write(readLength);

                for (long donorIndex = 0, numReads = 0; donorIndex <= donorFile.Length - readLength && numReads < limit; donorIndex++)
                {
                    var numReadsAtPos = poisson.Sample();
                    numReads += numReadsAtPos;

                    if (numReadsAtPos > 0)
                    {
                        donorFile.Seek(donorIndex, SeekOrigin.Begin);
                        var chars = reader.ReadChars(readLength);
                        string read = new string(chars);
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
