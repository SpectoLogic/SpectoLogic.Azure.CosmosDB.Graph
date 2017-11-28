using Microsoft.Azure.Graphs;
using SpectoLogic.Azure.Graph.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.Graph
{
    public class GraphCommandFactory
    {
        /// <summary>
        /// This uses undocumented and marked as INTERNAL ONLY functionality of CosmosDB Graph Library
        /// </summary>
        /// <param name="graphConnection"></param>
        /// <param name="outputformat">To be able to call NextAsPOCO outputformat must be set to GraphSON! </param>
        /// <returns></returns>
        public static GraphCommand Create(object graphConnection, OutputFormat outputformat= OutputFormat.GraphSON)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            CultureInfo culture = null; // use InvariantCulture or other if you prefer
            object instantiatedType =
              Activator.CreateInstance(typeof(GraphCommand), flags, null, new object[] { graphConnection, null, null, default(CancellationToken) }, culture);

            GraphCommand cmd = (GraphCommand)instantiatedType;
            cmd.SetOutputFormat(outputformat);  // GraphSON is necessary in order to be able to call "NextAsPOCO"
            return cmd;
        }
    }
}
