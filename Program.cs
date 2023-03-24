using Npgsql;
using Newtonsoft.Json.Linq;

//Global vars
var _VERSION = "0.0.1";

Test();
return;

//Main
// DisplayInfo();
// if(!CheckConfig()) return;
// else Menu();

//Methods
void Menu(){
    while(true){        
        Info("Please, select an option:");
        Info("   1: Load reporting data from 'teaching-stats'");
        Info("   2: Load reporting data from 'limesurvey'");
        Info("   3: Create a new survey into 'limesurvey'");
        Info("   0: Exit");
        Console.WriteLine();

        int option = -1;
        if(!int.TryParse(Console.ReadLine(), out option)) Error("Please, select a valid option.");
        else {
            switch (option){
                case 0:
                    return;

                case 1:
                    LoadFromTeachingStats();
                    break;

                 case 2:
                    LoadFromLimeSurvey();
                    break;

                case 3:
                    ReadNewSurveyData();
                    break;

                //new cases:
                //  load participants into its surveys using CSV files and generate the passwords (limesurvey)
                //  open the surveys and send the invitations (limesurvey)
                //  create new surveys (limesurvey)

                default:
                    Error("Please, select a valid option.");
                    break;
            }
        }

        Console.WriteLine();
    }   
}

void ReadNewSurveyData(){       
    var option = -1;
    var values = Enum.GetValues(typeof(LimeSurvey.Topic)).Cast<LimeSurvey.Topic>().ToDictionary(t => (int)t, t => t.ToString() );

    //int option = 1;
    while(option == -1){        
        Info("Please, select the template ID to create a new survey:");
        
        foreach(var id in values.Keys)
            Info($"   {id}: {values[id]}");            
        
        Info("   0: Exit");                        
        Console.WriteLine();

        
        if(!int.TryParse(Console.ReadLine(), out option)){
            Error("Please, select a valid option.");
            Console.WriteLine();
        }        
    }
    
    var topic = (LimeSurvey.Topic)option;
    var degreeName = Question("Please, write the DEGREE NAME:");
    var departmentName = Question("Please, write the DEPARTMENT NAME:");    
    var groupName = Question("Please, write the GROUP NAME:");
    var trainerName = Question("Please, write the TRAINER NAME:");    
    
    var subjectCode = string.Empty;
    var subjectName = string.Empty;
    if(topic == LimeSurvey.Topic.SUBJECT_CCFF){
        subjectCode = Question("Please, write the SUBJECT CODE:");
        subjectName = Question("Please, write the SUBJECT NAME:");
    }
   
    CreateNewSurveyFromTemplate(topic, degreeName, departmentName, groupName, trainerName, subjectCode, subjectName);
}

void CreateNewSurveyFromTemplate(LimeSurvey.Topic topic, string degreeName, string departmentName, string groupName, string trainerName, string subjectCode = "", string subjectName = ""){
    using(var ls = new LimeSurvey()){   
        try{            
            Info("Creating a new survey... ", false);    
            var surveyName = $"{degreeName} {subjectCode} - {subjectName}";
            if(!string.IsNullOrEmpty(trainerName)) surveyName += $" ({trainerName})";

            //Copying the survey with the correct name
            int newID = ls.CreateSurveyFromCSV(topic, degreeName, departmentName, groupName, trainerName, subjectCode, subjectName);    
            Success();

            //Loading the question IDs in order to set the correct values
            Info("Loading the survey data... ", false);    
            var qIDs = ls.GetQuestionsIDsByType(newID);            
            Success();

            //Changing the copied question data with the correct values
            Info("Setting up the survey degree... ", false);
            SetQuestionValue(ls, qIDs, LimeSurvey.Question.DEGREE, degreeName);                    
            Success();

            Info("Setting up the survey department... ", false);
            SetQuestionValue(ls, qIDs, LimeSurvey.Question.DEPARTMENT, departmentName);                    
            Success();

            Info("Setting up the survey group... ", false);
            SetQuestionValue(ls, qIDs, LimeSurvey.Question.GROUP, groupName);                    
            Success();

            Info("Setting up the survey trainer... ", false);
            SetQuestionValue(ls, qIDs, LimeSurvey.Question.TRAINER, trainerName);                    
            Success();                    

            if(topic == LimeSurvey.Topic.SUBJECT_CCFF){
                Info("Setting up the survey subject... ", false);
                SetQuestionValue(ls, qIDs, LimeSurvey.Question.SUBJECTCODE, subjectCode);                    
                SetQuestionValue(ls, qIDs, LimeSurvey.Question.SUBJECTNAME, subjectName);
                Success();
            }
        }
        catch(Exception ex){
            Error("Error: " + ex.ToString());
        }
    }
}

void SetQuestionValue(LimeSurvey ls, Dictionary<LimeSurvey.Question, List<int>> questionIDs, LimeSurvey.Question question, string value){
    ls.SetQuestionProperties(questionIDs[question].FirstOrDefault(), JObject.Parse(
        @"{
            ""question"" : """ + question.ToString().ToLower() + ": {'" + value + @"'}""  
        }"
    ));           

    //TODO: this does not work!!!
    ls.SetQuestionProperties(questionIDs[question].FirstOrDefault(), JObject.Parse(
        @"{
            ""attributes"" : {
                ""equation"": ""{'" + value + @"'}"",
                ""hidden"": ""1""
            } 
        }"
    ));  
}

void LoadFromLimeSurvey(){
    var response = Question("This option will load all the current 'limesurvey' responses into the report tables, closing and cleaning the original surveys. Do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
        using(var ls = new LimeSurvey()){
            using(var ts = new TeachingStats()){
                //TODO: chech for all the survey IDs...

                var surveyID = 272798;
                var answers = ls.GetSurveyResponses(surveyID);
                var questions = ls.GetSurveyQuestions(surveyID);
            
                ts.ImportFromLimeSurvey(questions, answers);

                //TODO: stop the LS surveys
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

bool CheckConfig(){            
    try{    
        using(var ts = new TeachingStats()){    
            if(!ts.CheckIfUpgraded()){
                var response = Question("The current 'teaching-stats' database has not been upgraded, do you want to perform the necessary changes to use this program? [Y/n]", "y");
                if(response.ToLower() != "y"){
                    Error("The program cannot continue, becasue the 'teaching-stats' database has not been upgraded.");
                    return false;
                }
            }

            Info("Upgrading the teaching-stats' database... ", false);
            ts.PerformDataDaseUpgrade();
            Success();
            Console.WriteLine();
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
    Console.WriteLine($"Fernando Porrino Serrano");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Under the AGPL license: ");
    Console.ResetColor();
    Console.WriteLine($"https://github.com/FherStk/teaching-stats-limesurvey/blob/main/LICENSE");
    Console.WriteLine();
}

void Test(){
    //Create survey
    // using(var ls = new LimeSurvey()){   
    //     var degreeName = "DAM";
    //     var departmentName = "Informàtica";    
    //     var groupName = "DAM2A";
    //     var trainerName = "Fernando Porrino";
    //     var subjectCode = "M05";
    //     var subjectName = "Entorns de Desenvolupament";
        
    //     ls.CreateSurveyFromCSV(LimeSurvey.Topic.SUBJECT_CCFF, degreeName, departmentName, groupName, trainerName, subjectCode, subjectName);
    // }

    //Export
    // using(var ls = new LimeSurvey()){
    //     using(var ts = new TeachingStats()){
    //         foreach(var s in ls.ListSurveys()){
    //             if((s["active"] ?? "").ToString() == "N") continue;
    //             int surveyID = int.Parse((s["sid"] ?? "").ToString());
    //             var type = ls.GetSurveyTopic(surveyID);
                
    //             if(type != null){
    //                 var answers = ls.GetSurveyResponses(surveyID);
    //                 var questions = ls.GetSurveyQuestions(surveyID);

    //                 ts.ImportFromLimeSurvey(questions, answers);
                    
    //                 //TODO: stop the LS surveys
    //             }
    //         }
    //     }
    // }

    //TODO: develop and test:
    //  1. Create from template (school, mentoring-1, mentoring-2)
    //  2. Export to teaching-stats all the templates

    //List
    // using(var ls = new LimeSurvey()){
    //     using(var ts = new TeachingStats()){
    //        Console.WriteLine(ls.ListSurveys());
    //     }
    // }

    
}