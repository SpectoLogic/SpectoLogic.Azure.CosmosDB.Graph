using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.CosmosDB
{
    public static class CosmosDBHelper
    {
        #region Helpers
        /// <summary>
        /// Helper to connect easily to CosmosDB
        /// </summary>
        /// <param name="accountUrl"></param>
        /// <param name="accountKey"></param>
        /// <returns></returns>
        public static async Task<DocumentClient> ConnectToCosmosDB(string accountUrl, string accountKey)
        {
            return await ConnectToCosmosDB(accountUrl, accountKey, null);
        }

        public static async Task<DocumentClient> ConnectToCosmosDB(string accountUrl, string accountKey, List<string> preferredRegions)
        {
            DocumentClient result = null;
            ConnectionPolicy connectionPolicy = new ConnectionPolicy() { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp };
            ConsistencyLevel? consistency = null; // Use default, can only be weaker than default!

            if (preferredRegions != null)
            {
                preferredRegions.ForEach(region => connectionPolicy.PreferredLocations.Add(region));
            }

            result = new DocumentClient(new System.Uri(accountUrl), accountKey, connectionPolicy, consistency);
            await result.OpenAsync().ConfigureAwait(false); // Initialize connection (https://msdn.microsoft.com/en-us/magazine/jj991977.aspx)
            return result;
        }

        /// <summary>
        /// Helper to retrieve/create a database in a cosmosdb account
        /// </summary>
        /// <param name="client"></param>
        /// <param name="docDBName"></param>
        /// <returns></returns>
        public static async Task<Database> CreateOrGetDatabase(DocumentClient client, string docDBName)
        {
            Database docDB = client.CreateDatabaseQuery()
                        .Where(d => d.Id == docDBName)
                        .AsEnumerable()
                        .FirstOrDefault();

            if (docDB == null)
                docDB = await client.CreateDatabaseAsync(new Database { Id = docDBName });
            return docDB;
        }

        public static async Task<DocumentCollection> CreateOrGetCollection(
            DocumentClient client,
            Database docDB,
            string colName,
            int throughPut,
            string partitionKey,
            Func<DocumentCollection, IndexingPolicy> defineCustomIndex,
            bool indexChanged)
        {
            return await CreateOrGetCollection(client, docDB, colName, throughPut, partitionKey, defineCustomIndex, indexChanged, false);
        }
        public static async Task<DocumentCollection> CreateCollection(
            DocumentClient client,
            Database docDB,
            string colName,
            int throughPut,
            string partitionKey,
            Func<DocumentCollection, IndexingPolicy> defineCustomIndex,
            bool indexChanged)
        {
            return await CreateOrGetCollection(client, docDB, colName, throughPut, partitionKey, defineCustomIndex, indexChanged, true);
        }

        /// <summary>
        /// Helper to create a new collection
        /// </summary>
        /// <param name="client"></param>
        /// <param name="docDB"></param>
        /// <param name="colName"></param>
        /// <param name="throughPut"></param>
        /// <param name="partitionKey"></param>
        /// <param name="defineCustomIndex"></param>
        /// <param name="indexChanged"></param>
        /// <returns></returns>
        public static async Task<DocumentCollection> CreateOrGetCollection(
            DocumentClient client,
            Database docDB,
            string colName,
            int throughPut,
            string partitionKey,
            Func<DocumentCollection, IndexingPolicy> defineCustomIndex,
            bool indexChanged,
            bool forceRecreation)
        {
            DocumentCollection docDBCollection = new DocumentCollection();

            docDBCollection = client.CreateDocumentCollectionQuery(docDB.SelfLink)
                            .Where(c => c.Id == colName)
                            .AsEnumerable()
                            .FirstOrDefault();

            if ((docDBCollection != null) && (forceRecreation))
            {
                await client.DeleteDocumentCollectionAsync(docDBCollection.SelfLink);
                docDBCollection = null;
            }

            if (docDBCollection == null)
            {
                docDBCollection = new DocumentCollection() { Id = colName };
                var requestOptions = new RequestOptions
                {
                    OfferThroughput = throughPut
                };
                if (!string.IsNullOrEmpty(partitionKey))
                {
                    docDBCollection.PartitionKey.Paths.Add(partitionKey);
                }
                defineCustomIndex?.Invoke(docDBCollection);
                docDBCollection = await client.CreateDocumentCollectionAsync(docDB.SelfLink, docDBCollection, requestOptions);
            }
            else
            {
                if (indexChanged)
                {
                    docDBCollection.IndexingPolicy = (defineCustomIndex?.Invoke(docDBCollection)) ?? docDBCollection.IndexingPolicy;
                    docDBCollection = await client.ReplaceDocumentCollectionAsync(docDBCollection);
                }
            }
            return docDBCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        private static IndexingPolicy DefineCustomIndex(DocumentCollection col)
        {
            return null;
        }
        #endregion
    }
}
