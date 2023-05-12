using JsonRPC;
using Newtonsoft.Json;
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
        SUBJECT_CCFF,
        STAFF,
        TEACHERS        
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
    public JArray ListSurveys(char activeValue = ' '){
        this.Client.Method = "list_surveys";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);        
        this.Client.Post();
        this.Client.ClearParameters();
        
        var group = (Utils.Settings.LimeSurvey == null ? 0 : Utils.Settings.LimeSurvey.Group);
        var list = JArray.Parse(this.ReadClientResult() ?? "");
        
        var filtered = new JArray();
        foreach(var survey in list){
            var id = int.Parse((survey["sid"] ?? "").ToString());
            var props = GetSurveyProperties(id);
            var gsid = int.Parse((props["gsid"] ?? "").ToString());

            if(gsid == group){
                var active = char.Parse((props["active"] ?? "").ToString());
                var expired = (props["expires"] ?? "").ToString();

                if(string.IsNullOrEmpty(expired) && (activeValue == ' ' || activeValue == active)) filtered.Add(survey);
            } 
        }
        
        return filtered;  
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

    public int CreateSurvey(Survey.SurveyData data){    
        var topic = (LimeSurvey.Topic)Enum.Parse(typeof(LimeSurvey.Topic), (data.Topic ?? "").Replace("-", "_"), true);        
        var template = $"{Path.Combine(Utils.TemplatesFolder, topic.ToString().ToLower().Replace("_", "-"))}.txt";    
        var content = File.ReadAllText(template);               

        //Setting up template values
        var surveyName = string.Empty;
        var description = string.Empty;
        var captions = (Utils.Settings.Data == null ? null : Utils.Settings.Data.Captions);
        switch(topic){
            case Topic.SCHOOL:
                data.SubjectCode = "Centre";
                data.SubjectName = "Instal·lacions i estada";
                surveyName = $"{data.GroupName} {(captions == null ? "SCHOOL" : captions.School)}";
                description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                <ol style='text-align: left;'>
                                    <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                    <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                    <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                </ol>";                
                break;

            case Topic.MENTORING_1_CCFF:
                data.SubjectCode = "Tutoria";
                data.SubjectName = "1er Curs";
                surveyName = $"{data.GroupName} {(captions == null ? "MENTORING 1ST" : captions.Mentoring1)} ({data.TrainerName})";
                description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                <ol style='text-align: left;'>
                                    <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                    <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                    <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                </ol>";
                break;

            case Topic.MENTORING_2_CCFF:
                data.SubjectCode = "Tutoria";
                data.SubjectName = "2n Curs";
                surveyName = $"{data.GroupName} {(captions == null ? "MENTORING 2ND" : captions.Mentoring2)} ({data.TrainerName})";
                description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                <ol style='text-align: left;'>
                                    <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                    <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                    <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                </ol>";
                break;

            case Topic.SUBJECT_CCFF:
                surveyName = $"{data.GroupName} {data.SubjectCode}: {data.SubjectName} ({data.TrainerName})";
                description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                <ol style='text-align: left;'>
                                    <li>Si no estàs matriculat d'aquest Mòdul Professional o en trobes a faltar enquestes sobre altres Mòduls que tens matriculats, posa't en contacte amb el teu tutor.</li>
                                    <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                    <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                    <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                </ol>";
            break;

            case Topic.STAFF:
            case Topic.TEACHERS:
            default:
                throw new NotImplementedException();
        }

                        
        //Replacing template values
        content = content.Replace("{'TITLE'}", $"{surveyName}");
        content = content.Replace("{'DESCRIPTION'}", $"{description}");
        content = content.Replace("{'DEPARTMENT'}", "{'" + data.DepartmentName + "'}");
        content = content.Replace("{'DEGREE'}", "{'" + data.DegreeName + "'}");
        content = content.Replace("{'GROUP'}", "{'" + data.GroupName + "'}");
        content = content.Replace("{'TRAINER'}", "{'" + data.TrainerName + "'}");

        if(topic == Topic.SUBJECT_CCFF){
            content = content.Replace("{'SUBJECT_CODE'}", "{'" + data.SubjectCode + "'}");
            content = content.Replace("{'SUBJECT_NAME'}", "{'" + data.SubjectName + "'}");
        }

        //Encoding
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var base64EncodedBytes =  System.Convert.ToBase64String(plainTextBytes);
        
        //Import
        this.Client.Method = "import_survey";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("sImportData", base64EncodedBytes);
        this.Client.Parameters.Add("sImportDataType", "txt");   
        this.Client.Post();
        this.Client.ClearParameters();

        //Returing the new survey's ID
        int newID = int.Parse(this.ReadClientResult() ?? "");
        SetSurveyProperties(newID, JObject.Parse(@"{'gsid': " + (Utils.Settings.LimeSurvey == null ? 1 : Utils.Settings.LimeSurvey.Group) + "}"));

        //Creating the participants table
        this.Client.Method = "activate_tokens";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", newID);        
        this.Client.Post();
        this.Client.ClearParameters();

        //Adding participants
        if(data.Participants != null && data.Participants.Count > 0) AddSurveyParticipants(newID, data.Participants);

        return newID;
    }

    public JArray AddSurveyParticipants(int surveyID, List<Survey.Participant> parts){
        var data = JsonConvert.SerializeObject(parts);        

        this.Client.Method = "add_participants";        
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);               
        this.Client.Parameters.Add("aParticipantData", JArray.Parse(data));                   
        this.Client.Post();
        this.Client.ClearParameters();        

        //Returns a collection with the added values
        return JArray.Parse(this.ReadClientResult() ?? "");
    }

    public JObject SendInvitationsToParticipants(int surveyID, int retries = 0, int maxRetries = 5){
        this.Client.Method = "invite_participants";        
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);               
        this.Client.Post();
        this.Client.ClearParameters();        

        //Returns a collection with the added values
        var result = JObject.Parse(this.ReadClientResult() ?? "");
        var data = (result["status"] ?? "").ToString();

        //For some reason (SMTP server limits, too much logint attempts, etc.) an OK is received but no invitations has been sent...
        //A retry will be performed till "No candidate tokens" has been received.
        if(data.Contains("No candidate tokens")) return result;
        else{
            if(retries < maxRetries) return SendInvitationsToParticipants(surveyID, retries+1);        
            else throw new SmtpException();
        } 
    }

    public JObject SendRemindersToParticipants(int surveyID, int retries = 0, int maxRetries = 5){
        this.Client.Method = "remind_participants";        
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);               
        this.Client.Post();
        this.Client.ClearParameters();        

        //Returns a collection with the added values
        var result = JObject.Parse(this.ReadClientResult() ?? "");
        var data = (result["status"] ?? "").ToString();

        //For some reason (SMTP server limits, too much logint attempts, etc.) an OK is received but no invitations has been sent...
        //A retry will be performed till "No candidate tokens" has been received.
        if(data.Contains("No candidate tokens")) return result;
        else{
            if(retries < maxRetries) return SendRemindersToParticipants(surveyID, retries+1);        
            else throw new SmtpException();
        } 
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

    public JObject SetSurveyProperties(int surveyID, JObject properties){
        this.Client.Method = "set_survey_properties";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Parameters.Add("aSurveyData", properties);
        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public JObject ActivateSurvey(int surveyID){
        //First must unexpire (mandatory to avoid incongruences) and setup a startdate (optional)
        SetSurveyProperties(surveyID, JObject.Parse(@"{'expires': null}"));
        SetSurveyProperties(surveyID, JObject.Parse(@"{'startdate': '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'}"));

        this.Client.Method = "activate_survey";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
    }

    public JObject ExpireSurvey(int surveyID){
        return SetSurveyProperties(surveyID, JObject.Parse(@"{'expires': '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'}"));
    }

    public JObject DeleteSurvey(int surveyID){        
        this.Client.Method = "delete_survey";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);
        this.Client.Post();
        this.Client.ClearParameters();

        return JObject.Parse(this.ReadClientResult() ?? "");
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