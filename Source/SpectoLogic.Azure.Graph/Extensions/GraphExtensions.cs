using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using Microsoft.Azure.Graphs.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpectoLogic.Azure.Graph.Serializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.Graph.Extensions
{
    /// <summary>
    /// Serveral extentension methods to enhance working with Graph and custom objects.
    /// </summary>
    public static class GraphExtensions
    {
        /// <summary>
        /// Allows to set the internal OutputFormat of the GraphCommand thus allowing you to
        /// retrieve GRAPHJSON or GraphsonCompact (without references to Edges) as result from Traversals
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="format"></param>
        public static void SetOutputFormat(this GraphCommand cmd, OutputFormat format)
        {
            Type graphCommandType = typeof(GraphCommand);
            PropertyInfo outputFormatPropertyInfo = graphCommandType.GetProperty("OutputFormat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object originalValue = outputFormatPropertyInfo.GetValue(cmd);
            Type outputFormatType = originalValue.GetType();
            string[] eNames = outputFormatType.GetEnumNames();
            object desiredValue = Enum.Parse(outputFormatType, format.ToString());
            outputFormatPropertyInfo.SetValue(cmd, desiredValue);
        }

        /// <summary>
        /// Can be called on a GraphTraversal to retrieve Vertices or Edges as custom objects.
        /// </summary>
        /// <typeparam name="T">Type of custom object that represents the Vertex or Edge</typeparam>
        /// <param name="trav">Extension Object GraphTraversal</param>
        /// <param name="context">Context that can store GraphElements. If you retrieved vertices and then an edge refering to those vertices they will be automatically linked.</param>
        /// <returns></returns>
        public static async Task<IList<T>> NextAsPOCO<T>(this GraphTraversal trav, IGraphContext context = null) // where T : new()
        {
            IGraphSerializer serializer = null;
            List<T> result = new List<T>();
            /// Verify if the OutputFormat of the GraphCommand was set to GraphSON!
            Type graphTraversalType = typeof(GraphTraversal);
            Type targetType = typeof(T);
            FieldInfo outputFormatPropertyInfo = graphTraversalType.GetField("outputFormat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            string outputFormat = outputFormatPropertyInfo.GetValue(trav).ToString();
            if (!outputFormat.StartsWith("GraphSON")) throw new Exception("OutputFormat of GraphCommand needs to be set to GRAPHSON!");

            // GraphSerializer<T> serializer = GraphSerializerFactory.CreateGraphSerializer<T>(context);

            // Edges and Vertices must be treated separately
            if (GraphSerializer.GetElementType(typeof(T))== GraphElementType.Edge)
            {
                List<Edge> resultSet = await trav.NextAsModelAsync<Edge>();
                foreach (Edge e in resultSet)
                {
                    string typeString = GraphSerializer.GetTypePropertyString(e, out string inVTypeString, out string outVTypeString);
                    if (String.IsNullOrEmpty(typeString))
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, targetType); // Try to instantiate T
                    else
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, typeString);
                    serializer.Convert(e, out object edge);
                    result.Add((T)edge);
                }
            }
            else
            {
                List<Vertex> resultSet = await trav.NextAsModelAsync<Vertex>();
                foreach (Vertex v in resultSet)
                {
                    string typeString = GraphSerializer.GetTypePropertyString(v);
                    if (String.IsNullOrEmpty(typeString))
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, targetType); // Try to instantiate T
                    else
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, typeString);
                    serializer.Convert(v, out object vertex);
                    result.Add((T)vertex);
                }
                //Alternative implementation TODO: Measure speed
                //==========================
                //foreach (var graphSON in trav)
                //{
                //    List<T> partialResult = serializer.DeserializeGraphSON(graphSON);
                //    foreach (T r in partialResult)
                //        result.Add(r);
                //}
            }
            return result;
        }

        /// <summary>
        /// Works simmilar to ExecuteNextAsyc<T> from IDocumentQuery<T> and allows to deserialize the results
        /// to custom objects of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gremlinQuery">ExtensionObject IDocumentQuery</param>
        /// <param name="context">Context that can store GraphElements. If you retrieved vertices and then an edge refering to those vertices they will be automatically linked.</param>
        /// <returns></returns>
        public static async Task<IList<T>> ExecuteNextAsyncAsPOCO<T>(this IDocumentQuery gremlinQuery, IGraphContext context = null) // where T : new()
        {
            List<T> result = new List<T>();
            IGraphSerializer serializer = null;
            Type targetType = typeof(T);

            if (gremlinQuery.GetType().GenericTypeArguments[0] != typeof(Vertex))
            {
                IDocumentQuery<Edge> edgeQuery = gremlinQuery as IDocumentQuery<Edge>;
                var resultSet = await edgeQuery.ExecuteNextAsync<Edge>();
                foreach (Edge e in resultSet)
                {
                    string typeString = GraphSerializer.GetTypePropertyString(e, out string inVTypeString, out string outVTypeString);
                    if (String.IsNullOrEmpty(typeString))
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, targetType); // Try to instantiate T
                    else
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, typeString);
                    serializer.Convert(e, out object edge);
                    result.Add((T)edge);
                }
            }
            else
            {
                IDocumentQuery<Vertex> vertexQuery = gremlinQuery as IDocumentQuery<Vertex>;
                var resultSet = await vertexQuery.ExecuteNextAsync<Vertex>();
                foreach (Vertex v in resultSet)
                {
                    string typeString = GraphSerializer.GetTypePropertyString(v);
                    if (String.IsNullOrEmpty(typeString))
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, targetType); // Try to instantiate T
                    else
                        serializer = GraphSerializerFactory.CreateGraphSerializer(context, typeString);
                    serializer.Convert(v, out object vertex);
                    result.Add((T)vertex);
                }
            }
            return result;
        }

        /// <summary>
        /// ExtensionMethod for DocumentCLient that allows to easily add a custom Graph Element to the CosmoDB
        /// </summary>
        /// <param name="client">Extension Object DocumentClient</param>
        /// <param name="collection">DocumentCollection to add the GraphElement to</param>
        /// <param name="poco"></param>
        /// <returns></returns>
        public static async Task<ResourceResponse<Document>> CreateGraphDocumentAsync(this DocumentClient client, DocumentCollection collection, object poco)
        {
            IGraphSerializer serializer = GraphSerializerFactory.CreateGraphSerializer(null, poco.GetType());
            return await client.CreateDocumentAsync(collection.SelfLink, serializer.ConvertToDocDBJObject(poco));
        }
        public static async Task<ResourceResponse<Document>> CreateGraphDocumentAsync<T>(this DocumentClient client, DocumentCollection collection, T poco) where T : new()
        {
            IGraphSerializer<T> serializer = GraphSerializerFactory.CreateGraphSerializer<T>();
            return await client.CreateDocumentAsync(collection.SelfLink, serializer.ConvertToDocDBJObject(poco));
        }

        public static async Task<ResourceResponse<Document>> UpsertGraphDocumentAsync<T>(this DocumentClient client, DocumentCollection collection, T poco)
        {
            IGraphSerializer serializer = GraphSerializerFactory.CreateGraphSerializer(null, poco.GetType());
            return await client.UpsertDocumentAsync(collection.SelfLink, serializer.ConvertToDocDBJObject(poco));
        }

        public static async Task<List<ResourceResponse<Document>>> UpsertGraphDocumentsAsync(this DocumentClient client, DocumentCollection collection, IGraphContext context)
        {
            List<ResourceResponse<Document>> results = new List<ResourceResponse<Document>>();
            foreach(object poco in context.Elements)
            {
                IGraphSerializer serializer = GraphSerializerFactory.CreateGraphSerializer(context, poco.GetType());
                results.Add(await client.UpsertDocumentAsync(collection.SelfLink, serializer.ConvertToDocDBJObject(poco)));
            }
            return results;
        }
        public static async Task<List<ResourceResponse<Document>>> UpsertGraphDocumentsAsync(this DocumentClient client, DocumentCollection collection, IEnumerable items)
        {
            List<ResourceResponse<Document>> results = new List<ResourceResponse<Document>>();
            foreach (object poco in items)
            {
                IGraphSerializer serializer = GraphSerializerFactory.CreateGraphSerializer(null, poco.GetType());
                results.Add(await client.UpsertDocumentAsync(collection.SelfLink, serializer.ConvertToDocDBJObject(poco)));
            }
            return results;
        }
    }
}
