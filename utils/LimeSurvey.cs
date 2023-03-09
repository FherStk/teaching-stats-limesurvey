using JsonRPC;

public class LimeSurvey : IDisposable{
    public string? SessionKey {get; private set;}    

    public JsonRPCclient Client {get; private set;}    


    public LimeSurvey(){
        Dictionary<string, string> items = new Dictionary<string, string>();
        var config = File.ReadAllText(Path.Combine(Utils.ConfigFolder, "limesurvey-connection-string.txt")).Split(";").Where(x => !string.IsNullOrEmpty(x));    

        foreach(var item in config){
            var values = item.Split("=");            
            items.Add(values[0], values[1]);
        }

        //Source: https://manual.limesurvey.org/RemoteControl_2_API#How_to_use_LSRC2
        Client = new JsonRPCclient($"{items["Server"]}/index.php/admin/remotecontrol");
        Client.Method = "get_session_key";
        Client.Parameters.Add("username", items["User Id"]);
        Client.Parameters.Add("password", items["Password"]);
        Client.Post();
        Client.ClearParameters();

        if(Client.Response == null || Client.Response.result == null) throw new ArgumentException("Unable to get the LimeSurvey's session key with the provided data. Please, check the settings within the 'limesurvey-connection-string.txt' file.");
        else this.SessionKey = Client.Response.result.ToString();
    }

    private static string GetSessionKey(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    }

    public void Dispose()
    {
        if(Client == null || string.IsNullOrEmpty(SessionKey)) return;
        
        Client.Method = "release_session_key";
        Client.Parameters.Add("sSessionKey", SessionKey);
        Client.Post();
        Client.ClearParameters();

        SessionKey = String.Empty;
    }
}