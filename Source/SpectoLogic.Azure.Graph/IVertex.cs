using Newtonsoft.Json;
using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph
{
    public interface IVertex<InEdge,OutEdge> : IGraphElement
    {
        [JsonIgnore]
        IList<InEdge> InE { get; set; }

        [JsonIgnore]
        IList<OutEdge> OutE { get; set; }
    }
}
