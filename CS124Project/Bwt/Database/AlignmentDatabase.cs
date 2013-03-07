using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace CS124Project.Bwt.Database
{
    class AlignmentDatabase
    {
        public AlignmentDatabase(string fileName)
        {
            DbFile = fileName;
            SessionFactory = CreateSessionFactory();
            Session = SessionFactory.OpenSession();
        }

        private string DbFile { get; set; }

        public ISession Session { get; private set; }

        public ISessionFactory SessionFactory { get; private set; }

        private ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                           .Database(SQLiteConfiguration.Standard
                                                        .UsingFile(DbFile))
                           .Mappings(m =>
                                     m.FluentMappings.AddFromAssemblyOf<AlignmentDatabase>())
                           .ExposeConfiguration(BuildSchema)
                           .BuildSessionFactory();
        }

        private void BuildSchema(Configuration config)
        {
            if (File.Exists(DbFile))
                File.Delete(DbFile);

            new SchemaExport(config).Create(false, true);
        }
    }
}