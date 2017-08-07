using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    [GraphClass(SerializeTypeInformation =true)]
    class Place : IDemoVertex
    {
        public Place()
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "Place";
            this.InE = new List<IDemoEdge>();
            this.OutE = new List<IDemoEdge>();
        }
        public IList<IDemoEdge> InE { get; set; } 
        public IList<IDemoEdge> OutE { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public string name { get; set; }
    }
}
