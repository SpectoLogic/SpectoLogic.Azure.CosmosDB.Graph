namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// Allows to declare if a property of a custom class is a "defined" property of a GraphElement
    /// </summary>
    public enum GraphDefinedPropertyType
    {
        None, // Custom Property
        Id,   // Defined as ID (Each Graph Object has an ID)
        Label,// Defined as Label (Each Graph Object has a Label)
        InV,  // Defined as InV (Each Edge has an incomming Vertex)
        OutV, // Defined as OutV (Each Edge has an outgoing Vertex)
        InE,  // Defined as InE (Each Vertex has 0-n incomming Edges)
        OutE  // Defined as OutE (Each Vertex has 0-n outgoing Edges)
    }
}
