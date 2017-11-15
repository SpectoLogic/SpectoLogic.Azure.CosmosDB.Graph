using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
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
    public class GraphConnectionFactory
    {
        /// <summary>
        /// This uses undocumented and marked as INTERNAL ONLY functionality of CosmosDB Graph Library
        /// </summary>
        /// <param name="documentClient"></param>
        /// <param name="documentCollection"></param>
        /// <returns></returns>
        public static object Create(DocumentClient documentClient, DocumentCollection documentCollection)
        {
            Assembly graphAssembly = typeof(GraphCommand).Assembly;
            Type graphConnectionType = graphAssembly.GetTypes().Where(t => t.Name == "GraphConnection").FirstOrDefault();

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            CultureInfo culture = null; // use InvariantCulture or other if you prefer
            object instantiatedType =
              Activator.CreateInstance(graphConnectionType, flags, null,new object[] { documentClient, documentCollection, null }, culture);

            return instantiatedType;
        }

    }
}
