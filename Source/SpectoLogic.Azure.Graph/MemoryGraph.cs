using System;
using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// Very basic implemenation of a temporary InMemory-Storage for GraphElements (Vertices and Edges)
    /// </summary>
    public class MemoryGraph : IGraphContext
    {
        public MemoryGraph()
        {
            myGraphElements = new Dictionary<string, object>();
        }

        private Dictionary<string, object> myGraphElements;

        private Dictionary<string, object> GraphElements
        {
            get { return myGraphElements; }
        }
        public object this[string id]
        {
            get
            {
                if (myGraphElements.ContainsKey(id))
                    return myGraphElements[id];
                else return null;
            }
            set
            {
                myGraphElements[id] = value;
            }
        }

        IEnumerable<object> IGraphContext.Elements
        {
            get
            {
                return myGraphElements.Values;
            }
        }

        public void Add(params IGraphElement[] elements)
        {
            foreach (IGraphElement element in elements)
            {
                myGraphElements[element.Id] = element;
            }
        }
        public void Add(string id, object graphElement)
        {
            if (graphElement is IGraphElement)
                this.Add((IGraphElement)graphElement);
            else
                myGraphElements[id] = graphElement;
        }
        public void Drop()
        {
            myGraphElements.Clear();
        }
    }
}
