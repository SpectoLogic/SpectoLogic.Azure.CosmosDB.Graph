using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    [GraphClass(SerializeTypeInformation =true)]
    class Place : IEndpointVertex
    {
        public Place()
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "Place";
            this.InE = new List<IDeliveryEdge>();
            this.OutE = new List<IDeliveryEdge>();
        }
        public IList<IDeliveryEdge> InE { get; set; } 
        public IList<IDeliveryEdge> OutE { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public string name { get; set; }
    }
}
