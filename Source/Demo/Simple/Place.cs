using Newtonsoft.Json;
using SpectoLogic.Azure.Graph;
using System;
using System.Collections.Generic;

namespace Demo
{
    /// <summary>
    /// Sample for Vertex
    /// </summary>
    [GraphClass(ElementType = GraphElementType.Vertex)]
    public class Place
    {
        public Place()
        {
            this.Id = Guid.NewGuid().ToString("D");
            this.Label = "place";
        }
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Id)]
        public string Id { get; set; }
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Label)]
        public string Label { get; set; }

        [GraphProperty(PropertyName = "name")] // NOT YET SUPPORTED
        public string name { get; set; }

        public GraphProperty country { get; set; } // GET RICH Information like Id,... TODO: Enable GraphProperty<T> to allow custom property values

        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.InE)]
        public IList<Path> InE { get; set; }

        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.OutE)]
        public IList<Path> OutE { get; set; }
    }

}
