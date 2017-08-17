using SpectoLogic.Azure.Graph;
using System;


namespace Demo.Delivery
{
    [GraphClass(SerializeTypeInformation=true)]
    public class DeliveryByTrain : IDeliveryEdge
    {
        public DeliveryByTrain()
        {

        }
        public DeliveryByTrain(IEndpointVertex from, IEndpointVertex to, double weight,string trainId)
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "traindelivers";
            this.InV = to;
            this.OutV = from;
            this.weight = weight;
            this.trainId = trainId;
            if (!from.OutE.Contains(this)) from.OutE.Add(this);
            if (!to.InE.Contains(this)) to.InE.Add(this);
        }

        public string Id { get; set; }
        public string Label { get; set; }
        public IEndpointVertex InV { get; set; }
        public IEndpointVertex OutV { get; set; }
        public double weight { get; set; }
        public string trainId { get; set; }
    }
}
