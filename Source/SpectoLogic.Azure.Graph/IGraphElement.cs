using Newtonsoft.Json;

namespace SpectoLogic.Azure.Graph
{
    public interface IGraphElement
    {
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Id, PropertyName = "id")]
        string Id { get; set; }
        [GraphProperty(DefinedProperty = GraphDefinedPropertyType.Label, PropertyName ="label")]
        string Label { get; set; }
    }
}