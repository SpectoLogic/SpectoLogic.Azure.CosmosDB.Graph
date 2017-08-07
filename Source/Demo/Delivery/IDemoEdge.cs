using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    public interface IDemoEdge : IEdge<IDemoVertex,IDemoVertex>
    {
        double weight { get; set; }
    }
}
