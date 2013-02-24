namespace CS124Project.SAIS
{
    internal interface ISuffixArray
    {
        uint this[uint index] { get; }
        long Length { get; }
    }
}