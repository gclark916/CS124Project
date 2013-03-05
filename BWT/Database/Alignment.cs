using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.BWT.Database
{
    public class Alignment
    {
        public virtual long Id { get; set; }
        public virtual byte[] ShortRead { get; set; }
        public virtual uint Position { get; set; }
    }
}
