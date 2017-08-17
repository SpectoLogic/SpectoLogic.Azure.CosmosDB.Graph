using Microsoft.Azure.Graphs.Elements;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph.Serializer
{
    public interface IGraphSerializer<T>
    {
        List<T> DeserializeGraphSON(string graphSON);
        JObject ConvertToDocDBJObject(object poco);
    }

    public interface IGraphSerializer
    {
        void SetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance, object value);
        object GetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance);
        void AddDefinedPropertyListItem(GraphDefinedPropertyType propertyType, object targetInstance, object value);

        object GetCustomProperty(string propertyName, object targetInstance);

        object CreateItemInstanceObject(string id);

        object CreateItemInstance(IGraphContext context, string id, out IGraphSerializer serializer);
        //IGraphSerializer CreateGraphSerializerForItem(GraphDefinedPropertyType propertyType);

        Type InType
        {
            get;
        }

        Type OutType
        {
            get;
        }

        IGraphContext GraphContext { get;  }
        bool IsEdge();
        bool IsVertex();
        void Convert(Vertex v, out object result);
        void Convert(Edge v, out object result);
        JObject ConvertToDocDBJObject(object poco);
    }
}
