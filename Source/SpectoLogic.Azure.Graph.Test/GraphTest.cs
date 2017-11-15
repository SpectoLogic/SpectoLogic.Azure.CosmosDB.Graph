using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Microsoft.Azure.Graphs.Elements;
using SpectoLogic.Azure.CosmosDB;
using SpectoLogic.Azure.Graph;
using SpectoLogic.Azure.Graph.Extensions;
using SpectoLogic.Azure.Graph.Serializer;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using SpectoLogic.Azure.Graph.Test.Delivery;

namespace SpectoLogic.Azure.Graph.Test
{

    [TestClass]
    public class GraphTest
    {
        static string CONFIG_Account_DemoBuild_Hobbit = "http://localhost:8081";
        static string CONFIG_Account_DemoBuild_Hobbit_Graph = "not sure";
        static string CONFIG_Account_DemoBuild_Hobbit_Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        static DocumentClient client;
        static Database db;
        static DocumentCollection collection;

        [ClassInitialize]
        public static async Task GraphTestInitialize(TestContext context)
        {
            object value;
            if (context.Properties.TryGetValue("Account_DemoBuild_Hobbit", out value)) CONFIG_Account_DemoBuild_Hobbit = value.ToString();
            if (context.Properties.TryGetValue("Account_DemoBuild_Hobbit_Graph", out value)) CONFIG_Account_DemoBuild_Hobbit_Graph = value.ToString();
            if (context.Properties.TryGetValue("Account_DemoBuild_Hobbit_Key", out value)) CONFIG_Account_DemoBuild_Hobbit_Key = value.ToString();

            client = await CosmosDBHelper.ConnectToCosmosDB(CONFIG_Account_DemoBuild_Hobbit, CONFIG_Account_DemoBuild_Hobbit_Key);
            db = await CosmosDBHelper.CreateOrGetDatabase(client, "demodb");
            collection = await CosmosDBHelper.CreateCollection(client, db, "thehobbit", 400, null, null, false);
        }

        [TestMethod]
        public void TestConfiguration()
        {
            Assert.IsNotNull(CONFIG_Account_DemoBuild_Hobbit);
            Assert.IsNotNull(CONFIG_Account_DemoBuild_Hobbit_Graph);
            Assert.IsNotNull(CONFIG_Account_DemoBuild_Hobbit_Key);
        }

        [TestMethod]
        public async Task TestSimple()
        {
            Simple.Place cave = new Simple.Place() { name = "Cave of Hobbit" };
            Simple.Place restaurant = new Simple.Place() { name = "Restaurant Green Dragon" };
            Simple.Place europe = new Simple.Place()
            {
                name = "Europe",
                country = GraphProperty.Create("country", "AT", "MetaTag1", "Austria").AddValue("FI", "MetaTag1", "Finnland")
            };
            Simple.Path hobbitPath = new Simple.Path(cave, restaurant, 2); //TODO: find out why 2.3 has an issue

            await client.CreateGraphDocumentAsync<Simple.Place>(collection, cave);
            await client.CreateGraphDocumentAsync<Simple.Place>(collection, restaurant);
            await client.CreateGraphDocumentAsync<Simple.Place>(collection, europe);
            await client.CreateGraphDocumentAsync<Simple.Path>(collection, hobbitPath);

            MemoryGraph partialGraph = new MemoryGraph();

            string gremlinQueryStatement = "g.V().hasLabel('place')";
            Console.WriteLine($"Executing gremlin query as string: {gremlinQueryStatement}");
            var germlinQuery = client.CreateGremlinQuery<Vertex>(collection, gremlinQueryStatement);
            while (germlinQuery.HasMoreResults)
            {
                // It is not required to pass in a context like partialGraph here. This parameter can be omitted.
                foreach (var result in await germlinQuery.ExecuteNextAsyncAsPOCO<Simple.Place>(partialGraph))
                {
                    Console.WriteLine($"Vertex ==> Label:{result.Label} Name:{result.name}");
                }
            }

            #region EXPERIMENTAL DEMO
            /// =================================================================================================
            /// IMPORTANT: The following code makes use of the internal GraphTraversal class, which should not
            /// be used according to the documentation of Microsofts Graph Library. Use at your own risk.
            /// =================================================================================================
            // Connect with GraphConnection
            object graphConnection = GraphConnectionFactory.Create(client, collection);
            // Drop previous context (optional if the same graph)
            partialGraph.Drop();
            Microsoft.Azure.Graphs.GraphCommand cmd = GraphCommandFactory.Create(graphConnection);

            GraphTraversal placeTrav = cmd.g().V().HasLabel("place");
            GraphTraversal edgeTrav = cmd.g().E().HasLabel("path");
            {
                Console.WriteLine("Retrieving all places with 'NextAsPOCO'-Extension on GraphTraversal ");
                // Returns a list of all vertices for place
                var places = await placeTrav.NextAsPOCO<Simple.Place>(partialGraph);
                foreach (Simple.Place place in places)
                {
                    Console.WriteLine($"Vertex ==> Label:{place.Label} Name:{place.name}");
                }
            }

            // Drop previous context (optional if the same graph)
            partialGraph.Drop();
            IGraphSerializer<Simple.Place> placeGraphSerializer = GraphSerializerFactory.CreateGraphSerializer<Simple.Place>(partialGraph);
            foreach (var p in placeTrav)
            {
                IList<Simple.Place> places = placeGraphSerializer.DeserializeGraphSON(p); // Returns more than one result in each call
                foreach (Simple.Place place in places)
                {
                    Console.WriteLine($"Vertex ==> Label:{place.Label} Name:{place.name}");
                    Console.WriteLine("Serializing to CosmosDB internal represenation: ");
                    string docDBJson = placeGraphSerializer.ConvertToDocDBJObject(place).ToString();
                    Console.WriteLine($"JSON ==> {docDBJson}");
                }
            }

            Console.WriteLine("Iterating over GraphTraversal Paths (Edges) and deserializing GraphSON to custom object ");
            IGraphSerializer<Simple.Path> pathGraphSerializer = GraphSerializerFactory.CreateGraphSerializer<Simple.Path>(partialGraph);
            foreach (var p in edgeTrav)
            {
                IList<Simple.Path> paths = pathGraphSerializer.DeserializeGraphSON(p); // Returns more than one result in each loop
                foreach (Simple.Path path in paths)
                {
                    Console.WriteLine($"Edge ==> Label:{path.Label} Weight:{path.weight}");
                    Console.WriteLine("Serializing to CosmosDB internal represenation: ");
                    string docDBJson = pathGraphSerializer.ConvertToDocDBJObject(path).ToString();
                    Console.WriteLine($"JSON ==> {docDBJson}");
                }
            }
            #endregion

        }

        [TestMethod]
        public async Task TestDelivery()
        {
            Assert.IsNotNull(GraphTest.client, "DocumentDB Client is null");
            Assert.IsNotNull(GraphTest.collection, "DocumentDB Collection is null");
            Assert.IsNotNull(GraphTest.db, "DocumentDB Database is null");

            MemoryGraph context = new MemoryGraph();
            Delivery.Person andreas = new Person() { FirstName = "Andreas", LastName = "Pollak" };
            Delivery.Person tina = new Person() { FirstName = "Tina", LastName = "Pollak" };
            Delivery.Place vienna = new Place() { name = "Vienna" };
            Delivery.Place venice = new Place() { name = "Venice" };
            context.Add(andreas, tina, vienna, venice);

            Assert.AreEqual("Andreas", andreas.FirstName);
            Assert.AreEqual("Pollak", andreas.LastName);

            Delivery.DeliveryByCar andreasTotina = new DeliveryByCar(andreas, tina, 1);
            Delivery.DeliveryByCar andreasToVienna = new DeliveryByCar(andreas, vienna, 1);
            Delivery.DeliveryByCar andreasToVenice = new DeliveryByCar(andreas, venice, 5);
            Delivery.DeliveryByTrain andreasToVeniceByTrain = new DeliveryByTrain(andreas, venice, 3, "TR0012");
            Delivery.DeliveryByTrain tinaToViennaByTrain = new DeliveryByTrain(tina, vienna, 2, "TR0042");
            // Add Paths/Edges
            context.Add(andreasTotina, andreasToVienna, andreasToVenice, andreasToVeniceByTrain, tinaToViennaByTrain);

            await client.UpsertGraphDocumentsAsync(collection, context);

            MemoryGraph partialGraph = new MemoryGraph();

            /// Try both queries in both directions first vertices then edges then edges and vertices
            var query = client.CreateGremlinQuery<Vertex>(collection, "g.V()");
            int count = 0;
            foreach (var result in await query.ExecuteNextAsyncAsPOCO<IEndpointVertex>(partialGraph))
            {
                if (result is Delivery.Person)
                {
                    Delivery.Person person = ((Delivery.Person)result);
                    Assert.AreEqual("Pollak", person.LastName);
                    Assert.AreEqual("Person", person.Label);
                    if (person.FirstName=="Andreas")
                        Assert.AreEqual("Andreas", person.FirstName);
                    else
                        Assert.AreEqual("Tina", person.FirstName);
                }
                if (result is Delivery.Place)
                {
                    Delivery.Place place = ((Delivery.Place)result);
                    Assert.AreEqual("Place", place.Label);
                    Assert.IsTrue(((place.name == "Vienna") ||(place.name == "Venice")));
                }
                count++;
            }
            Assert.AreEqual(4, count);

            var edgeQuery = client.CreateGremlinQuery<Edge>(collection, "g.E()");
            foreach (var result in await edgeQuery.ExecuteNextAsyncAsPOCO<IDeliveryEdge>(partialGraph))
            {
                Debug.WriteLine(result.GetType().Name);
            }

            // await TestDeliveryExperimental();
        }

        public async Task TestDeliveryExperimental()
        {
            MemoryGraph partialGraph = new MemoryGraph();

            #region EXPERIMENTAL DEMO
            /// =================================================================================================
            /// IMPORTANT: The following code makes use of the internal GraphTraversal class, which should not
            /// be used according to the documentation of Microsofts Graph Library. Use at your own risk.
            /// =================================================================================================
            var graphConnection = GraphConnectionFactory.Create(client, collection);
            GraphCommand cmd = GraphCommandFactory.Create(graphConnection);
            partialGraph.Drop();

            GraphTraversal personTrav = cmd.g().V().HasLabel("Person");
            {
                var persons = await personTrav.NextAsPOCO<IEndpointVertex>(partialGraph);
                foreach (Delivery.Person p in persons)
                {
                    Console.WriteLine($"Vertex ==> Label:{p.Label} Name:{p.FirstName}");
                }
            }
            #endregion
        }

    }
}
