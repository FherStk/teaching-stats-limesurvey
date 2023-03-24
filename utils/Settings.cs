public class Settings
{
    public TeachingStatsSettings? TeachingStats {get; set;}
    
    public LimeSurveySettings? LimeSurvey {get; set;}    
    

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
}