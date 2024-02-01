//Global vars
var _VERSION = "0.13.0";

DisplayInfo();
//if(!CheckConfig()) return;
//Utils.SerializeImportTemplateAsYamlFile();

//Warnings
Dictionary<Survey.Participant, List<Settings.SubjectData>> enrollmentWarnings;

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
    var currentGroupName = Path.GetFileNameWithoutExtension(filePath);  //Must be like ASIX2B
    enrollmentWarnings = new Dictionary<Survey.Participant, List<Settings.SubjectData>>();
    
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

        //Setting up student's enrollment data
        Info("   Loading surveys by enrollment data... ", false);
        var surveyByEnrollment = new Dictionary<string, Survey.SurveyData>();

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
                var subjects = ((string)r.MATRICULADES).Split(",").Where(x => x.Length > 3).ToList().OrderBy(x => x); //UF codes should be used, when the same MP is coursed along 1st and 2nd course (like DAM M03)
                var surveyID = $"{currentGroupName}-{string.Join('-', subjects)}";
                
                List<Survey.Participant> participants;
                if(surveyByEnrollment.ContainsKey(surveyID)) participants = surveyByEnrollment[surveyID].Participants ?? new List<Survey.Participant>();
                else {
                    //The survey did not exists
                    var topics = new List<Survey.SurveyTopic>();
                    foreach(var subjectCode in subjects){
                        var subjectData = GetSubjectData(degree, subjectCode);                       
                        
                        if(topics.Where(x => x.SubjectCode == subjectData.Code).Count()  == 0){
                            //Enrollment data is loaded by UF, but topics must be stored by MP with no duplications.
                            topics.Add(new Survey.SurveyTopic(){                        
                                SubjectCode = subjectData.Code,
                                SubjectName = subjectData.Name,
                                Topic = "SUBJECT-CCFF",
                                TrainerName = GetTrainerName(subjectData, p, currentGroupName)
                            });
                        }                        
                    }

                    //Adding the school survey
                    topics.Add(new Survey.SurveyTopic(){                                                    
                        Topic = "SCHOOL"
                    });

                    //Adding the mentoring survey
                    var mentoringCode = $"MENTORING-{degreeCourse}-CCFF";
                    var mentoringData = GetSubjectData(degree, mentoringCode);                                               
                    topics.Add(new Survey.SurveyTopic(){                                                    
                        Topic = mentoringCode,
                        TrainerName = GetTrainerName(mentoringData, p, currentGroupName)
                    });                      

                    participants = new List<Survey.Participant>();
                    surveyByEnrollment.Add(surveyID, new Survey.SurveyData(){       
                        Id = surveyID,             
                        DegreeName = degree.Name,
                        DepartmentName = degree.Department,
                        GroupName = currentGroupName,      
                        Topics = topics,
                        Participants = participants
                    });                  
                }

                participants.Add(p);
            }
        }
        Success();

        Info("   Generating the YAML file for the current group... ", false);        
        Utils.SerializeYamlFile(
            new Survey(){
                Data = surveyByEnrollment.Values.ToList()
            }, Path.Combine(Utils.ActionsFolder, $"create-surveys-{currentGroupName}.yml")
        );

        if(enrollmentWarnings.Count == 0) Success();    
        else{
            string info = $"WARNING: the following students are enrolled on '{currentGroupName}' but have been assigned to different groups. Please, fix them manually if needed (possibly are repeater students).\n";            
            foreach(var student in enrollmentWarnings.Keys){
                info += $"      - {student.Firstname} {student.Lastname}:\n";                
                foreach(var subject in enrollmentWarnings[student].Distinct()){
                    info += $"         - {subject.Code}: {subject.Name}\n";                
                }
                info += "\n";
            }        
            Warning(info);
        }        
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
            Console.WriteLine();        
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

Settings.SubjectData GetSubjectData(Settings.DegreeData degreeData, string subjectCode){
    if(degreeData.Subjects == null) throw new Exception($"Unable to load any subject data.");

    var subjectData = degreeData.Subjects.SingleOrDefault(x => x.Ids != null && x.Ids.Contains(subjectCode));
    if(subjectData == null) throw new Exception($"Unable to find any subject with the code '{subjectCode}'");

    return subjectData;
}

string GetTrainerName(Settings.SubjectData subjectData, Survey.Participant participant, string currentGroupName){
    var trainerName = string.Empty;
    if(subjectData.Trainers != null){
        var trainerData = subjectData.Trainers.Where(x => x.Groups != null && x.Groups.Contains(currentGroupName)).SingleOrDefault();
        trainerName = (trainerData == null ? string.Empty : trainerData.Name ?? string.Empty);

        if(string.IsNullOrEmpty(trainerName)){
            //If a 2nd course student is repeating a 1st course subject, a 1st group teacher will be got, but a warning must be displayed if there's more than one option.            
            trainerData = subjectData.Trainers.FirstOrDefault();
            trainerName = (trainerData == null ? string.Empty : trainerData.Name ?? string.Empty);

            if(subjectData.Trainers.Count() > 1){
                List<Settings.SubjectData> subjects;
                if(enrollmentWarnings.ContainsKey(participant)) subjects = enrollmentWarnings[participant];
                else{
                    subjects = new List<Settings.SubjectData>();
                    enrollmentWarnings.Add(participant, subjects);
                }

                subjects.Add(subjectData);
            }            
        }   
    } 

    if(string.IsNullOrEmpty(trainerName)) throw new Exception("Unable to get any trainer for the subject '{subjectData.Code}' within the group '{currentGroupName}'");
    return trainerName;
}