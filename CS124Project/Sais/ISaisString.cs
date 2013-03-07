using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.Sais
{
    interface ISaisString
    {
        long Length { get; }
        TypeArray Types { get; set; }
        long this[long index] { get; }
    }
}
