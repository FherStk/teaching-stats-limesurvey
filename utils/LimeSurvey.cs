using JsonRPC;

public class LimeSurvey{
    public string? SessionKey {get; private set;}

    public LimeSurvey(){
        Dictionary<string, string> items = new Dictionary<string, string>();
        var config = File.ReadAllText(Path.Combine(Utils.ConfigFolder, "limesurvey-connection-string.txt")).Split(";");    

        foreach(var item in config){
            var values = item.Split("=");
            items.Add(values[0], values[1]);
        }

        //Source: https://manual.limesurvey.org/RemoteControl_2_API#How_to_use_LSRC2
        string Baseurl = $"{items["Server"]}index.php?r=admin/remotecontrol";
        JsonRPCclient client = new JsonRPCclient(Baseurl);
        client.Method = "get_session_key";
        client.Parameters.Add("username", items["User Id"]);
        client.Parameters.Add("password", items["Password"]);
        client.Post();

        if(client.Response != null && client.Response.result != null) 
            this.SessionKey = client.Response.result.ToString();
    }

    private static string GetSessionKey(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    } 

}