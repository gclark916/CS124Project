using FluentNHibernate.Mapping;

namespace CS124Project.Bwt.Database
{
    class AlignmentMap : ClassMap<Alignment>
    {
        public AlignmentMap()
        {
            Id(a => a.Id)
                .GeneratedBy.Assigned();
            Map(a => a.Position)
                .Index("IDX_Position")
                .Not.Nullable();
            Map(a => a.ShortRead)
                .Not.Nullable();
        }
    }
}
