using Npgsql;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

//Global vars
var _VERSION = "0.0.1";

//Main
// DisplayInfo();
// if(!CheckConfig()) return;
// else Menu();

//Methods
void Menu(){
    CreateNewSurvey();

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
    //TODO: do the inverse, fill the class and generate the yaml
    //TODO: create the settings.yml and fill all the settings there, it will be easier to manage with a static class

    var deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
    .Build();

    // var p = deserializer.Deserialize<Templates>(File.ReadAllText(Path.Combine(Utils.ConfigFolder, "templates.yml")));
    // var h = p.Surveys["school"];
    // using(var ls = new LimeSurvey()){
    //     try{



    //         Info("Creating a new survey... ");    
    //         ls.Client.Method = "copy_survey";
    //         ls.Client.Parameters.Add("sSessionKey", ls.SessionKey);
    //         ls.Client.Parameters.Add("iSurveyID_org", "123549");
    //         ls.Client.Parameters.Add("sNewname", "Automated Survey");
    //         ls.Client.Post();
    //         ls.Client.ClearParameters();

    //         int newID = 0;
    //         if(ls.Client.Response != null && ls.Client.Response.result != null){
    //             var response = ls.Client.Response.result.ToString();
    //             if(!int.TryParse(response, out newID)) throw new Exception($"Unable to parse the new survey ID from '{response}'");
    //         }
    //         Success();

    //         Info("Setting up the survey group... ");    
    //         // ls.Client.Method = "set_survey_properties";
    //         // ls.Client.Parameters.Add("sSessionKey", ls.SessionKey);
    //         // ls.Client.Parameters.Add("iSurveyID", newID);
    //         // ls.Client.Parameters.Add("aSurveyData", );
    //         // ls.Client.Post();
    //         // ls.Client.ClearParameters();

    //         // int newID = 0;
    //         // if(ls.Client.Response != null && ls.Client.Response.result != null){
    //         //     var response = ls.Client.Response.result.ToString();
    //         //     if(!int.TryParse(response, out newID)) throw new Exception($"Unable to parse the new survey ID from '{response}'");
    //         // }
    //         // Success();


    //     }
    //     catch(Exception ex){
    //         Error("Error: " + ex.ToString());
    //     }
    // }
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