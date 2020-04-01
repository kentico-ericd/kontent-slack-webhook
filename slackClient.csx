using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Specialized;
 
//A simple C# class to post messages to a Slack channel
//Note: This class uses the Newtonsoft Json.NET serializer available via NuGet
public class SlackClient
{
    private readonly Uri _uri;
    private readonly Encoding _encoding = new UTF8Encoding();
 
    public SlackClient(string urlWithAccessToken)
    {
        _uri = new Uri(urlWithAccessToken);
    }
 
    //Post a message using simple strings
    public string PostMessage(string text, string username = null)
    {
        Payload payload = new Payload() {
            Username = username,
            Text = text
        };
 
        string payloadJson = JsonConvert.SerializeObject(payload);
 
        using (WebClient client = new WebClient())
        {
            NameValueCollection data = new NameValueCollection();
            data["payload"] = payloadJson;
 
            var response = client.UploadValues(_uri, "POST", data);
 
            //The response text is usually "ok"
            return _encoding.GetString(response);
        }
    }

    public class Payload
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
