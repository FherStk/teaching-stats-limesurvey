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
            var type = (Question)Enum.Parse(typeof(Question), (q["title"] ?? "").ToString().ToUpper());

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

    public string GetQuestionProperties(int questionID){
        this.Client.Method = "get_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Post();
        this.Client.ClearParameters();

        return this.ReadClientResult() ?? "";
    }

    public string GetAllQuestionsProperties(int surveyID){
        
        this.Client.Method = "list_questions";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", surveyID);
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

        //TODO: cook the json in order to help with the teaching-stats database import
        //TODO: the questions json is needed because the LimeSurvey API does not allow changing the 'equation' property.
        //1. Repeat the question block for each "questions[CODE] and comments"
        //2. id -> evaluation_id (get the next participationID, the same for each repeated block))
        //3. submitdate -> timestamp
        //4. null -> year (extract from timestamp)
        //5. level -> level
        //6. department -> departament (get the 'question' value for the 'title=department' within questions json)
        //7. degree -> degree (get the 'question' value for the 'title=degree' within questions json)
        //8. group -> group (get the 'question' value for the 'title=group' within questions json)
        //9. subjectcode -> subject_code (get the 'question' value for the 'title=subjectcode' within questions json)
        //10. subjectname -> subject_name (get the 'question' value for the 'title=subjectname' within questions json)
        //11. trainer -> trainer subject_name (get the 'question' value for the 'title=trainer' within questions json)
        //12. topic -> topic
        //13. null -> question_sort (get the order from 'questions[SQ00x]' and add the 'comments' at the end)
        //14. aaa -> value (get it from 'questions[SQ00x]' and the 'comments' fields)
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