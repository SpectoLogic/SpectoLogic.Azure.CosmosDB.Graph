using System;

namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// Use to decorate your custom objects to enable custom Names for defined poperties
    /// I would prefer to use an interface for that purpose. So this probably will change
    /// See: IGraphElement, IVertex, IEdge
    /// </summary>
    public class GraphPropertyAttribute : Attribute
    {
        public GraphPropertyAttribute()
        {
            DefinedProperty = GraphDefinedPropertyType.None;
        }
        public GraphDefinedPropertyType DefinedProperty { get; set; }

        /// <summary>
        /// NOT YET SUPPORTED
        /// </summary>
        public string PropertyName { get; set; }
    }
}
