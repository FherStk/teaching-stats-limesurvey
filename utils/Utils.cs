public static class Utils{
    private static string? _configFolder;
    public static string ConfigFolder {
        get{
            if(string.IsNullOrEmpty(_configFolder)) _configFolder = GetConfigFolder();
            return _configFolder;
        }
        
        private set{
            _configFolder = value;
        }
    }

    private static string GetConfigFolder(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    } 
}