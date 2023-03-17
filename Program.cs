using Npgsql;
using Newtonsoft.Json.Linq;

//Global vars
var _VERSION = "0.0.1";

//Main
// DisplayInfo();
// if(!CheckConfig()) return;
// else Menu();

//Utils.SerializeSettingsTemplateAsYamlFile();
CreateNewSurvey();

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
                    CreateNewSurvey();
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

void CreateNewSurvey(){   
    var settings = Utils.Settings;
    if(settings == null || settings.Templates == null) throw new IncorrectSettingsException();

    int i = 1;
  
    var options = new Dictionary<int, object>();    
    foreach(var t in settings.Templates){
        options.Add(i++, new {
            ID = t.Value.Id,
            Caption = t.Value.Name ?? "",
            Type = t.Key 
        });
    }

    //int option = -1;
    int option = 1;
    while(option == -1){        
        Info("Please, select the template ID to create a new survey:");

        foreach(var e in options){
            dynamic v = e.Value;
            Info($"   {e.Key}: {v.Caption}");            
        }

        Info("   0: Exit");                        
        Console.WriteLine();

        
        if(!int.TryParse(Console.ReadLine(), out option)){
            Error("Please, select a valid option.");
            Console.WriteLine();
        }        
    }

    dynamic template = options[option];
    switch(template.Type){
        case "subject-ccff":
            CreateNewSubjectSurvey(template.ID);
            break;

        case "mentoring-1-ccff":
            break;

        case "mentoring-2-ccff":
            break;

        case "school":
            break;
    }
}

void CreateNewSubjectSurvey(int templateID){
    //TODO: split the method by template type (subject, school, etc...)

    // var degreeName = Question("Please, write the DEGREE NAME:");
    // var departmentName = Question("Please, write the DEPARTMENT NAME:");
    // var subjectCode = Question("Please, write the SUBJECT CODE:");
    // var subjectName = Question("Please, write the SUBJECT NAME:");
    // var groupName = Question("Please, write the GROUP NAME:");
    // var trainerName = Question("Please, write the TRAINER NAME:");    

    var degreeName = "DAM";
    var departmentName = "Informàtica";
    var subjectCode = "M10";
    var subjectName = "Sistemes de gestió empresarial";
    var groupName = "DAM2A";
    var trainerName = "Fernando Porrino";
    

    using(var ls = new LimeSurvey()){       
        try{            
            Info("Creating a new survey... ");    
            var surveyName = $"{degreeName} {subjectCode} - {subjectName}";
            if(!string.IsNullOrEmpty(trainerName)) surveyName += $" ({trainerName})";

            //Copying the survey with the correct name
            int newID = ls.CopySurvey(templateID, surveyName);            
            Success();

            //Loading the question IDs in order to set the correct values
            Info("Loading the survey data... ");    
            var qIDs = ls.GetSurveyQuestionIDs(newID);            
            Success();

            //Changing the copied question data with the correct values
            Info("Setting up the survey degree... ");    
            ls.SetQuestionProperties(qIDs[LimeSurvey.Question.DEGREE], JObject.Parse(
                @"{
                    ""question"" : ""degree: {'" + degreeName + @"'}""  
                }"
            ));           

            //TODO: this does not work!!!
            ls.SetQuestionProperties(qIDs[LimeSurvey.Question.DEGREE], JObject.Parse(
                @"{
                    ""attributes"" : {
                        ""equation"": ""{'" + degreeName + @"'}"",
                        ""hidden"": ""1""
                    } 
                }"
            ));
            //ls.SetQuestionProperties(qIDs[LimeSurvey.Question.DEGREE], new JObject(new JProperty("attributes", new JProperty("equation", "{'" + degreeName + "'}"))));
            Success();
            

            //TODO:
            /*
            {
                "qid": "3793",
                "parent_qid": "0",
                "sid": "195332",
                "gid": "218",
                "type": "*",
                "title": "degree",
                "question": "degree: {'DEGREE'}",
                "preg": "",
                "help": "",
                "other": "N",
                "mandatory": "N",
                "question_order": "5",
                "language": "ca",
                "scale_id": "0",
                "same_default": "0",
                "relevance": "1",
                "modulename": "",
                "available_answers": "No available answers",
                "subquestions": "No available answers",
                "attributes": {
                    "equation": "{'DEGREE'}",
                    "hidden": "1"
                },
                "attributes_lang": "No available attributes",
                "answeroptions": "No available answer options",
                "defaultvalue": null
                }
            */
            // var responseQuestions = JArray.Parse(ls.ReadClientResult() ?? "");
            // if(responseQuestions == null) throw new Exception($"Unable to read properties from the survey ID '{templateID}'");            

            


            // Info("Setting up the survey group... ");    
            // // ls.Client.Method = "set_survey_properties";
            // // ls.Client.Parameters.Add("sSessionKey", ls.SessionKey);
            // // ls.Client.Parameters.Add("iSurveyID", newID);
            // // ls.Client.Parameters.Add("aSurveyData", );
            // // ls.Client.Post();
            // // ls.Client.ClearParameters();

            // // int newID = 0;
            // // if(ls.Client.Response != null && ls.Client.Response.result != null){
            // //     var response = ls.Client.Response.result.ToString();
            // //     if(!int.TryParse(response, out newID)) throw new Exception($"Unable to parse the new survey ID from '{response}'");
            // // }
            // // Success();

        
        }
        catch(Exception ex){
            Error("Error: " + ex.ToString());
        }
    }
}

void LoadFromLimeSurvey(){
    var response = Question("This option will load all the current 'limesurvey' responses into the report tables, closing and cleaning the original surveys. Do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
        using(var ls = new LimeSurvey()){

        }

        //Get the existing sirveys within the correct groups.
        //For each survey:
        //  1. Warn if the survey is not ready to collect (its open or closed).
        //  2. Open a transaction to the database
        //  3. Download its data, cook it and store it into the database
        //  4. When done, close the survey.
        //  5. If everything worked, then commit the transaction (or rollback on error).
        //  6. Next survey.
        
      
 
    //   if(client.Response.StatusCode == System.Net.HttpStatusCode.OK){
    //     client.Method = "import_survey";
    //     client.Parameters.Add("sSessionKey", SessionKey);
    //     client.Parameters.Add("sImportData", Base64Encode(yourImportDataString));
    //     client.Parameters.Add("sImportDataType", "lss");
    //     //client.Parameters.Add("sNewSurveyName", "test");
    //     //client.Parameters.Add("DestSurveyID", 1);
    //     client.Post();
    //   }
 
    //   client.ClearParameters();
 
    //   Console.WriteLine("new survey id:" + client.Response.result.ToString());
    //   Console.ReadLine();
        
        // using(var conn = GetTeachingStatsConnection()){
        //     NpgsqlTransaction trans = null;

        //     try{
        //         conn.Open();
        //         trans = conn.BeginTransaction();
                            
        //         Info("Loading data into the reporting tables... ", false);
        //         using (NpgsqlCommand cmd = new NpgsqlCommand(@"
        //             INSERT INTO reports.answer
        //             SELECT * FROM reports.answer_all;", conn)){
                    
        //             cmd.ExecuteNonQuery();
        //         }
        //         Success();

        //         Info("Cleaning the original answers... ", false);
        //         using (NpgsqlCommand cmd = new NpgsqlCommand(@"
        //             TRUNCATE TABLE public.forms_answer;
        //             TRUNCATE TABLE public.forms_participation;
        //             TRUNCATE TABLE public.forms_evaluation CASCADE;", conn)){
                    
        //             cmd.ExecuteNonQuery();
        //         }
        //         Success();  

        //         trans.Commit();
        //         Success("Done!");
        //     }
        //     catch(Exception ex){               
        //         if(trans != null) trans.Rollback(); 
        //         Error("Error: " + ex.ToString());
        //     }
        //     finally{
        //         conn.Close();                
        //     }
        // }
    }
}

void LoadFromTeachingStats(){
    var response = Question("This option will load all the current 'teaching-stats' responses into the report tables, cleaning the original tables (evaluation, answer and participation). Do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
        NpgsqlTransaction? trans = null;

        try{
            using(var ts = new TeachingStats()){
                ts.Connection.Open();   //closed when disposing
                trans = ts.Connection.BeginTransaction();
                            
                Info("Loading data into the reporting tables... ", false);
                using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                    INSERT INTO reports.answer
                    SELECT * FROM reports.answer_all;", ts.Connection)){
                    
                    cmd.ExecuteNonQuery();
                }
                Success();

                Info("Cleaning the original answers... ", false);
                using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                    TRUNCATE TABLE public.forms_answer;
                    TRUNCATE TABLE public.forms_participation;
                    TRUNCATE TABLE public.forms_evaluation CASCADE;", ts.Connection)){
                    
                    cmd.ExecuteNonQuery();
                }
                Success();  

                trans.Commit();
                Success("Done!");
            }
        }
        catch(Exception ex){               
            if(trans != null) trans.Rollback(); 
            Error("Error: " + ex.ToString());
        }        
    }
}

bool CheckConfig(){        
    try{
        using(var ts = new TeachingStats()){    
            ts.Connection.Open();   //closed on dispose
            
            using (NpgsqlCommand existCmd = new NpgsqlCommand("SELECT EXISTS (SELECT relname FROM pg_class WHERE relname='answer' AND relkind = 'r');", ts.Connection)){
                var exists = (bool)(existCmd.ExecuteScalar() ?? false);
                if(!exists){
                    //The 'answer' table does not exists
                    var response = Question("The current 'teaching-stats' database has not been upgraded, do you want to perform the necessary changes to use this program? [Y/n]", "y");
                    if(response.ToLower() != "y"){
                        Error("The program cannot continue, becasue the 'teaching-stats' database has not been upgraded.");
                        return false;
                    }

                    //Must upgrade
                    Info("Upgrading the teaching-stats' database... ", false);
                    using (NpgsqlCommand upgradeCmd = new NpgsqlCommand(@"
                        ALTER VIEW reports.answer RENAME TO answer_all;
                        SELECT * INTO reports.answer FROM reports.answer_all;                        
                        TRUNCATE TABLE public.forms_answer;
                        TRUNCATE TABLE public.forms_participation;
                        TRUNCATE TABLE public.forms_evaluation CASCADE;                        
                        CREATE INDEX answer_year_idx ON reports.answer (""year"");", ts.Connection)){
                        
                        upgradeCmd.ExecuteNonQuery();
                    }
                    Success();
                    Console.WriteLine();

                }
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