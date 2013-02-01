using System.Security.Cryptography;
using System.IO;

namespace CS124Project.Genome
{
    class Generator
    {
        private readonly RNGCryptoServiceProvider _rngCsp = new RNGCryptoServiceProvider();

        public void GenerateRandomGenome(string fileName)
        {
            byte[] randomGenome = new byte[1000000];
            _rngCsp.GetBytes(randomGenome);
            File.WriteAllBytes(fileName, randomGenome);
        }
    }
}
