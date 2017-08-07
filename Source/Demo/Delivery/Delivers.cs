using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    [GraphClass(SerializeTypeInformation =true)]
    public class Delivers : IDemoEdge
    {
        public Delivers()
        {

        }
        public Delivers(IDemoVertex from, IDemoVertex to, double weight)
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "delivers";
            this.InV = to;
            this.OutV = from;
            this.weight = weight;
            if (!from.OutE.Contains(this)) from.OutE.Add(this);
            if (!to.InE.Contains(this)) to.InE.Add(this);
        }

        public string Id { get; set; }
        public string Label { get; set; }
        public IDemoVertex InV { get; set; }
        public IDemoVertex OutV { get; set; }
        public double weight { get; set; }
    }
}
