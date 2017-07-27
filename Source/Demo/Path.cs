using SpectoLogic.Azure.Graph;
using System;

namespace Demo
{
    /// <summary>
    /// Sample for Edge
    /// If the GraphObjectType is not provided the library will detect that this object is an edge
    /// by looking for the properties InV or OutV
    /// </summary>
    [GraphClass(ElementType = GraphElementType.Edge)]
    public class Path
    {
        public Path()
        {
            this.Id = Guid.NewGuid().ToString("D");
        }
        public Path(string label, Place from, Place to, double weight)
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = label;
            this.InV = to;
            this.OutV = from;
            this.weight = weight;
        }

        /// <summary>
        /// GraphProperties 
        /// </summary>
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Id)]
        public string Id { get; set; }
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Label)]
        public string Label { get; set; }

        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.InV)]
        public Place InV { get; set; } 
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.OutV)]
        public Place OutV { get; set; } 
        public double weight { get; set; }
    }
}
