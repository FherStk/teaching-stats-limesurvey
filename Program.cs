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
    var surveys = new Dictionary<int, Survey.SurveyData>();
    var groupName = Path.GetFileNameWithoutExtension(filePath);  //Must be like ASIX2B
    
    var degreeName = string.Empty;
    for(int i=0; i<groupName.Length; i++){
        if(groupName.Substring(i, 1).All(char.IsNumber)){
            degreeName = groupName.Substring(0, i);
            break;
        }
    }   
    
    if(Utils.Settings.Data == null || Utils.Settings.Data.Degrees == null) throw new IncorrectSettingsException();
    var degree = Utils.Settings.Data.Degrees.Where(x => x.Name == degreeName).SingleOrDefault();

    //Setting up survey data
    if(degree == null || degree.Subjects == null) throw new IncorrectSettingsException();
    
    //Fill the survey data using the settings info.
    //NOTE: this will be filled from teaching-stats database, once integrated within the IMS (the lack of backoffice for teaching-stats does easier to define all the master data within a YML file).             
    foreach(var s in degree.Subjects){
        if(s.Trainers == null) throw new IncorrectSettingsException();
        
        foreach(var t in s.Trainers){
            if(t.Groups == null) throw new IncorrectSettingsException();

            var g = t.Groups.Where(x => x.Code == groupName).SingleOrDefault();
            if(g == null) continue; //the current trainer does not teach the current group

            //A survey must be generated for this group
                var sd = new Survey.SurveyData(){
                Topic = "SUBJECT-CCFF",
                DegreeName = degree.Name,
                DepartmentName = degree.Department ,
                GroupName = groupName,
                TrainerName = t.Name,
                SubjectCode = s.Code,
                SubjectName = s.Name,
                Participants = new List<Survey.Participant>()
            };

            int id = int.Parse((sd.SubjectCode ?? "M00").Substring(1));
            surveys.Add(id, sd);                
        }
    }    

    //TODO: the CSV column names must be edited (with no spaces, numers, etc.)
    using (var reader = new StreamReader(filePath, System.Text.Encoding.Latin1))
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
            foreach(var s in subjects){
                var id = 0;
                if(int.TryParse(s, out id)){
                    //Some subjects start with a letter, only numbers are needed
                    var parts = surveys[id-100].Participants;
                    if(parts == null) parts = new List<Survey.Participant>();
                    parts.Add(p);
                }
            }
        }
    }

    var data = new Survey(){Data = surveys.Values.ToList()};
    Utils.SerializeYamlFile(data, Path.Combine(Utils.ActionsFolder, $"create-surveys-{groupName}.yml"));
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
        using(var ts = new TeachingStats()){    
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
        using(var ls = new LimeSurvey()){}

        //All tests clear
        return true;
    }
    catch (FileNotFoundException ex){
        Error(ex.Message);
    }
    catch(Exception ex){
        Error("Error: " + ex.ToString());
    }

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

void Warning(string text, bool newLine = true){
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