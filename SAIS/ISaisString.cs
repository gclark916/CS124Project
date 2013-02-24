namespace CS124Project.SAIS
{
    internal interface ISaisString
    {
        uint Length { get; }
        uint[] BucketIndices { get; }
        TypeArray Types { get; set; }
        uint this[uint index] { get; }
    }
}