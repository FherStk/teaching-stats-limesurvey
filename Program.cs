//Global vars
var _VERSION = "0.1.0";

DisplayInfo();
if(!CheckConfig()) return;

//CLI arguments
var action = string.Empty;
var file = string.Empty;

//Load the interactive menu or the unatended opps
if(args == null || args.Length == 0) Help();
else{
    int i = 0;

    //TODO: check for invalid arguments
    foreach(var arg in args){
        switch(arg){        
            case "--create-survey":
            case "-cs":
                //TODO: check there is a filepath
                CreateNewSurveyFromFile(args[i+1]);
                break;    

            case "--start-survey":
            case "-ss":
                StartSurveys();
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
    Info("  -ss, --start-survey: enables all the created surveys at limesurvey (just the created with this tool) and sends the invitations to the participants.");
    Info("  -lt, --load-teachingstats: loads all pending reporting data from 'teaching-stats'.");
    Info("  -ll, --load-limesurvey: loads all pending reporting data from 'lime-survey'.");
    Console.WriteLine();    
}

void CreateNewSurveyFromFile(string filePath){
    //This option will create new 'limesurvey' surveyss using the provided YML file (template at 'actions/create-survey.yml.template')
    
    var importData = Utils.DeserializeYamlFile<Survey>(filePath);
    if(importData.Data == null) return;

    using(var ls = new LimeSurvey()){   
        foreach(var data in importData.Data){ 
            try{
                Info("Creating a new survey... ", false);
                int id = ls.CreateSurvey(data);

                Success($"OK: id={id}");
            }
            catch(Exception ex){
                Error($"ERROR: {ex.ToString()}");
            }
        }

        if(importData.Data.Count == 0) Warning($"There is no new survey info within the '{filePath}' YAML file.");
        else Success("Process finished, all the surveys have been created.");        
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
        else Success("Process finished, all the non-active surveys within the app group have been activated.");        
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