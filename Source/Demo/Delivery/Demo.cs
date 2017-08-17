using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Microsoft.Azure.Graphs.Elements;
using SpectoLogic.Azure.Graph;
using SpectoLogic.Azure.Graph.Extensions;
using System;
using System.Threading.Tasks;

namespace Demo.Delivery
{
    public class Demo
    {
        public async Task Execute(DocumentClient client, DocumentCollection collection)
        {
            MemoryGraph context = new MemoryGraph();

            Person andreas = new Person() { FirstName="Andreas", LastName="Pollak"  }; 
            Person tina = new Person() { FirstName="Tina",LastName="Pollak" };
            Place vienna = new Place() { name = "Vienna" };
            Place venice = new Place() { name = "Venice" };
            // Add EndPoints
            context.Add(andreas,tina,vienna,venice);

            DeliveryByCar andreasTotina = new DeliveryByCar(andreas, tina, 1);
            DeliveryByCar andreasToVienna = new DeliveryByCar(andreas, vienna, 1);
            DeliveryByCar andreasToVenice = new DeliveryByCar(andreas, venice, 5);
            DeliveryByTrain andreasToVeniceByTrain = new DeliveryByTrain(andreas, venice, 3, "TR0012");
            DeliveryByTrain tinaToViennaByTrain = new DeliveryByTrain(tina, vienna, 2, "TR0042");
            // Add Paths/Edges
            context.Add(andreasTotina, andreasToVienna, andreasToVenice, andreasToVeniceByTrain, tinaToViennaByTrain);

            await client.UpsertGraphDocumentsAsync(collection, context);

            MemoryGraph partialGraph = new MemoryGraph();

            /// Try both queries in both directions first vertices then edges then edges and vertices
            var query = client.CreateGremlinQuery<Vertex>(collection, "g.V()");
            foreach (var result in await query.ExecuteNextAsyncAsPOCO<IEndpointVertex>(partialGraph))
            {
                Console.WriteLine(result.GetType().Name);
            }
            var edgeQuery = client.CreateGremlinQuery<Edge>(collection, "g.E()");
            foreach (var result in await edgeQuery.ExecuteNextAsyncAsPOCO<IDeliveryEdge>(partialGraph))
            {
                Console.WriteLine(result.GetType().Name);
            }

            #region EXPERIMENTAL DEMO
            /// =================================================================================================
            /// IMPORTANT: The following code makes use of the internal GraphTraversal class, which should not
            /// be used according to the documentation of Microsofts Graph Library. Use at your own risk.
            /// =================================================================================================
            var graphConnection =  GraphConnectionFactory.Create(client, collection);
            GraphCommand cmd = GraphCommandFactory.Create(graphConnection);
            partialGraph.Drop();

            GraphTraversal personTrav = cmd.g().V().HasLabel("Person");
            {
                var persons = await personTrav.NextAsPOCO<IEndpointVertex>(partialGraph);
                foreach (Person p in persons)
                {
                    Console.WriteLine($"Vertex ==> Label:{p.Label} Name:{p.FirstName}");
                }
            }
            #endregion
        }
    }
}
