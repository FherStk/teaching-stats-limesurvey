public class Survey
{
    public List<SurveyData>? Data {get; set;}    

    public class SurveyData{
        public string? Topic {get; set;}
        public string? DegreeName {get; set;}
        public string? DepartmentName {get; set;}
        public string? GroupName {get; set;}
        public string? TrainerName {get; set;}
        public string? SubjectCode {get; set;}
        public string? SubjectName {get; set;}
         public List<Participant>? Participants {get; set;}    
    }

    public class Participant{
        public string? Firstname {get; set;}
        public string? Lastname {get; set;}
        public string? Email {get; set;}
    }
}