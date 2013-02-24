using CS124Project.Genome;

namespace CS124Project.SAIS
{
    internal interface ISaisString
    {
        long Length { get; }
        long GetCharacter(long index);
        SaisType GetCharacterType(long index);
        uint GetBucketSize(Base dnaBase);
    }
}