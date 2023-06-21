//Global vars
var _VERSION = "0.12.3";

DisplayInfo();
if(!CheckConfig()) return;

//CLI arguments
var action = string.Empty;
var file = string.Empty;

//Run action or display help
if(args == null || args.Length == 0) Help();
else{
    int i = 0;

    //TODO: check for invalid arguments
    foreach(var arg in args){
        switch(arg){    
            case "--saga-convert":
            case "-sc":                
                SetupConvertSagaCSVtoImportYML(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));                
                break;  

            case "--create-survey":
            case "-cs":
                SetupCreateNewSurveyFromFile(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));
                break;    

            case "--start-surveys":
            case "-ss":
                StartSurveys();
                break;  

            case "--expire-surveys":
            case "-es":
                ExpireSurveys();
                break;  

            case "--send-invitations":
            case "-si":
                SendInvitations();
                break; 

            case "--send-reminders":
            case "-sr":
                SendReminders();
                break; 

            case "--load-teachingstats":
            case "-lt":
                LoadFromTeachingStats();
                break;  

            case "--load-limesurvey":
            case "-ll":
                LoadFromLimeSurvey();
                break;     
        }  

        i++;
    }
}
Console.WriteLine();

//Methods
void Help(){
    Console.WriteLine();
    Info("dotnet run [arguments] <FILE_PATH>: ");
    Console.WriteLine();
    
    Console.ForegroundColor = ConsoleColor.DarkBlue;        
    Console.WriteLine("Allowed arguments: ");
    Highlight("  -cs <FILE_PATH>, --create-survey <FILE_PATH>", "creates a new survey, a YML file must be provided.");
    Highlight("  -sc <FILE_PATH>, --saga-convert <FILE_PATH>", "parses a SAGA's CSV file and creates a YML file which can be used to create new surveys (school, mentoring and subject) on LimeSurvey, a CSV file must be provided.");
    Highlight("  -ss, --start-surveys", "enables all the surveys at limesurvey (just the ones belonging to the defined group at settings) and sends the invitations to the participants.");
    Highlight("  -es, --expire-surveys", "expires all the surveys at limesurvey (just the ones belonging to the defined group at settings) so no pending participants will be able to answer the surveys.");
    Highlight("  -si, --send-invitations", "send the invitations for already active surveys, but just for whom has not received any yet.");
    Highlight("  -sr, --send-reminders", "send survey reminders to all the participants (just the ones belonging to the defined group at settings) that still has not responded the surveys.");
    Highlight("  -lt, --load-teachingstats", "loads all pending reporting data from 'teaching-stats'.");
    Highlight("  -ll, --load-limesurvey", "loads all pending reporting data from expired surveys in 'lime-survey'.");
    //TODO: clear (stop) surveys
    Console.WriteLine();    
}

void SetupConvertSagaCSVtoImportYML(string[] files){ 
    if(files.Length == 0) Error("Unable to find the specified file");
    else{
        foreach (var f in files.OrderBy(x => x))
        {
            //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
            //generates the 2nd level file and updates the 1st level ones).
            if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
            ConvertSagaCSVtoImportYML(f);                    
        }     
    }                                   
}

void ConvertSagaCSVtoImportYML(string filePath){     
    //WARNING: overrides the current group file and updates another existing files.
    //Must be executed for 1st courses first, and then for 2nd courses.   
    var surveysByContent = new Dictionary<string, Dictionary<string, Survey.SurveyData>>();    
    var currentGroupName = Path.GetFileNameWithoutExtension(filePath);  //Must be like ASIX2B
    
    Info($"Converting from CSV to a LimeSurvey compatible YAML file ({Path.GetFileName(filePath)}):");
    try{
        Info("   Loading degree data... ", false);
        var degreeName = string.Empty;
        var degreeCourse = 0;
        for(int i=0; i<currentGroupName.Length; i++){
            if(currentGroupName.Substring(i, 1).All(char.IsNumber)){
                degreeName = currentGroupName.Substring(0, i);
                degreeCourse = int.Parse(currentGroupName.Substring(i, 1));
                break;
            }
        }   
        
        if(Utils.Settings.Data == null || Utils.Settings.Data.Degrees == null) throw new IncorrectSettingsException();
        var degree = Utils.Settings.Data.Degrees.Where(x => x.Name == degreeName).SingleOrDefault();    

        //Setting up survey data
        if(degree == null || degree.Subjects == null) throw new IncorrectSettingsException();
        Success();
        
        Info("   Loading surveys data... ", false);
        //School surveys
        
        var surveyByGroup = new Dictionary<string, Survey.SurveyData>();
        surveyByGroup.Add(currentGroupName, new Survey.SurveyData(){                    
            Topic = "SCHOOL",
            DegreeName = degree.Name,
            DepartmentName = degree.Department,
            GroupName = currentGroupName,      
            Participants = new List<Survey.Participant>()
        }); 
        surveysByContent.Add("SCHOOL", surveyByGroup);    

        //NOTE: this will be filled from teaching-stats database, once integrated within the IMS (the lack of backoffice for teaching-stats does easier to define all the master data within a YML file).
        foreach(var s in degree.Subjects){
            if(s.Trainers == null) throw new IncorrectSettingsException();         

            //The same SurveyData instance will be used along content ID's within the same group
            surveyByGroup = new Dictionary<string, Survey.SurveyData>();        
            foreach(var t in s.Trainers){
                if(t.Groups == null) throw new IncorrectSettingsException();                     

                foreach(var groupName in t.Groups){  
                    //Same data object for every subject ID within the same group which simplifies the Distinct() process.
                    var data = new Survey.SurveyData(){            
                        DegreeName = degree.Name,
                        DepartmentName = degree.Department,                        
                        GroupName = groupName,
                        TrainerName = t.Name,
                        Topic = s.Code,
                        Participants = new List<Survey.Participant>()
                    };

                    if(s.Code != "MENTORING-1-CCFF" && s.Code != "MENTORING-2-CCFF"){
                        data.Topic = "SUBJECT-CCFF";
                        data.SubjectCode = s.Code;
                        data.SubjectName = s.Name;
                    } 

                    if(surveyByGroup.ContainsKey(data.GroupName)) throw new IncorrectSettingsException($"The group '{data.GroupName}' cannot appear more than once for the subject '{s.Name}'");
                    else surveyByGroup.Add(data.GroupName, data);
                }             
            }

            //The same survey data will be used along content IDs (same subject, distinct content)
            if(s.Ids == null) throw new IncorrectSettingsException(); 
            foreach(var id in s.Ids){                                    
                if(!surveysByContent.ContainsKey(id)) surveysByContent.Add(id, surveyByGroup);
                else{
                    var current = surveysByContent[id];
                    foreach(var key in surveyByGroup.Keys){
                        current.Add(key, surveyByGroup[key]);
                    }
                }
            }
        }
        Success();  
        
        //At this point, surveys contains:
        //  - key: the subject's content code (UF)
        //  - value:
        //      - key: the group code (DAM2A, GA2B...)
        //      - value: the survey data, ready to add participants. 
        //               the survey data is shared along different content codes (because is the same subject)
        //               different teachers for the same content code and the same group is not supported (check 11401 for ASIX)
        //               more than one teacher in the same group for the same content codes must be evaluated as a single survey
        //               different teacher can be evaluated in different surveys for the same group if the content codes (UF) are different.

        Info("   Loading participants data... ", false);
        var warnings = new Dictionary<string, List<string>>();

        using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
        using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
        {                
            var records = csv.GetRecords<dynamic>();
            foreach (var r in records)
            {
                string completeName = r.NOM;
                var coma = completeName.IndexOf(",");           

                var p = new Survey.Participant(){
                    Firstname = completeName.Substring(coma+1).Trim(),
                    Lastname = completeName.Substring(0, coma).Trim(),
                    Email = r.EMAIL
                };
                                
                var studentSurveys = new List<Survey.SurveyData>();            
                var subjects = ((string)r.MATRICULADES).Split(",").Where(x => x.Length > 3).ToList();            
                foreach(var id in subjects){
                    //MPs (codes like 101) and UFs (codes like 10101), the UFs codes will be used when a subject is for 1st and 2nd course (like DAM M03).
                    //Repeated surveys will be added (repeated but same instance, so no memory waste and easy to Distinct())
                    if(!surveysByContent.ContainsKey(id)) throw new IncorrectSettingsException($"The content code '{id}' cannot be found within the config file for the group '{currentGroupName}'");
                    surveyByGroup = surveysByContent[id];

                    //The participant will be added to the survey, should be in:
                    //  1. Its own group (this is the normal behaviour).
                    //  2. A 1st course group if the it's a second course student repeating a 1st course subject (and there's only one 1st course group).
                    //  3. More than one 1st course group if the it's a second course student repeating a 1st course subject  (and there's more than one 1st course group). This produces a WARNING.                    
                    if(surveyByGroup.ContainsKey(currentGroupName)) studentSurveys.Add(surveyByGroup[currentGroupName]);
                    else{
                        var first = surveyByGroup.Values.FirstOrDefault(); 
                        if(first != null){
                            //The user has been added to another group, a warning will be displayed, repeated entries will be added.
                            if(!warnings.ContainsKey(r.NOM)) warnings.Add(r.NOM, new List<string>());
                            warnings[r.NOM].Add($"{first.SubjectCode}: {first.SubjectName}");                            
                            studentSurveys.AddRange(surveyByGroup.Values);
                        }
                    }
                }

                //At this point, studentSurveys will have all the enrolled ones but repeated (got by content and not by subject)
                foreach(var s in studentSurveys.Distinct()){
                    if(s.Participants == null) s.Participants = new List<Survey.Participant>(); 
                    s.Participants.Add(p);
                }

                //Adding the participant to the school survey
                var school = surveysByContent["SCHOOL"].FirstOrDefault().Value;
                if(school != null && school.Participants != null) school.Participants.Add(p);

                //Adding the participants to the mentory survey
                var mentoring = surveysByContent[$"MENTORING-{degreeCourse}-CCFF"][currentGroupName];
                if(mentoring != null && mentoring.Participants != null) mentoring.Participants.Add(p);
            }
        }
        
        if(warnings.Count == 0) Success();    
        else{
            string info = $"   WARNING: the following students are enrolled on '{currentGroupName}' but have been assigned to different groups. Please, fix them manually (possibly a repeater students).\n";            
            foreach(var student in warnings.Keys){
                info += $"      - {student}:\n";                
                foreach(var subject in warnings[student].Distinct()){
                    info += $"         - {subject}\n";                
                }
                info += "\n";
            }        
            Warning(info);
        }
        
        Info("   Generating the YAML file for the current group... ", false);
        var allGroupsData = surveysByContent.Values.SelectMany(x => x.Values).Distinct().Where(x => x.Participants != null && x.Participants.Count > 0).ToList();
        var currentGroupData = new Survey(){Data = allGroupsData.Where(x => x.GroupName == currentGroupName).ToList()};
        Utils.SerializeYamlFile(currentGroupData, Path.Combine(Utils.ActionsFolder, $"create-surveys-{currentGroupName}.yml"));
        Success();    

        Info("   Updating existing YAML file for repeater studnets... ", false);
        var otherGroupData = allGroupsData.Where(x => x.GroupName != currentGroupName).GroupBy(x => x.GroupName).ToDictionary(x => x.Key ?? "", x => x.ToList());
        foreach(var otherGroupCode in otherGroupData.Keys){        
            var otherYamlPath = Path.Combine(Utils.ActionsFolder, $"create-surveys-{otherGroupCode}.yml");
            if(!File.Exists(otherYamlPath)) Utils.SerializeYamlFile( new Survey(){Data = otherGroupData[otherGroupCode]}, otherYamlPath);
            else{    
                //Loading the current file and adding new data
                var otherYamlData = Utils.DeserializeYamlFile<Survey>(otherYamlPath);
                if(otherYamlData.Data != null){
                    foreach(var newData in otherGroupData[otherGroupCode]){
                        //Getting existing data (could not exists)                        
                        var oldData = otherYamlData.Data.Where(x => x.SubjectCode == newData.SubjectCode && x.SubjectName == newData.SubjectName).SingleOrDefault();                    
                        if(oldData == null) otherYamlData.Data.AddRange(otherGroupData[otherGroupCode]);
                        else{
                            //Data exists but must be updated
                            if(oldData.Participants == null) oldData.Participants = new List<Survey.Participant>();
                            if(newData.Participants != null) oldData.Participants.AddRange(newData.Participants);
                        }
                    }
                }

                //Storing the updated file
                Utils.SerializeYamlFile(otherYamlData, Path.Combine(otherYamlPath));
            }
        }
        Success();    
    }
    catch (Exception ex){
        Error("ERROR: " + ex.Message + "\n" + ex.StackTrace);
    }

    Console.WriteLine();
}

void SetupCreateNewSurveyFromFile(string[] files){ 
    if(files.Length == 0) Error("Unable to find the specified file");
    else{
        foreach (var f in files.OrderBy(x => x))
        {
            //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
            //generates the 2nd level file and updates the 1st level ones).
            if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
            CreateNewSurveyFromFile(f);                    
        }     
    }                                   
}

void CreateNewSurveyFromFile(string filePath){
    //This option will create new 'limesurvey' surveyss using the provided YML file (template at 'actions/create-survey.yml.template')
    Info($"Creating new surveys ({Path.GetFileName(filePath)}):");
    var importData = Utils.DeserializeYamlFile<Survey>(filePath);
    if(importData.Data == null) return;

    using(var ls = new LimeSurvey()){           
        foreach(var data in importData.Data){ 
            try{
                Info("   Creating... ", false);
                int id = ls.CreateSurvey(data);

                Success($"OK (id={id})");
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");
            }
        }
        
        if(importData.Data.Count == 0) Warning($"There is no new survey info within the '{filePath}' YAML file.");
        else{            
            Success("Process finished, all the surveys have been created.");        
            Console.WriteLine();
        } 
    }
}

void LoadFromLimeSurvey(){        
    Info($"Loading data from LimeSurvey:");
    using(var ls = new LimeSurvey()){
        Info($"   Loading the survey list... ", false);        
        var list = ls.ListSurveys(LimeSurvey.Status.EXPIRED);
        Success();
        Console.WriteLine();

        int i = 1;    
        using(var ts = new TeachingStats()){                                
            foreach(var s in list){
                int surveyID = int.Parse((s["sid"] ?? "").ToString());
                // var type = ls.GetSurveyTopic(surveyID);
                
                // if(type != null){
                var id = int.Parse((s["sid"] ?? "").ToString());
                Info($"Importing all pending answers from LimeSurvey to Teaching-Stats, {i++}/{list.Count} with id={id}:");
                
                try{
                    Info("   Downloading data from LimeSurvey... ", false);
                    var answers = ls.GetSurveyResponses(surveyID);

                    if(answers == null) Info("   No answers received for this survey, skipping... ", false);
                    else{
                        var questions = ls.GetSurveyQuestions(surveyID);
                        Success();

                        Info("   Importing data into Teaching-Stats... ", false);
                        ts.ImportFromLimeSurvey(questions, answers);
                    }
                    
                    Success();                  
                }
                catch(Exception ex){
                    Error($"ERROR: {ex.ToString()}");
                }
                finally{
                    Console.WriteLine();
                }
                // }
            }
        }
    }    
}

void LoadFromTeachingStats(){
    //DEPRECATED: theaching-stats should not be used anymore in order to generate surveys.
    var response = Question("This option will load all the current 'teaching-stats' responses into the report tables, cleaning the original tables (evaluation, answer and participation). This opperation cannot be undone, do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
        using(var ts = new TeachingStats()){
            try{
                Info("Loading data into the reporting tables and clearing the answers... ", false);
                ts.ImportFromTeachingStats();
                Success();     
            }
            catch(Exception ex){                               
                Error("Error: " + ex.ToString());
            }        

        }     
    }
}

void StartSurveys(){
    //This option will start all the 'limesurvey' surveys (only for the surveys within the definded app setting's group, which should be the surveys created with this tool) sending also the email invitations to the participants.
    Info($"Starting surveys from LimeSurvey:");
    using(var ls = new LimeSurvey()){            
        Info($"   Loading the survey list... ", false);        
        var list = ls.ListSurveys(LimeSurvey.Status.STOPPED);
        Success();
        Console.WriteLine();

        int i = 1;
        foreach(var s in list){
            //Just the non-active surveys (all within the current group, which should be the surveys created with this tool).
            var id = int.Parse((s["sid"] ?? "").ToString());
            Info($"Starting survey {i++}/{list.Count} with id={id}: ", true);
            
            try{
                Info($"   Activating... ", false);
                ls.ActivateSurvey(id);
                Success();
                
                Info($"   Sending invitation... ", false);
                ls.SendInvitationsToParticipants(id);
                Success();
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");

                //TODO: wait and retry
            }
            finally{
                Console.WriteLine();
            }
        }

        if(list.Count == 0) Warning($"Unable to load any non-active survey from limesurvey (within the current app group).");
        else{
            Console.WriteLine();
            Success("Process finished, all the non-active surveys within the app group have been activated.");  
        }  
    }
}

void ExpireSurveys(){
    //This option will start all the 'limesurvey' surveys (only for the surveys within the definded app setting's group, which should be the surveys created with this tool) sending also the email invitations to the participants.
    Info($"Expiring surveys from LimeSurvey:");
    using(var ls = new LimeSurvey()){            
        Info($"   Loading the survey list... ", false);        
        var list = ls.ListSurveys(LimeSurvey.Status.ACTIVE);
        Success();
        Console.WriteLine();

        int i = 1;
        Info($"Sending requests:");
        foreach(var s in list){
            //Just the non-active surveys (all within the current group, which should be the surveys created with this tool).                        
            try{
                var id = int.Parse((s["sid"] ?? "").ToString());
                Info($"   Expiring survey {i++}/{list.Count} with id={id}...: ", false);                
                ls.ExpireSurvey(id);
                Success();    
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");

                //TODO: wait and retry
            }            
        }

        if(list.Count == 0) Warning($"Unable to load any active survey from limesurvey (within the current app group).");
        else{
            Console.WriteLine();
            Success("Process finished, all the non-active surveys within the app group have been expired.");  
        }  
    }
}

void SendInvitations(){
    //NOTE: this should be a temporal method, because for some reason, all the surveys has been activated but not all the invitations has been sent (email limit?)
    Info($"Sending invitations from LimeSurvey:");

    using(var ls = new LimeSurvey()){    
        Info($"   Loading the survey list... ", false);        
        var list = ls.ListSurveys(LimeSurvey.Status.ACTIVE);
        Success();
        Console.WriteLine();

        int i = 1;
        Info($"Sending requests:");
        foreach(var s in list){            
            var id = int.Parse((s["sid"] ?? "").ToString());

            try{                
                Info($"   Sending invitation for the survey {i++}/{list.Count} with id={id}... ", false);
                ls.SendInvitationsToParticipants(id);
                Success();
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");

                //TODO: wait and retry
            }            
        }

        if(list.Count == 0) Warning($"Unable to load any active survey from limesurvey (within the current app group).");
        else{
            Console.WriteLine();
            Success("Process finished, all the invitations has been sent.");  
        }   
    }
}

void SendReminders(){
    //This option will start all the 'limesurvey' surveys (only for the surveys within the definded app setting's group, which should be the surveys created with this tool) sending also the email invitations to the participants.
    Info($"Sending reminders from LimeSurvey:");
    
    using(var ls = new LimeSurvey()){           
        Info($"   Loading the survey list... ", false);        
        var list = ls.ListSurveys(LimeSurvey.Status.ACTIVE);
        Success();
        Console.WriteLine();

        int i = 1;
        Info($"Sending requests:");
        foreach(var s in list){
            //Just the non-active surveys (all within the current group, which should be the surveys created with this tool).
            var id = int.Parse((s["sid"] ?? "").ToString());
                        
            try{               
                Info($"   Sending reminders for the survey {i++}/{list.Count} with id={id}... ", false);
                ls.SendRemindersToParticipants(id);
                Success();
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");
                Console.WriteLine();
            }            
        }

        if(list.Count == 0) Warning($"Unable to load any active survey from limesurvey (within the current app group).");
        else{
            Console.WriteLine();
            Success("Process finished, all the reminders has been sent.");  
        }   
    }
}

bool CheckConfig(){    
    try{
        Info("Starting the app:");    
        Info("   Checking the 'teaching-stats' configuration... ", false);            
        using(var ts = new TeachingStats()){    
            Success();
            
            if(!ts.CheckIfUpgraded()){
                var response = Question("The current 'teaching-stats' database has not been upgraded, do you want to perform the necessary changes to use this program? [Y/n]", "y");
                if(response.ToLower() != "y"){
                    Error("The program cannot continue, becasue the 'teaching-stats' database has not been upgraded.");
                    return false;
                }

                Info("   Upgrading the teaching-stats' database... ", false);
                ts.PerformDataDaseUpgrade();
                Success();
            }
        }

        //Testing LimeSurvey config
        Info("   Checking the 'lime-survey' configuration... ", false);            
        using(var ls = new LimeSurvey()){}
        Success();
        Console.WriteLine();

        //All tests clear
        return true;
    }
    catch (FileNotFoundException ex){
        Error(ex.Message);
    }
    catch(Exception ex){
        Error("Error: " + ex.ToString());
    }
    
    Console.WriteLine();

    return false;   
}

void Info(string text, bool newLine = true){
    Console.ResetColor();    
    if(newLine) Console.WriteLine(text);
    else Console.Write(text);
}

void Success(string text = "OK"){
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(text);
    Console.ResetColor();    
}

void Warning(string text = "WARNING", bool newLine = true){
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine(text);
    Console.ResetColor();    
}

void Error(string text = "ERROR"){
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(text);
    Console.ResetColor();    
}

string Question(string text, string @default = ""){
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine(text);
    Console.ResetColor();

    var response = Console.ReadLine();
    return (string.IsNullOrEmpty(response) ? @default : response);
}

void Highlight(string high, string description){   
    Console.ForegroundColor = ConsoleColor.Yellow;        
    Console.Write(high);
    Console.ResetColor();
    Console.WriteLine($": {description}");    
}

void DisplayInfo(){
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Teaching Stats: ");
    Console.ResetColor();
    Console.WriteLine($"LimeSurvey (v{_VERSION})");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Copyright © 2023: ");
    Console.ResetColor();
    Console.WriteLine($"Marcos Alcocer Gil");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Copyright © 2023: ");
    Console.ResetColor();
    Console.WriteLine($"Fernando Porrino Serrano");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Under the AGPL license: ");
    Console.ResetColor();
    Console.WriteLine($"https://github.com/FherStk/teaching-stats-limesurvey/blob/main/LICENSE");
    Console.WriteLine();
}