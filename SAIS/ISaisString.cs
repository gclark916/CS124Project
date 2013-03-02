using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CS124Project.SAIS
{
    interface ISaisString
    {
        long Length { get; }
        TypeArray Types { get; set; }
        int this[long index] { get; }
    }
}
