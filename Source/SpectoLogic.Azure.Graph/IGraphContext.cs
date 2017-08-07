using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// GraphContext to enable the temporary storage of GraphElements during multiple queries
    /// </summary>
    public interface IGraphContext
    {
        void Add(params IGraphElement[] elements);

        void Add(string id, object element);

        object this[string id]
        {
            get;
            set;
        }

        IEnumerable<object> Elements
        {
            get;
        }

        void Drop();
    }
}
