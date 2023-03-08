using Npgsql;

//Global vars
var _VERSION = "0.0.1";

//Main
DisplayInfo();
CheckDataBase();
Menu();


void Menu(){
    while(true){        
        Info("Please, select an option:");
        Info("   1: Load reporting data from 'teaching-stats'");
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

                default:
                    Error("Please, select a valid option.");
                    break;
            }
        }

        Console.WriteLine();
    }
    
}

void LoadFromTeachingStats(){
    var response = Question("This option will load all the current 'teaching-stats' responses into the report tables, cleaning the original tables (evaluation, answer and participation). Do you want no continue? [Y/n]", "y");
    if(response == "n") Error("Operation cancelled.");
    else{
        using(var conn = GetTeachingStatsConnection()){
            NpgsqlTransaction trans = null;

            try{
                conn.Open();
                trans = conn.BeginTransaction();
                            
                Info("Loading data into the reporting tables... ", false);
                using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                    INSERT INTO reports.answer
                    SELECT * FROM reports.answer_all;", conn)){
                    
                    cmd.ExecuteNonQuery();
                }
                Success();

                Info("Cleaning the original answers... ", false);
                using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                    TRUNCATE TABLE public.forms_answer;
                    TRUNCATE TABLE public.forms_participation;
                    TRUNCATE TABLE public.forms_evaluation CASCADE;", conn)){
                    
                    cmd.ExecuteNonQuery();
                }
                Success();  

                trans.Commit();
                Success("Done!");
            }
            catch(Exception ex){               
                if(trans != null) trans.Rollback(); 
                Error("Error: " + ex.ToString());
            }
            finally{
                conn.Close();                
            }
        }
    }
}

void CheckDataBase(){    
    using(var conn = GetTeachingStatsConnection()){
        try{
            conn.Open();
        
            using (NpgsqlCommand existCmd = new NpgsqlCommand("SELECT EXISTS (SELECT relname FROM pg_class WHERE relname='answer' AND relkind = 'r');", conn)){
                var exists = (bool)(existCmd.ExecuteScalar() ?? false);
                if(!exists){
                    //The 'answer' table does not exists
                    var response = Question("The current 'teaching-stats' database has not been upgraded, do you want to perform the necessary changes to use this program? [Y/n]", "y");
                    if(response.ToLower() != "y"){
                        Error("The program cannot continue, becasue the 'teaching-stats' database has not been upgraded.");
                        return;
                    }

                    //Must upgrade
                    Info("Upgrading the teaching-stats' database... ", false);
                    using (NpgsqlCommand upgradeCmd = new NpgsqlCommand(@"
                        ALTER VIEW reports.answer RENAME TO answer_all;
                        SELECT * INTO reports.answer FROM reports.answer_all;                        
                        TRUNCATE TABLE public.forms_answer;
                        TRUNCATE TABLE public.forms_participation;
                        TRUNCATE TABLE public.forms_evaluation CASCADE;                        
                        CREATE INDEX answer_year_idx ON reports.answer ('year');", conn)){
                        
                        upgradeCmd.ExecuteNonQuery();
                    }
                    Success();

                }
            }
        }
        catch(Exception ex){
            Error("Error: " + ex.ToString());
        }
        finally{
            conn.Close();
        }
    }
}

NpgsqlConnection GetTeachingStatsConnection(){
    return new NpgsqlConnection(File.ReadAllText(Path.Combine(ConfigFolder(), "teaching-stats-connection-string.txt")));
}

string ConfigFolder(){
    var executionFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
    var appFolder = executionFolder.Substring(0, executionFolder.IndexOf("bin"));
    appFolder = Path.TrimEndingDirectorySeparator(appFolder);
    return Path.Combine(appFolder, "config");
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
    Console.WriteLine($"Reports (v{_VERSION})");

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
    Console.WriteLine($"https://github.com/FherStk/teaching-stats-reports/blob/main/LICENSE");
    Console.WriteLine();
}