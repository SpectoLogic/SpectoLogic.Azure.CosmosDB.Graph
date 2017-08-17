using Newtonsoft.Json;

namespace SpectoLogic.Azure.Graph
{
    public interface IEdge : IGraphElement
    {
        [JsonIgnore]
        object InV { get; set; }

        [JsonIgnore]
        object OutV { get; set; }
    }

    public interface IEdge<InVertex, OutVertex> : IGraphElement 
    {
        [JsonIgnore]
        InVertex InV { get; set; }

        [JsonIgnore]
        OutVertex OutV { get; set; }
    }
}
