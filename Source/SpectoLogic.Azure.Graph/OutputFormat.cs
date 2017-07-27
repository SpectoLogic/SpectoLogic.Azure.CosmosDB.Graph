namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// A public version of the internal Microsoft.Azure.Graphs.OutputFormat which is internal only
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// Use the gremlin console format for outputs.
        /// </summary>
        Regular,
        /// <summary>
        /// Use the GraphSON compact format for outputs.
        /// Output will return valid GraphSON, without including adjacency information.
        /// </summary>
        GraphSONCompact,
        /// <summary>
        /// Use full GraphSON format for outputs.
        /// Output will return valid GraphSON, including adjacency information.
        /// </summary>
        GraphSON
    }

}
