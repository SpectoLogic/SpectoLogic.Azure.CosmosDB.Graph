using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.Graph.Test.Delivery
{
    public interface IEndpointVertex : IVertex<IDeliveryEdge,IDeliveryEdge>
    {
    }
}
