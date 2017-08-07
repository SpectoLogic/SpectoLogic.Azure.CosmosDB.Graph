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
            SerializeTypeInformation = false;
            TypeKey = null;
        }
        public GraphElementType ElementType { get; set; }
        public bool SerializeTypeInformation { get; set; }
        /// <summary>
        /// Allows to store your own unique shorter key to reference a type, instead of the infered 
        /// Assembly TypeName and Name of the Type
        /// </summary>
        public string TypeKey { get; set; }
    }
}
