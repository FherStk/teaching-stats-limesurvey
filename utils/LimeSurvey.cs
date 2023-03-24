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

    public enum Topic{
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

    public void Dispose()
    {
        if(Client == null || string.IsNullOrEmpty(SessionKey)) return;
        
        Client.Method = "release_session_key";
        Client.Parameters.Add("sSessionKey", SessionKey);
        Client.Post();
        Client.ClearParameters();

        SessionKey = String.Empty;
    }
#region Survey
    public JArray ListSurveys(){
        this.Client.Method = "list_surveys";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);        
        this.Client.Post();
        this.Client.ClearParameters();

        return JArray.Parse(this.ReadClientResult() ?? "");
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

    public int CreateSurveyFromCSV(Topic topic, string degreeName, string departmentName, string groupName, string trainerName, string subjectCode = "", string subjectName = ""){    
        var template = $"{Path.Combine(Utils.TemplatesFolder, topic.ToString().ToLower().Replace("_", "-"))}.txt";    
        var content = File.ReadAllText(template);       

        //Setting up template values
        var surveyName = $"{degreeName} {subjectCode}: {subjectName}";
        if(!string.IsNullOrEmpty(trainerName)) surveyName += $" ({trainerName})";
        
        var description = string.Empty;

        switch(topic){
            case Topic.SUBJECT_CCFF:
                description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                <ol style='text-align: left;'>
                                    <li>Si no estàs matriculat d'aquest Mòdul Professional o en trobes a faltar enquestes sobre altres Mòduls que tens matriculats, posa't en contacte amb el teu tutor.</li>
                                    <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                    <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                    <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                </ol>";
            break;
        }

        //Replacing template values
        content = content.Replace("{'TITLE'}", $"{surveyName}");
        content = content.Replace("{'DESCRIPTION'}", $"{description}");
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

    public JArray GetSurveyQuestions(int surveyID){
        
        this.Client.Method = "list_questions";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID_org", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        return JArray.Parse(this.ReadClientResult() ?? "");
    }
    
    public JObject GetSurveyResponses(int surveyID){
        this.Client.Method = "export_responses";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("sDocumentType", "json");
        //TODO: The idea was to export also the question name so all the information neede came within a unique JSON but no question statement is beeing exportes... weird...
        // this.Client.Parameters.Add("sLanguageCode", "");
        // this.Client.Parameters.Add("sHeadingType", "full");
        // this.Client.Parameters.Add("sResponseType", "long");

        this.Client.Post();
        this.Client.ClearParameters();

        var base64EncodedBytes = System.Convert.FromBase64String(this.ReadClientResult() ?? "");
        return JObject.Parse(System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
    }

    public Topic? GetSurveyTopic(int surveyID){      
        var questions = GetSurveyQuestions(surveyID);
        foreach(var item in questions.Children()){
            if((item["title"] ?? "").ToString() == "topic"){
                var value = (item["question"] ?? "").ToString();
                
                if(value.Contains("topic")){
                    //Note: the question will come as the "Topic" enum needs, because has been setup like this within the 'question' property.
                    var topic = value.ToString().Split(new char[]{'{', '}'})[1].Trim('\'');
                    return (Topic)Enum.Parse(typeof(Topic), topic.Replace("-", "_"), true);    
                }
            }
        }
      
        return null;
    }
#endregion
#region Questions
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
    
    public JObject SetQuestionProperties(int questionID, JObject properties){
        this.Client.Method = "set_question_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iQuestionID", questionID);

        this.Client.Parameters.Add("aQuestionData", properties);
        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    
#endregion
#region Private
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
#endregion 
}