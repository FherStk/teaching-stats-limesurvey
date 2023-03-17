using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

    private static Settings? _settings;
    public static Settings Settings{
        get{
            if(_settings == null) _settings = LoadSettings();           
            return _settings;
        }
        
        private set{
            _settings = value;
        }
    }

    private static Settings LoadSettings(){
        //Source: https://github.com/aaubry/YamlDotNet
        var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)  // see height_in_inches in sample yml 
        .Build();

        var yml = File.ReadAllText(Path.Combine(ConfigFolder, "settings.yml"));
        return deserializer.Deserialize<Settings>(yml);
    }

    private static string GetConfigFolder(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    }
    
    public static void SerializeSettingsTemplateAsYamlFile(){
        //This is just a test method
        //Source: https://github.com/aaubry/YamlDotNet
        var settings = new Settings
        {
            TeachingStats = new Settings.TeachingStatsSettings(){
                Host = "localhost",
                Username = "postgres",
                Password = "postgres"
            },
            LimeSurvey = new Settings.LimeSurveySettings(){
                Host = "https://limesurvey.elpuig.xeill.net",
                Username = "admin",
                Password = "admin"
            },
            Templates =new Dictionary<string, Settings.SurveySettings>
            {
                {
                    "subject-ccff", new Settings.SurveySettings(){
                        Name = "Subject (CCFF)",
                        Id = 123456
                    }
                },
                {
                    "mentoring-1-ccff", new Settings.SurveySettings(){
                        Name = "Mentoring 1st (CCFF)",
                        Id = 123457
                    }
                },
                {
                    "mentoring-2-ccff", new Settings.SurveySettings(){
                        Name = "Mentoring 2nd (CCFF)",
                        Id = 123458
                    }
                },
                {
                    "school", new Settings.SurveySettings(){
                        Name = "School (General)",
                        Id = 123459
                    }
                }
            }
            
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    
        var yaml = serializer.Serialize(settings);
        System.Console.WriteLine(yaml);
    } 
}