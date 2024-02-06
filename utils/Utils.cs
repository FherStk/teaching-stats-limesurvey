using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class Utils{        
    private static string? _configFolder;
    private static string? _templatesFolder;
    public static string ConfigFolder {
        get{
            if(string.IsNullOrEmpty(_configFolder)) _configFolder = GetConfigFolder();
            return _configFolder;
        }
        
        private set{
            _configFolder = value;
        }
    }

    public static string TemplatesFolder {
        get{
            if(string.IsNullOrEmpty(_templatesFolder)) _templatesFolder = GetConfigFolder().Replace("config", "templates");
            return _templatesFolder;
        }
        
        private set{
            _templatesFolder = value;
        }
    }

    public static string ActionsFolder {
        get{
            if(string.IsNullOrEmpty(_templatesFolder)) _templatesFolder = GetConfigFolder().Replace("config", "actions");
            return _templatesFolder;
        }
        
        private set{
            _templatesFolder = value;
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
        return DeserializeYamlFile<Settings>(Path.Combine(ConfigFolder, "settings.yml"));
    }

    public static T DeserializeYamlFile<T>(string filePath){
        //Source: https://github.com/aaubry/YamlDotNet
        var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

        var yml = File.ReadAllText(filePath);
        return deserializer.Deserialize<T>(yml);
    }

    public static void SerializeYamlFile<T>(T data, string outputPath){
         var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    
        var yaml = serializer.Serialize(data);
        File.WriteAllText(outputPath, yaml, System.Text.Encoding.UTF8);
    }

    private static string GetConfigFolder(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    }
    
    public static void SerializeImportTemplateAsYamlFile(){
        //This is just a test method
        //Source: https://github.com/aaubry/YamlDotNet
        var import = new Survey
        {
            Data = new List<Survey.SurveyData>(){
                new Survey.SurveyData(){
                    Id = "SURVEY-1",
                    DegreeName = "DEGREE",
                    DepartmentName = "DEPTARTMENT",
                    GroupName = "GROUP",
                    Topics = new List<Survey.SurveyTopic>(){
                        new Survey.SurveyTopic(){
                            Topic = "SUBJECT-CCFF",
                            TrainerName = "Teacher 1",
                            SubjectAcronym = "MP01",
                            SubjectName = "Comunicació i atenció al client"
                        },
                        new Survey.SurveyTopic(){
                            Topic = "SUBJECT-CCFF",
                            TrainerName = "Teacher 2",
                            SubjectAcronym = "MP02",
                            SubjectName = "Gestió de la documentació jurídica i empresarial"
                        },
                        new Survey.SurveyTopic(){
                            Topic = "MENTORING-1-CCFF",
                            TrainerName = "Teacher 3",
                        },
                        new Survey.SurveyTopic(){
                            Topic = "SCHOOL"
                        }
                    },
                    Participants = new List<Survey.Participant>(){
                        new Survey.Participant(){
                            Firstname = "Name 1",
                            Lastname = "Surname 1",
                            Email = "name1@domain.com"
                        },
                        new Survey.Participant(){
                            Firstname = "Name 2",
                            Lastname = "Surname 2",
                            Email = "name2@domain.com"
                        }
                    }
                },

                new Survey.SurveyData(){
                    Id = "SURVEY-2",
                    DegreeName = "DEGREE",
                    DepartmentName = "DEPTARTMENT",
                    GroupName = "GROUP",
                    Topics = new List<Survey.SurveyTopic>(){
                        new Survey.SurveyTopic(){
                            Topic = "SUBJECT-CCFF",
                            TrainerName = "Teacher 7",
                            SubjectAcronym = "MP07",
                            SubjectName = "Procés integral de l’activitat comercial"
                        },                       
                        new Survey.SurveyTopic(){
                            Topic = "MENTORING-2-CCFF",
                            TrainerName = "Teacher 9",
                        },
                        new Survey.SurveyTopic(){
                            Topic = "SCHOOL"
                        }                    
                    },
                    Participants = new List<Survey.Participant>(){
                        new Survey.Participant(){
                            Firstname = "Name 1",
                            Lastname = "Surname 1",
                            Email = "name1@domain.com"
                        },
                        new Survey.Participant(){
                            Firstname = "Name 2",
                            Lastname = "Surname 2",
                            Email = "name2@domain.com"
                        },
                        new Survey.Participant(){
                            Firstname = "Name 3",
                            Lastname = "Surname 3",
                            Email = "name3@domain.com"
                        }
                    }
                }
            }
        };
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    
        var yaml = serializer.Serialize(import);
        System.Console.WriteLine(yaml);
    } 
}