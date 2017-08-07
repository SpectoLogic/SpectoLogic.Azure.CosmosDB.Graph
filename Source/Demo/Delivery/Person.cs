using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    [GraphClass(SerializeTypeInformation =true)]
    public class Person : IDemoVertex
    {
        public Person()
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "Person";
            this.InE = new List<IDemoEdge>();
            this.OutE = new List<IDemoEdge>();
        }
        public IList<IDemoEdge> InE { get; set; }
        public IList<IDemoEdge> OutE { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
