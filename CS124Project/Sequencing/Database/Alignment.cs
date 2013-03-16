namespace CS124Project.Sequencing.Database
{
    public class Alignment
    {
        public virtual long Id { get; set; }
        public virtual byte[] ShortRead { get; set; }
        public virtual uint Position { get; set; }
    }
}
