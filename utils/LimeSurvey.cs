using JsonRPC;
using Newtonsoft.Json.Linq;

public class LimeSurvey : IDisposable{
    public enum Question{
        DEGREE,
        DEPARTMENT,
        SUBJECTCODE,
        SUBJECTNAME,
        GROUP,
        TRAINER,
        QUESTIONS,
        COMMENTS,
        LEVEL,
        TOPIC
    }

    public enum Type{
        SCHOOL,
        MENTORING_1_CCFF,
        MENTORING_2_CCFF,
        SUBJECT_CCFF        
    }

    public string? SessionKey {get; private set;}    

    public JsonRPCclient Client {get; private set;}    


    public LimeSurvey(){
        var settings = Utils.Settings;  
        if(settings == null || settings.LimeSurvey == null) throw new IncorrectSettingsException();      

       
        //Source: https://manual.limesurvey.org/RemoteControl_2_API#How_to_use_LSRC2
        Client = new JsonRPCclient($"{settings.LimeSurvey.Host}/index.php/admin/remotecontrol");
        Client.Method = "get_session_key";
        Client.Parameters.Add("username", settings.LimeSurvey.Username);
        Client.Parameters.Add("password", settings.LimeSurvey.Password);
        Client.Post();
        Client.ClearParameters();

        if(Client.Response == null || Client.Response.result == null) throw new IncorrectSettingsException();      
        else this.SessionKey = Client.Response.result.ToString();
    }

    private static string GetSessionKey(){
        var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
        appFolder = Path.TrimEndingDirectorySeparator(appFolder);
        return Path.Combine(appFolder, "config");
    }

    private string? ReadClientResult(){
        if(this.Client.Response != null && this.Client.Response.result != null) return this.Client.Response.result.ToString();
        else return null;
    }

    public int CopySurvey(int templateID, string newName){
        this.Client.Method = "copy_survey";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", templateID);
        this.Client.Parameters.Add("sNewname", newName);
        this.Client.Post();
        this.Client.ClearParameters();

        var response = JObject.Parse(this.ReadClientResult() ?? "");
        return int.Parse((response["newsid"] ?? "").ToString());
    }

    public Dictionary<Question, List<int>> GetQuestionsIDsByType(int surveyID){
        var IDs = new Dictionary<Question, List<int>>();
        
        this.Client.Method = "list_questions";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        var response = JArray.Parse(this.ReadClientResult() ?? "");
        if(response == null) throw new Exception($"Unable to read properties from the survey ID '{surveyID}'");

        foreach(var q in response){
            var qID = int.Parse((q["qid"] ?? "").ToString());
            
            Question type;            
            if(!Enum.TryParse<Question>((q["title"] ?? "").ToString(), true, out type))
                type = Question.QUESTIONS;           

            List<int> list;
            try{
                list = IDs[type];
            }            
            catch(KeyNotFoundException){
                list = new List<int>();
                IDs.Add(type, list);
            }

            list.Add(qID);                    
        }            

        return IDs;
    }

    public JObject GetQuestionProperties(int questionID){
        this.Client.Method = "get_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public JArray GetAllQuestionsProperties(int surveyID){
        
        this.Client.Method = "list_questions";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        return JArray.Parse(this.ReadClientResult() ?? "");
    }

    public JObject SetQuestionProperties(int questionID, JObject properties){
        this.Client.Method = "set_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Parameters.Add("aQuestionData", properties);
        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public JObject GetSurveySummary(int surveyID){
        this.Client.Method = "get_summary";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("sStatName", "all");

        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public JObject GetSurveyProperties(int surveyID){
        this.Client.Method = "get_survey_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);

        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public int CreateSurveyFromCSV(Type type, string degreeName, string departmentName, string groupName, string trainerName, string subjectCode = "", string subjectName = ""){    
        var template = $"{Path.Combine(Utils.TemplatesFolder, type.ToString().ToLower().Replace("_", "-"))}.txt";    
        var content = File.ReadAllText(template);       

        //Replacing template values
        var surveyName = $"{degreeName} {subjectCode} - {subjectName}";
        content = content.Replace("surveyls_title\t\t\"Assignatura CCFF - Template\"", $"surveyls_title\t\t\"{surveyName}\"");
        content = content.Replace("{'DEPARTMENT'}", "{'" + departmentName + "'}");
        content = content.Replace("{'DEGREE'}", "{'" + degreeName + "'}");
        content = content.Replace("{'GROUP'}", "{'" + groupName + "'}");
        content = content.Replace("{'TRAINER'}", "{'" + trainerName + "'}");
        content = content.Replace("{'SUBJECT_CODE'}", "{'" + subjectCode + "'}");
        content = content.Replace("{'SUBJECT_NAME'}", "{'" + subjectName + "'}");

        //Encoding
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var base64EncodedBytes =  System.Convert.ToBase64String(plainTextBytes);
        
        //Import
        this.Client.Method = "import_survey";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("sImportData", base64EncodedBytes);
        this.Client.Parameters.Add("sImportDataType", "txt");   
        this.Client.Parameters.Add("sDocumentType", "json");
        
        //Post
        this.Client.Post();
        this.Client.ClearParameters();

        //Returing the new survey's ID
        return int.Parse(this.ReadClientResult() ?? "");
    }

    public JObject GetSurveyResponses(int surveyID){
        this.Client.Method = "export_responses";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("sDocumentType", "json");
        this.Client.Parameters.Add("sHeadingType", "full");

        this.Client.Post();
        this.Client.ClearParameters();

        var base64EncodedBytes = System.Convert.FromBase64String(this.ReadClientResult() ?? "");
        return JObject.Parse(System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
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