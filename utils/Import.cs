public class Import
{
    public List<ImportData>? Data {get; set;}    

    public class ImportData{
        public string? Topic {get; set;}
        public string? DegreeName {get; set;}
        public string? DepartmentName {get; set;}
        public string? GroupName {get; set;}
        public string? TrainerName {get; set;}
        public string? SubjectCode {get; set;}
        public string? SubjectName {get; set;}
    }
}