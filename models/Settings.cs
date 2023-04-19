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
        public List<DegreeData>? Degrees {get; set;}
    }

    public class DegreeData{
        public string? Name {get; set;}
        public string? Department {get; set;}
        public List<SubjectData>? Subjects {get; set;}
    }

    public class SubjectData{
        public string? Code {get; set;}
        public string? Name {get; set;}
        public List<string>? Ids {get; set;}
        public List<TrainerData>? Trainers {get; set;}
    }    

    public class TrainerData{        
        public string? Name {get; set;}
        public List<string>? Groups {get; set;}
    }    
}