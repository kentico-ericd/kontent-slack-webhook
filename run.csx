#r "Newtonsoft.Json"
#load "slackClient.csx"

using Kentico.Kontent.Delivery;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;

static string slackChannel = "https://hooks.slack.com/services/[...]";
static string previewKey = "";
static string projectId = "";
static string webhookSecret = "";

private static string GenerateHash(string message, string secret)
{
    secret = secret ?? "";
    UTF8Encoding SafeUTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    byte[] keyBytes = SafeUTF8.GetBytes(secret);
    byte[] messageBytes = SafeUTF8.GetBytes(message);
    using (HMACSHA256 hmacsha256 = new HMACSHA256(keyBytes))
    {
        byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashmessage);
    }
}

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    // Get the signature for validation
    IEnumerable<string> headerValues = req.Headers["X-KC-Signature"];
    var sig = headerValues.FirstOrDefault();

    // Get body
    string content;
    using (Stream receiveStr  eam = req.Body)
    {
      using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
      {
        content = readStream.ReadToEnd();
      }
    }

    // Generate a hash using the content and the webhook secret
    var hash = GenerateHash(content, webhookSecret);

    // Verify the notification is valid
    if(sig != hash)
    {
      return new HttpResponseMessage(HttpStatusCode.Unauthorized) {
        ReasonPhrase = "Signature validation failed"
      } as IActionResult;
    }
    
    var settings = new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
      };
    dynamic data = JsonConvert.DeserializeObject(content, settings);

    if (data == null)
    {
      return new HttpResponseMessage(HttpStatusCode.BadRequest) {
        ReasonPhrase = "Please pass data properties in the input object"
      } as IActionResult;
    }

    // Make sure it's a valid operation
    if(data.message.operation.ToString().ToLower() == "publish")
    {
      List<string> lstCodeNames = new List<string>();

      foreach(var item in data.data.items)
      {
        lstCodeNames.Add(item.codename.ToString());
      }
      if(lstCodeNames.Count > 0)
      {
        await PostToSlack(lstCodeNames, log);
      }
      return new HttpResponseMessage(HttpStatusCode.OK) as IActionResult;
    }

    return new HttpResponseMessage(HttpStatusCode.NoContent) as IActionResult;
}

private static async Task PostToSlack(List<string> list, ILogger log)
{
    try
    {
        SlackClient slackclient = new SlackClient(slackChannel);
        IDeliveryClient deliveryclient = DeliveryClientBuilder
            .WithOptions(builder => builder
                .WithProjectId(projectId)
                .UsePreviewApi(previewKey)
            .Build())
            .Build();

        foreach(string codename in list)
        {
            DeliveryItemResponse response = await deliveryclient.GetItemAsync(codename);
            if(response != null)
            {
                var item = response.Item;    
 
                var msg = slackclient.PostMessage(
                    username: "KontentBot",
                    text: "Content update: '" + item.System.Name
                );
            }
            else
            {
                log.LogInformation($"Item {codename} not found!");
            }
        }
    }
    catch (Exception ex)
    {
        log.LogInformation(ex.Message.ToString());
    }
}
