using Newtonsoft.Json;

namespace SpectoLogic.Azure.Graph
{
    public interface IEdge<InVertex, OutVertex> : IGraphElement 
    {
        [JsonIgnore]
        InVertex InV { get; set; }

        [JsonIgnore]
        OutVertex OutV { get; set; }
    }
}
