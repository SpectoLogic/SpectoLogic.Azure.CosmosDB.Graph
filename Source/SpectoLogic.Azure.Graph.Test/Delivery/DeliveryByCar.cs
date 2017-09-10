using SpectoLogic.Azure.Graph;
using System;

namespace SpectoLogic.Azure.Graph.Test.Delivery
{
    [GraphClass(SerializeTypeInformation=true)]
    public class DeliveryByCar : IDeliveryEdge
    {
        public DeliveryByCar()
        {

        }
        public DeliveryByCar(IEndpointVertex from, IEndpointVertex to, double weight)
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "cardelivers";
            this.InV = to;
            this.OutV = from;
            this.weight = weight;
            if (!from.OutE.Contains(this)) from.OutE.Add(this);
            if (!to.InE.Contains(this)) to.InE.Add(this);
        }

        public string Id { get; set; }
        public string Label { get; set; }
        public IEndpointVertex InV { get; set; }
        public IEndpointVertex OutV { get; set; }
        public double weight { get; set; }
    }
}
