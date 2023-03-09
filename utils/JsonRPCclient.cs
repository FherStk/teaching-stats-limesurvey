using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 
//Source: https://manual.limesurvey.org/RemoteControl_2_API#How_to_use_LSRC2

namespace JsonRPC {
 
  public class JsonRPCclient {
 
    private int id = 0;
    /// <summary>
    /// Set JSON-RPC webservice URL
    /// </summary>
    public string? URL { set; get; }
    /// <summary>
    /// Set JSON-RPC method
    /// </summary>
    public string? Method { set; get; }
    /// <summary>
    /// Add JSON-RPC params
    /// </summary>
    public JObject Parameters { set; get; }
 
    /// <summary>
    /// Results of the request
    /// </summary>
    public JsonRPCresponse? Response { set; get; }
 
 
    /// <summary>
    /// Create a new object of RPCclient 
    /// </summary>
    public JsonRPCclient() {
      Parameters = new JObject();
      Response = null;
    }
 
    /// <summary>
    /// Create a new object of RPCclient
    /// </summary>
    /// <param name="URL"></param>
    public JsonRPCclient(string URL) {
      this.URL = URL;
      Parameters = new JObject();
      Response = null;
    }
 
    /// <summary>
    /// POST the request and returns server response
    /// </summary>
    /// <returns></returns>
    public string Post() {
      try {
        JObject jobject = new JObject();
        jobject.Add(new JProperty("jsonrpc", "2.0"));
        jobject.Add(new JProperty("id", ++id));
        jobject.Add(new JProperty("method", Method));
        jobject.Add(new JProperty("params", Parameters));
        
        var httpClient = new HttpClient();
        var asyncPost = httpClient.PostAsync(URL, new StringContent(JsonConvert.SerializeObject(jobject), Encoding.UTF8, "application/json"));    
        asyncPost.Wait();
        asyncPost.Result.EnsureSuccessStatusCode();

        var asyncRead = asyncPost.Result.Content.ReadAsStringAsync();
        asyncRead.Wait();        
 
        Response = new JsonRPCresponse();
        Response = JsonConvert.DeserializeObject<JsonRPCresponse>(asyncRead.Result);
        if(Response != null){
            Response.StatusCode = asyncPost.Result.StatusCode;
            return Response.ToString();
        }
 
        return string.Empty;
      }
      catch (Exception e) {
        return e.ToString();
      }
    }
 
    public void ClearParameters() {
      this.Parameters = new JObject();
    }
 
 
  }
 
  public class JsonRPCresponse {
    public int id { set; get; }
    public object? result { set; get; }
    public string? error { set; get; }
    public HttpStatusCode StatusCode { set; get; }
 
    //public JsonRPCresponse() { }
 
    public override string ToString() {
      return "{\"id\":" + id.ToString() + ",\"result\":\"" + (result ?? "").ToString() + "\",\"error\":" + error + ((String.IsNullOrEmpty(error)) ? "null" : "\"" + error + "\"") + "}";
    }
  }
 
}