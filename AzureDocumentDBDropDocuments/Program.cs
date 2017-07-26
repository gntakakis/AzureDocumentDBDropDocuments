using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDocumentDBDropDocuments
{
    class Program
    {
        //Keys
        private const string connectionEndpointUri = "https://dropdocuments.documents.azure.com:443/";
        private const string primaryKey = "Kl9XbLDeSPjcmoJra7tKkHblfamvhmxFdzcw7LN3Pl44ENCdBYHax6krgmqGgqfpRbUEj73Rml0YAOR2ALJBZg==";

        //Database and collection
        private static string databaseId = @"DatabaseName";
        private static string collectionId = @"CollectionName";
        private Uri collectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

        //Client-side representation for the Azure DocumentDB database service
        private static DocumentClient client;
        
        static void Main(string[] args)
        {
            AzureDropDocuments();
        }

        private async static void AzureDropDocuments()
        {
            //Collect all document names
            List<string> Ids = await CollectIdsAsync();

            //Clear document client
            client = null;

            //Delete Azure documents by id (Required Partition key in the json file)
            foreach (string id in Ids)
                await DeleteDocumentAsync(JsonConvert.DeserializeObject<ResponseID>(id).id);
        }

        public async static Task<List<string>> CollectIdsAsync()
        {
            List<string> ids = new List<string>();

            FeedOptions feedOptions = new FeedOptions();
            feedOptions.EnableCrossPartitionQuery = true;
            feedOptions.PartitionKey = new PartitionKey("yourpartitionkey");

            using (client = new DocumentClient(new Uri(connectionEndpointUri), primaryKey))
            {
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
                var query = client.CreateDocumentQuery(collectionLink, String.Format("Select f.id From f"), feedOptions).AsDocumentQuery();
                while (query.HasMoreResults)
                    foreach (var item in await query.ExecuteNextAsync())
                        ids.Add(Convert.ToString((object)item));
            }
            return ids;
        }

        public static async Task DeleteDocumentAsync(string id)
        {
            Document doc = GetDocument(id);

            RequestOptions requestOptions = new RequestOptions();
            requestOptions.PartitionKey = new PartitionKey("yourpartitionkey");

            await Client.DeleteDocumentAsync(doc.SelfLink, requestOptions);
        }

        #region Document Structure
        private static DocumentClient Client
        {
            get
            {
                if (client == null)
                {
                    string endpoint = connectionEndpointUri;
                    string authKey = primaryKey;
                    Uri endpointUri = new Uri(endpoint);
                    client = new DocumentClient(endpointUri, authKey);
                }
                return client;
            }
        }
        private static Document GetDocument(string id)
        {
            FeedOptions feedOptions = new FeedOptions();
            feedOptions.EnableCrossPartitionQuery = true;
            feedOptions.PartitionKey = new PartitionKey("yourpartitionkey");

            return Client.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), feedOptions)
                .Where(d => d.Id == id)
                .AsEnumerable()
                .FirstOrDefault();
        }
        #endregion
    }
}
