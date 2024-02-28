using System.IO.Compression;
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
        TEACHERS,
        FCT,
        SERVICES        
    }

    public enum Status{
        ACTIVE,
        EXPIRED,
        STOPPED
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
    public JArray ListSurveys(int group = 0, Status? status = null){
        this.Client.Method = "list_surveys";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);        
        this.Client.Post();
        this.Client.ClearParameters();
                
        var list = JArray.Parse(this.ReadClientResult() ?? "");        
        var filtered = new JArray();
    
        var groups = new Dictionary<int, string?>();
        if(group > 0) groups.Add(0, "ALL");
        else groups = (Utils.Settings.LimeSurvey == null || Utils.Settings.LimeSurvey.Groups == null ? new Dictionary<int, string?>() : Utils.Settings.LimeSurvey.Groups.ToDictionary(x => x.Group, x => x.Degree));
        
        foreach(var survey in list){
            var id = int.Parse((survey["sid"] ?? "").ToString());
            var props = GetSurveyProperties(id);
            var gsid = int.Parse((props["gsid"] ?? "").ToString());

            if(groups.ContainsKey(gsid)){
                var active = char.Parse((props["active"] ?? "").ToString());
                var expired = (props["expires"] ?? "").ToString();

                if(status == null) filtered.Add(survey);
                else{
                    if(active == 'N' && status == Status.STOPPED) filtered.Add(survey);                    
                    else if(string.IsNullOrEmpty(expired) && active == 'Y' && status == Status.ACTIVE) filtered.Add(survey);
                    else if(!string.IsNullOrEmpty(expired) && active == 'Y' && status == Status.EXPIRED) filtered.Add(survey);                    
                }
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
        //Setting up the main template
        var template = Path.Combine(Utils.TemplatesFolder, "main-students-ccff.txt");
        var content = File.ReadAllText(template);

        var captions = (Utils.Settings.Data == null ? null : Utils.Settings.Data.Captions);
        content = content.Replace("{'DESCRIPTION'}", (captions == null ? data.GroupName : captions.Survey));
        content = content.Replace("{'TITLE'}", data.Topics == null ? data.GroupName : $"{data.GroupName} | {string.Join(", ", data.Topics.Where(x => !string.IsNullOrEmpty(x.SubjectAcronym)).Select(x => x.SubjectAcronym).OrderBy(x => x).ToList())}");

        //Setting up each topic template
        var questionID = 4;   //each question must have a unique numerical id, for subject it should star with 4 (400, 4001, 4002...)
        if(data.Topics != null){
            //This is the easiest way to order and process the surveys (less code, less methods, etc.)
            var orderedItems = data.Topics.Where(x => x.Topic == "SUBJECT-CCFF").OrderBy(x => x.SubjectAcronym).ToList();
            orderedItems.AddRange(data.Topics.Where(x => x.Topic != "SUBJECT-CCFF").OrderBy(x => x.Topic).ToList());

            foreach(var entry in orderedItems){
                var block = string.Empty;                     
                var subjectCode = string.Empty;
                var subjectName = string.Empty;
                var blockName = string.Empty;
                var description = string.Empty;                
                var topic = (LimeSurvey.Topic)Enum.Parse(typeof(LimeSurvey.Topic), (entry.Topic ?? "").Replace("-", "_"), true);        

                switch(topic){
                    case Topic.SCHOOL:
                        template = "block-school";
                        subjectCode = "Centre";
                        subjectName = "Instal·lacions i estada";
                        blockName = $"{data.GroupName} {(captions == null ? "SCHOOL" : captions.School)}";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
                                            <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                            <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                            <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                        </ol>";                
                        break;

                    case Topic.MENTORING_1_CCFF:
                        template = "block-mentoring-1-ccff";
                        subjectCode = "Tutoria";
                        subjectName = "1er Curs";
                        blockName = $"{data.GroupName} {(captions == null ? "MENTORING 1ST" : captions.Mentoring1)} ({entry.TrainerName})";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
                                            <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                            <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                            <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                        </ol>";
                        break;

                    case Topic.MENTORING_2_CCFF:
                        template = "block-mentoring-2-ccff";
                        subjectCode = "Tutoria";
                        subjectName = "2n Curs";
                        blockName = $"{data.GroupName} {(captions == null ? "MENTORING 2ND" : captions.Mentoring2)} ({entry.TrainerName})";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
                                            <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                            <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                            <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                        </ol>";
                        break;

                    case Topic.SUBJECT_CCFF:
                        template = "block-subject-ccff";
                        blockName = $"{data.GroupName} {entry.SubjectAcronym}: {entry.SubjectName} ({entry.TrainerName})";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
                                            <li>Si no estàs matriculat d'aquest Mòdul Professional o en trobes a faltar enquestes sobre altres Mòduls que tens matriculats, posa't en contacte amb el teu tutor.</li>
                                            <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                            <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                            <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                        </ol>";
                        break;

                    case Topic.FCT:
                        //Todo: change how the FCT is setup at settings.yaml (should be as mentoring)
                        template = "block-fct";
                        blockName = $"{data.GroupName} {(captions == null ? "FCT" : captions.FCT)} ({entry.TrainerName})";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
                                            <li>Si no has matriculat les FCT o en trobes a faltar enquestes sobre altres Mòduls que tens matriculats, posa't en contacte amb el teu tutor.</li>
                                            <li>Aquesta enquesta és completament anònima, si us plau, sigues sincer.</li>
                                            <li>Sigues constructiu, explica'ns quines coses fem bé i com podem millorar.</li>
                                            <li>Sigues educat i respectuós, així ens ajudes a fer millor el nostre institut.</li>
                                        </ol>";
                        break;

                     case Topic.SERVICES:
                        template = "block-services";
                        subjectCode = "Centre";
                        subjectName = "Serveis";
                        blockName = $"{data.GroupName} {(captions == null ? "SERVICES" : captions.Services)}";
                        description = @"<p><strong>Si us plau, abans de contestar l'enquesta, tingues en compte el següent:</strong></p>
                                        <ol style='text-align: left;'>
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
                
                template = Path.Combine(Utils.TemplatesFolder, $"{template}.txt");
                block = File.ReadAllText(template);

                block = block.Replace("{'TITLE'}", $"{blockName}");
                block = block.Replace("{'DESCRIPTION'}", $"{description}");
                block = block.Replace("{'DEPARTMENT'}", "{'" + data.DepartmentName + "'}");
                block = block.Replace("{'DEGREE'}", "{'" + data.DegreeName + "'}");
                block = block.Replace("{'GROUP'}", "{'" + data.GroupName + "'}");
                block = block.Replace("{'TRAINER'}", "{'" + entry.TrainerName + "'}");

                if(topic == Topic.SUBJECT_CCFF){
                    block = block.Replace("{'SUBJECT_CODE'}", "{'" + entry.SubjectAcronym + "'}");
                    block = block.Replace("{'SUBJECT_NAME'}", "{'" + entry.SubjectName + "'}");
                    block = block.Replace("{'X'}", (questionID++).ToString());
                }

                content += block;                
            }              
        }

        //File.WriteAllText(Path.Combine(Utils.TemplatesFolder, "test.txt"), content);
                    
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
        SetSurveyProperties(newID, JObject.Parse(@"{'gsid': " + (Utils.Settings.LimeSurvey == null ? 1 : data.DegreeName) + "}"));

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

    public JObject SendInvitationsToParticipants(int surveyID, int retries = 0, int maxRetries = 1){
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

    public JObject SendRemindersToParticipants(int surveyID, int retries = 0, int maxRetries = 1){
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
        if(data.Contains("0 left to send")) return result;
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
    
    public JObject? GetSurveyResponses(int surveyID){
        this.Client.Method = "export_responses";
        this.Client.Parameters.Add("sSessionKey", this.SessionKey);
        this.Client.Parameters.Add("iSurveyID", surveyID);        
        this.Client.Parameters.Add("sDocumentType", "json");
        //TODO: The idea was to export also the question name so all the information neede came within a unique JSON but no question statement is beeing exportes... weird...        
        // this.Client.Parameters.Add("sLanguageCode", "");
        // this.Client.Parameters.Add("sHeadingType", "full");
        // this.Client.Parameters.Add("sResponseType", "long");
        //this.Client.Parameters.Add("sCompletionStatus", "complete"); //fails with "The input is not a valid Base-64 string as it contains a non-base 64 character"
        //"{\n  \"status\": \"No Data, could not get max id.\"\n}"
        this.Client.Post();
        this.Client.ClearParameters();

        var result = (this.ReadClientResult() ?? "");
        if(string.IsNullOrEmpty(result) || result.Contains("No Data, could not get max id.")) return null;
        else{
            var base64EncodedBytes = System.Convert.FromBase64String(result);
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
        }
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