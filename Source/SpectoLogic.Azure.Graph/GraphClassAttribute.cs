using System;

namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// Allows to decorate your custom classes for easier classification
    /// </summary>
    public class GraphClassAttribute : Attribute
    {
        public GraphClassAttribute()
        {
        }
        public GraphElementType ElementType { get; set; }
    }
}
