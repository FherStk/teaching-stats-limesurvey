//Global vars
var _VERSION = "0.2.2";

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
                //TODO: check there is a filepath
                ConvertSagaCSVtoImportYML(args[i+1]);
                break;  

            case "--create-survey":
            case "-cs":
                //TODO: check there is a filepath
                CreateNewSurveyFromFile(args[i+1]);
                break;    

            case "--start-survey":
            case "-ss":
                StartSurveys();
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
    Info("Allowed arguments: ");
    Info("  -cs <FILE_PATH>, --create-survey <FILE_PATH>: creates a new survey, a YML file must be provided.");    
    Info("  -sc <FILE_PATH>, --saga-convert <FILE_PATH>: parses a SAGA's CSV file and creates a new import action YML file, a CSV file must be provided.");    
    Info("  -ss, --start-survey: enables all the created surveys at limesurvey (just the created with this tool) and sends the invitations to the participants.");
    Info("  -sr, --send-reminders: send survey reminders to all the participants (just the created with this tool) that still has not responded the surveys.");
    Info("  -lt, --load-teachingstats: loads all pending reporting data from 'teaching-stats'.");
    Info("  -ll, --load-limesurvey: loads all pending reporting data from 'lime-survey'.");
    Console.WriteLine();    
}

void ConvertSagaCSVtoImportYML(string filePath){        
    var surveys = new Dictionary<string, List<Survey.SurveyData>>();    
    var groupName = Path.GetFileNameWithoutExtension(filePath);  //Must be like ASIX2B
    
    Info("Converting from CSV to a lime-survey compatible YAML file:");
    
    Info("   Loading degree data...", false);
    var degreeName = string.Empty;
    var degreeCourse = 0;
    for(int i=0; i<groupName.Length; i++){
        if(groupName.Substring(i, 1).All(char.IsNumber)){
            degreeName = groupName.Substring(0, i);
            degreeCourse = int.Parse(groupName.Substring(i, 1));
            break;
        }
    }   
    
    if(Utils.Settings.Data == null || Utils.Settings.Data.Degrees == null) throw new IncorrectSettingsException();
    var degree = Utils.Settings.Data.Degrees.Where(x => x.Name == degreeName).SingleOrDefault();    

    //Setting up survey data
    if(degree == null || degree.Subjects == null) throw new IncorrectSettingsException();
    Success();
    
    Info("   Loading surveys data...", false);
    //School surveys
    surveys.Add("SCHOOL", new List<Survey.SurveyData>(){
        new Survey.SurveyData(){
            Topic = "SCHOOL",
            DegreeName = degree.Name,
            DepartmentName = degree.Department,
            GroupName = groupName,           
            Participants = new List<Survey.Participant>()
        }
    });

    //Loading the subjects data for the given degree (ignoring group or level)
    //NOTE: this will be filled from teaching-stats database, once integrated within the IMS (the lack of backoffice for teaching-stats does easier to define all the master data within a YML file).                     
    foreach(var s in degree.Subjects){  
        string id = (s.Id ?? "");      
        if(!surveys.ContainsKey(id)) surveys.Add(id, new List<Survey.SurveyData>());               

        var grpSurveys = surveys[id];        
        if(s.Name == "FCT") {
            //TODO: there is no FCT survey at the moment, but this is needed for the app to work with no extra complexity
            grpSurveys.Add(new Survey.SurveyData(){
                Topic = "FCT",
                DegreeName = degree.Name,
                DepartmentName = degree.Department,
                GroupName = groupName,
                SubjectCode = s.Code,
                SubjectName = s.Name,
                Participants = new List<Survey.Participant>()
            }); 
        }
        else{
            if(s.Trainers == null) throw new IncorrectSettingsException();        
            
            foreach(var t in s.Trainers){
                if(t.Groups == null) throw new IncorrectSettingsException();
                                
                foreach(var g in t.Groups){                      
                    if(s.Name == "MENTORING-1-CCFF" || s.Name == "MENTORING-2-CCFF") {
                        //Mentoring surveys
                        grpSurveys.Add(new Survey.SurveyData(){
                            Topic = s.Name,
                            DegreeName = degree.Name,
                            DepartmentName = degree.Department,
                            GroupName = groupName, 
                            TrainerName = t.Name,                          
                            Participants = new List<Survey.Participant>()
                        }); 
                    }
                    else{ 
                        //Regular subject surveys                                        
                        grpSurveys.Add(new Survey.SurveyData(){
                            Topic = "SUBJECT-CCFF",
                            DegreeName = degree.Name,
                            DepartmentName = degree.Department ,
                            GroupName = g.Code,
                            TrainerName = t.Name,
                            SubjectCode = s.Code,
                            SubjectName = s.Name,
                            Participants = new List<Survey.Participant>()
                        });   
                    }
                }             
            }
        }
    }
    Success();  
    
    //TODO: the CSV column names must be edited (with no spaces, numers, etc.)
    Info("   Loading participants data...", false);
    var warnings = new List<string>();

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

            var subjects = ((string)r.MATRICULADES).Split(",").Select(x => x.Substring(0,3).Trim()).Distinct().ToList(); //only MPs wanted. 10102 = 101 = MP01
            foreach(var id in subjects){                
                var mpSurveys = surveys[id];
                if(mpSurveys.Count > 1){
                    //WARNING:  If the current subject can be done in more than one group, the student should be assigned to:
                    //          1. It's own group (could be teached by more than one trainer).
                    //          2. Another group (if it's repeating the subject)
                    //          3. More than one group (WARNING) if it's repeating but it's impossible to know in which group.
                    if(mpSurveys.Count(x => x.GroupName == groupName) == 0) warnings.Add($"   WARNING: the student '{r.NOM}' has been assigned to more than one group for 'MP{id}'. Please, fix it manually (possibly a repeater student).");
                    else mpSurveys = mpSurveys.Where(x => x.GroupName == groupName).ToList();                    
                }

                foreach(var survey in mpSurveys){
                    //The participant will be added to the survey, should be in:
                    //  1. Its own group (this is the normal behaviour).
                    //  2. A 1st course group if the it's a second course student repeating a 1st course subject (and there's only one 1st course group).
                    //  3. More than one 1st course group if the it's a second course student repeating a 1st course subject  (and there's more than one 1st course group). This produces a WARNING.
                    var parts = survey.Participants;
                    if(parts == null) parts = new List<Survey.Participant>();
                    parts.Add(p);                    
                }
                
                //Adding the participant to the school survey
                var school = surveys["SCHOOL"].FirstOrDefault();
                if(school != null && school.Participants != null) school.Participants.Add(p);

                var mentoring = surveys[$"MENTORING-{degreeCourse}-CCFF"].FirstOrDefault();
                if(mentoring != null && mentoring.Participants != null) mentoring.Participants.Add(p);
            }
        }
    }
    
    if(warnings.Count == 0) Success();    
    else Warning("WARNING: \n" + string.Join('\n', warnings));
    
    Info("   Generating the YAML file for the current group...", false);
    var allGroupsData = surveys.Values.SelectMany(x => x).Where(x => x.Participants != null && x.Participants.Count > 0);    
    var currentGroupData = new Survey(){Data = allGroupsData.Where(x => x.GroupName == groupName).ToList()};
    Utils.SerializeYamlFile(currentGroupData, Path.Combine(Utils.ActionsFolder, $"create-surveys-{groupName}.yml"));
    Success();    

    Info("   Updating existing YAML file for repeater studnets...", false);
    var otherGroupData = allGroupsData.Where(x => x.GroupName != groupName).GroupBy(x => x.GroupName).ToDictionary(x => x.Key ?? "", x => x.ToList());
    foreach(var otherGroupCode in otherGroupData.Keys){        
        var otherYamlPath = Path.Combine(Utils.ActionsFolder, $"create-surveys-{otherGroupCode}.yml");

        //Loading the current file and adding new data
        var otherYamlData = Utils.DeserializeYamlFile<Survey>(otherYamlPath);
        if(otherYamlData.Data != null) otherYamlData.Data.AddRange(otherGroupData[otherGroupCode]);

        //Storing the updated file
        Utils.SerializeYamlFile(otherYamlData, Path.Combine(otherYamlPath));
    }

    Success();    
}

void CreateNewSurveyFromFile(string filePath){
    //This option will create new 'limesurvey' surveyss using the provided YML file (template at 'actions/create-survey.yml.template')
    
    var importData = Utils.DeserializeYamlFile<Survey>(filePath);
    if(importData.Data == null) return;

    using(var ls = new LimeSurvey()){   
        Info($"Creating new surveys:");
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
        } 
    }
}

void LoadFromLimeSurvey(){
    var response = Question("This option will load all the 'limesurvey' responses (only for the surveys generated with this tool) into the report tables, closing and cleaning the original surveys. Do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
         using(var ls = new LimeSurvey()){
            using(var ts = new TeachingStats()){
                foreach(var s in ls.ListSurveys()){
                    if((s["active"] ?? "").ToString() == "N") continue;
                    int surveyID = int.Parse((s["sid"] ?? "").ToString());
                    var type = ls.GetSurveyTopic(surveyID);
                    
                    if(type != null){
                        //TODO: this will be not necessary once ListSurveys returns the surveys by group
                        var answers = ls.GetSurveyResponses(surveyID);
                        var questions = ls.GetSurveyQuestions(surveyID);

                        ts.ImportFromLimeSurvey(questions, answers);
                        
                        //TODO: stop the LS surveys
                    }
                }
            }
        }
    }
}

void LoadFromTeachingStats(){
    var response = Question("This option will load all the current 'teaching-stats' responses into the report tables, cleaning the original tables (evaluation, answer and participation). Do you want no continue? [Y/n]", "y");
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
    
    using(var ls = new LimeSurvey()){            
        var list = ls.ListSurveys('N');        
        
        foreach(var s in list){
            //Just the non-active surveys (all within the current group, which should be the surveys created with this tool).
            var id = int.Parse((s["sid"] ?? "").ToString());
            Info($"Starting survey (id={id}): ", true);
            
            try{
                Info($"   Activating... ", false);
                ls.ActivateSurvey(id);
                Success("OK");
                
                Info($"   Sending invitation... ", false);
                ls.SendInvitationsToParticipants(id);
                Success("OK");
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");
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

void SendReminders(){
    //This option will start all the 'limesurvey' surveys (only for the surveys within the definded app setting's group, which should be the surveys created with this tool) sending also the email invitations to the participants.
    
    using(var ls = new LimeSurvey()){   
        Info($"Sending reminders for all open surveys:"); 
        var list = ls.ListSurveys('Y');        
        
        foreach(var s in list){
            //Just the non-active surveys (all within the current group, which should be the surveys created with this tool).
            var id = int.Parse((s["sid"] ?? "").ToString());
                        
            try{               
                Info($"   Sending reminders for the survey with id={id}... ", false);
                ls.SendRemindersToParticipants(id);
                Success("OK");
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

                Info("Upgrading the teaching-stats' database... ", false);
                ts.PerformDataDaseUpgrade();
                Success();
                Console.WriteLine();
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
    Console.WriteLine($"Coordinació de Qualitat Serrano");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Under the AGPL license: ");
    Console.ResetColor();
    Console.WriteLine($"https://github.com/FherStk/teaching-stats-limesurvey/blob/main/LICENSE");
    Console.WriteLine();
}