using Newtonsoft.Json;

namespace SpectoLogic.Azure.Graph
{
    public interface IEdge<I, O> : IGraphElement where I : new() where O : new()
    {
        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.InV)]
        I InV { get; set; }

        [JsonIgnore]
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.OutV)]
        O OutV { get; set; }
    }
}
