using YamlDotNet.Serialization;

public class Settings
{
    public TeachingStatsSettings? TeachingStats {get; set;}
    
    public LimeSurveySettings? LimeSurvey {get; set;}    

    public MasterData? Data {get; set;}    
    
    public class TeachingStatsSettings{
        public string? Host {get; set;}
        public string? Username {get; set;}
        public string? Password {get; set;}
    }

    public class LimeSurveySettings{
        public string? Host {get; set;}
        public string? Username {get; set;}
        public string? Password {get; set;}
        public int Group {get; set;}
    }

    public class MasterData{
        public CaptionsData? Captions {get; set;}
        public List<DegreeData>? Degrees {get; set;}
    }

    public class CaptionsData{
        public string? Survey {get; set;}
        public string? School {get; set;}
        public string? Services {get; set;}
        
        [YamlMember(Alias ="fct", ApplyNamingConventions = false)]
        public string? FCT {get; set;}
        
        [YamlMember(Alias ="mentoring-1-ccff", ApplyNamingConventions = false)]
        public string? Mentoring1 {get; set;}
        
        [YamlMember(Alias ="mentoring-2-ccff", ApplyNamingConventions = false)]
        public string? Mentoring2 {get; set;}
    }

    public class DegreeData{
        public string? Code {get; set;}
        public string? Acronym {get; set;}
        public string? Name {get; set;}
        public string? Department {get; set;}
        public List<SubjectData>? Subjects {get; set;}
    }

    public class SubjectData{
        public string? Code {get; set;}
        public string? Acronym {get; set;}
        public string? Name {get; set;}
        public List<string>? Ids {get; set;}
        public List<TrainerData>? Trainers {get; set;}
    }    

    public class TrainerData{        
        public string? Name {get; set;}
        public List<string>? Groups {get; set;}
    }    
}