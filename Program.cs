using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS124Project.Genome;

namespace CS124Project
{
    class Program
    {
        static void Main(string[] args)
        {
            const string genomeFilePath = "genome.bin";
            const string shortreadsFilePath = "reads.bin";
            Generator randomGenerator = new Generator();
            Reader shortReadReader = new Reader();
            randomGenerator.GenerateRandomGenome(genomeFilePath);
            shortReadReader.GenerateReads(genomeFilePath, shortreadsFilePath, 0.1, 4, 1.0);
        }
    }
}
