namespace CS124Project.SAIS
{
    internal interface ISuffixArray
    {
        uint GetCharacterIndex(long suffixArrayIndex);
        long Length { get; }
    }
}