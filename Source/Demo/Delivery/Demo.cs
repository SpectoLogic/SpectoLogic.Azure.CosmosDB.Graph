using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using SpectoLogic.Azure.Graph;
using SpectoLogic.Azure.Graph.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            // Add some vertices
            context.Add(andreas,tina,vienna,venice);

            Delivers andreasTotina = new Delivers(andreas, tina, 1);
            Delivers andreasToVienna = new Delivers(andreas, vienna, 1);
            Delivers andreasToVenice = new Delivers(andreas, venice, 5);
            // Add paths
            context.Add(andreasTotina, andreasToVienna, andreasToVenice);

            await client.UpsertGraphDocumentsAsync(collection, context);

            MemoryGraph partialGraph = new MemoryGraph();
            object graphConnection =  GraphConnectionFactory.Create(client, collection);
            Microsoft.Azure.Graphs.GraphCommand cmd = GraphCommandFactory.Create(graphConnection);
            cmd.SetOutputFormat(OutputFormat.GraphSON); // This is necessary in order to be able to call "NextAsPOCO"

            GraphTraversal verticesTrav = cmd.g().V(); 
            GraphTraversal edgeTrav = cmd.g().E().HasLabel("delivers");

            //var vertices = await verticesTrav.NextAsPOCO<IDemoVertex>(partialGraph);
            //foreach (IDemoVertex v in vertices)
            //{
            //    Console.WriteLine($"Vertex ==> Label:{v.Label} Name:{v.Id}");
            //}



        }
    }
}
