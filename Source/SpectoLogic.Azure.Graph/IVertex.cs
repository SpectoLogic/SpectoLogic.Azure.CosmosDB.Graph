using Newtonsoft.Json;
using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph
{
    public interface IVertex<I,O> : IGraphElement where I:new() where O : new()
    {
        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.InE)]
        IList<I> InE { get; set; }

        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.OutE)]
        IList<O> OutE { get; set; }
    }
}
