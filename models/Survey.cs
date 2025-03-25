using Newtonsoft.Json;

public class Survey
{
    public List<SurveyData>? Data {get; set;}    

    public class SurveyData{
        public string? Id {get; set;}        
        public string? DegreeName {get; set;}
        public string? DepartmentName {get; set;}
        public string? GroupName {get; set;}
        public List<SurveyTopic>? Topics {get; set;}
        public List<Participant>? Participants {get; set;}  
    }

    public class SurveyTopic{
        public string? Topic {get; set;}        
        public string? TrainerName {get; set;}
        public string? SubjectAcronym {get; set;}
        public string? SubjectName {get; set;}
    }

    public class Participant{
        [JsonProperty("firstname")]
        public string? Firstname {get; set;}
        
        [JsonProperty("lastname")]
        public string? Lastname {get; set;}

        [JsonProperty("email")]
        public string? Email {get; set;}
    }
}