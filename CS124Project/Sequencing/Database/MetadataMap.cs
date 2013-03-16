using FluentNHibernate.Mapping;

namespace CS124Project.Sequencing.Database
{
    class MetadataMap : ClassMap<Metadata>
    {
        public MetadataMap()
        {
            Id(a => a.Id)
                .GeneratedBy.Assigned();
            Map(a => a.GenomeLength)
                .Not.Nullable();
            Map(a => a.ReadLength)
                .Not.Nullable();
        }
    }
}
