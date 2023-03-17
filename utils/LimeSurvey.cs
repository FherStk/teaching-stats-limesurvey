using JsonRPC;
using Newtonsoft.Json.Linq;

public class LimeSurvey : IDisposable{
    public enum Question{
        DEGREE,
        DEPARTMENT,
        SUBJECT_CODE,
        SUBJECT_NAME,
        GROUP,
        TRAINER
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

    public Dictionary<Question, int> GetQuestionIDs(int surveyID){
        var IDs = new Dictionary<Question, int>();
        
        this.Client.Method = "list_questions";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        var response = JArray.Parse(this.ReadClientResult() ?? "");
        if(response == null) throw new Exception($"Unable to read properties from the survey ID '{surveyID}'");

        foreach(var q in response){
            var qID = int.Parse((q["qid"] ?? "").ToString());

            switch((q["title"] ?? "").ToString().ToLower()){
                case "degree":
                    IDs.Add(Question.DEGREE, qID);
                    break;

                case "department":
                    IDs.Add(Question.DEPARTMENT, qID);
                    break;

                case "subjectcode":
                    IDs.Add(Question.SUBJECT_CODE, qID);
                    break;

                case "subjectname":
                    IDs.Add(Question.SUBJECT_NAME, qID);
                    break;

                case "group":
                    IDs.Add(Question.GROUP, qID);
                    break;

                case "trainer":
                    IDs.Add(Question.TRAINER, qID);
                    break;
            }          
        }            

        return IDs;
    }

    public string GetQuestionProperties(int questionID){
        this.Client.Method = "get_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Post();
        this.Client.ClearParameters();

        return this.ReadClientResult() ?? "";
    }

    public string SetQuestionProperties(int questionID, JObject properties){
        this.Client.Method = "set_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Parameters.Add("aQuestionData", properties);
        this.Client.Post();
        this.Client.ClearParameters();

        return this.ReadClientResult() ?? "";
    }

    public string GetSurveySummary(int surveyID){
        this.Client.Method = "get_summary";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("sStatName", "all");

        this.Client.Post();
        this.Client.ClearParameters();

        return this.ReadClientResult() ?? "";
    }

    public string GetSurveyProperties(int surveyID){
        this.Client.Method = "get_survey_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);

        this.Client.Post();
        this.Client.ClearParameters();

        return this.ReadClientResult() ?? "";
    }

    public string ExportSurveyResponses(int surveyID){
        this.Client.Method = "export_responses";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("sDocumentType", "json");

        this.Client.Post();
        this.Client.ClearParameters();

        var base64EncodedBytes = System.Convert.FromBase64String(this.ReadClientResult() ?? "");
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
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