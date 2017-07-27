namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// GraphContext to enable the temporary storage of GraphElements during multiple queries
    /// </summary>
    public interface IGraphContext
    {
        object this[string id]
        {
            get;
            set;
        }

        void Drop();
    }
}
