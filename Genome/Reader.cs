using System;
using System.Linq;
using System.IO;
using System.Web.UI.DataVisualization.Charting;
using System.Reflection;
using System.Numerics;
using System.Diagnostics;

namespace CS124Project.Genome
{
    class Reader
    {
        private readonly Random _random;
        private readonly StatisticFormula _statisticFormula;

        public Reader()
        {
            _random = new Random();
            Chart chart = new Chart();
            _statisticFormula = chart.DataManipulator.Statistics;
        }

        public void GenerateReads(string genomePath, string readsPath, double meanReadsPerSnp, int maxReadsPerSnp, double standardDeviation)
        {
            // Create probability cutoffs for number of reads at any SNP
            double[] pvalues = new double[maxReadsPerSnp];
            for (int i = 0; i < pvalues.Count(); i++)
                pvalues[i] = _statisticFormula.NormalDistribution(zValue: (1 - meanReadsPerSnp) + (double) i*standardDeviation);

            byte[] readSequence = new byte[9];  // reads are only 60 bits, but can get 4 at a time by reading in an extra byte
            byte[] bitmaskBytes = new byte[8] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF};
            BigInteger bitmask = new BigInteger(bitmaskBytes);
            BigInteger[] bitmasks = new BigInteger[4] {bitmask, bitmask << 2, bitmask << 4, bitmask << 6};

            using (FileStream genomeStream = File.OpenRead(genomePath), 
                outputStream = File.OpenWrite(readsPath))
            {
                int readCount = 0;

                while (genomeStream.Read(readSequence, 0, 9) == 9)
                {
                    BigInteger fourReads = new BigInteger(readSequence);

                    for (int byteReadIndex = 0; byteReadIndex < 4; byteReadIndex++)
                    {
                        double numReadsProbability = _random.NextDouble();

                        int numReads = 0;
                        while (numReads < pvalues.Count() && numReadsProbability > pvalues[numReads])
                            numReads++;

                        readCount += numReads;

                        if (numReads > 0)
                        {
                            BigInteger read = fourReads & bitmasks[byteReadIndex];
                            read >>= (byteReadIndex*2);
                            ulong readBytes = (ulong) read;
                            //Debug.Assert(readBytes.Count() == 8);

                            for (int repeatedReadsIndex = 0; repeatedReadsIndex < numReads; repeatedReadsIndex++)
                                outputStream.Write(BitConverter.GetBytes(readBytes), 0, 8);
                        }
                    }
                }

                Console.Out.Write("Made {0} reads", readCount);
            }
        }
    }
}
