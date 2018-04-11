using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Rest;
using System.Text;

class SubscriptionValidationEventData
{
	public string ValidationCode { get; set; }
}

class SubscriptionValidationResponseData
{
	public string ValidationResponse { get; set; }
}

public static class computeEventResponse
{
	[FunctionName("computeEventResponse")]
	public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
	{
		log.Info($"C# HTTP trigger function begun");
		string response = string.Empty;
		const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";
		const string StorageBlobCreatedEvent = "Microsoft.Storage.BlobCreated";
		const string ResourceWriteSuccessEvent = "Microsoft.Resources.ResourceWriteSuccess";

		var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
		var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
		var secret = Environment.GetEnvironmentVariable("AZURE_SECRET");
		var subscriptionId = "45f7e5d7-593c-47c4-989c-d4745c4a175c";

		string requestContent = await req.Content.ReadAsStringAsync();
		EventGridEvent[] eventGridEvents = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);

		foreach (EventGridEvent eventGridEvent in eventGridEvents)
		{
			JObject dataObject = eventGridEvent.Data as JObject;

			// Deserialize the event data into the appropriate type based on event type 
			if (string.Equals(eventGridEvent.EventType, SubscriptionValidationEvent, StringComparison.OrdinalIgnoreCase))
			{
				var eventData = dataObject.ToObject<SubscriptionValidationEventData>();
				log.Info($"Got SubscriptionValidation event data, validation code: {eventData.ValidationCode}, topic: {eventGridEvent.Topic}");

				// Do any additional validation (as required) and then return back the below response
				var responseData = new SubscriptionValidationResponseData();
				responseData.ValidationResponse = eventData.ValidationCode;
				return req.CreateResponse(HttpStatusCode.OK, responseData);
			}

			else if (string.Equals(eventGridEvent.EventType, ResourceWriteSuccessEvent, StringComparison.OrdinalIgnoreCase))
			{
				var eventData = dataObject.ToObject<ResourceWriteSuccessData>();
				log.Info($"Got VM event data {eventData}");

				var azureServiceTokenProvider = new AzureServiceTokenProvider();

				try
				{
					var serviceCreds = new TokenCredentials(await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/").ConfigureAwait(false));

					var resourceManagementClient =
						new ResourceManagementClient(serviceCreds) { SubscriptionId = subscriptionId };

					var resourceState = await resourceManagementClient.Resources.GetByIdAsync(eventData.ResourceUri, "2017-12-01");
					var properties = resourceState.Properties.ToString()
						.Replace(Environment.NewLine, String.Empty)
						.Replace("\\", String.Empty).Replace(" ", String.Empty);

					log.Info(properties);
					response = properties;

				}
				catch (Exception exp)
				{
					log.Info($"Something went wrong: {exp.Message}");
				}

			}
		}

		return req.CreateResponse(HttpStatusCode.OK, response, "application/json");
	}
}
