using Newtonsoft.Json.Linq;
using System;

namespace SpectoLogic.Azure.Graph.Serializer
{
    public interface IGraphSerializer
    {
        void SetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance, object value);
        object GetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance);
        void AddDefinedPropertyListItem(GraphDefinedPropertyType propertyType, object targetInstance, object value);

        object GetCustomProperty(string propertyName, object targetInstance);

        object CreateListItemInstance(GraphDefinedPropertyType propertyType, string id);
        object CreateItemInstanceObject(string id);

        IGraphSerializer CreateGraphSerializerForListItem(GraphDefinedPropertyType propertyType);
        IGraphSerializer CreateGraphSerializerForItem(GraphDefinedPropertyType propertyType);
        IGraphSerializer CreateGraphSerializerForType(Type itemType);

        bool IsEdge();
        bool IsVertex();

        JObject ConvertToDocDBJObject(object poco);
    }
}
