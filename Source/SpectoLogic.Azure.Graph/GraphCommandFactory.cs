using Microsoft.Azure.Graphs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.Graph
{
    public class GraphCommandFactory
    {
        /// <summary>
        /// This uses undocumented and marked as INTERNAL ONLY functionality of CosmosDB Graph Library
        /// </summary>
        /// <param name="graphConnection"></param>
        /// <returns></returns>
        public static GraphCommand Create(object graphConnection)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            CultureInfo culture = null; // use InvariantCulture or other if you prefer
            object instantiatedType =
              Activator.CreateInstance(typeof(GraphCommand), flags, null, new object[] { graphConnection }, culture);
            return (GraphCommand)instantiatedType;
        }
    }
}
