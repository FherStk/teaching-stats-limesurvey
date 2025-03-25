namespace TeachingStats2LimeSurvey;

internal class Program
{
    public const string Version = "2023-2024.2.1";
    public static Dictionary<Survey.Participant, List<Settings.SubjectData>>? EnrollmentWarnings {get; private set;}

    static void Main(string[] args)
    {
        EnrollmentWarnings = new Dictionary<Survey.Participant, List<Settings.SubjectData>>();

        DisplayInfo();
        //if(!CheckConfig()) return;
        //Utils.SerializeImportTemplateAsYamlFile();

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
                        SagaCsvToYml(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));                
                        break;  

                    case "--create-surveys":
                    case "-cs":
                        YmlToLimeSurvey(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));
                        break;    

                    case "--start-surveys":
                    case "-ss":
                        if(args.Length <= i+1) StartSurveys();
                        else {
                            if (int.TryParse(args[i + 1], out int id)) StartSurveys(id);
                            else StartSurveys(args[i + 1]);
                        }
                        break;  

                    case "--expire-surveys":
                    case "-es":
                        if(args.Length <= i+1) ExpireSurveys();
                        else {
                            if (int.TryParse(args[i + 1], out int id)) ExpireSurveys(id);
                            else ExpireSurveys(args[i + 1]);
                        }
                        break;  

                    case "--send-invitations":
                    case "-si":
                        if(args.Length <= i+1) SendInvitations();
                        else {
                            if (int.TryParse(args[i + 1], out int id)) SendInvitations(id);
                            else SendInvitations(args[i + 1]);
                        }
                        break; 

                    case "--send-reminders":
                    case "-sr":
                        if(args.Length <= i+1) SendReminders();
                        else {
                            if (int.TryParse(args[i + 1], out int id)) SendReminders(id);
                            else SendReminders(args[i + 1]);
                        }
                        break; 

                    // case "--load-teachingstats":
                    // case "-lt":
                    //     TeachingStatsToMetabase();
                    //     break;  

                    case "--load-limesurvey":
                    case "-ll":                    
                        if(args.Length <= i+1) LimeSurveyToMetabase();
                        else {
                            if (int.TryParse(args[i + 1], out int id)) LimeSurveyToMetabase(id);
                            else LimeSurveyToMetabase(args[i + 1]);
                        }
                        break;   

                    case "--load-googleforms-eso":
                    case "-lg-eso":      
                        GoogleFormsToMetabaseESO(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));                
                        break;        

                    case "--load-googleforms-risks":
                    case "-lg-risks":      
                        GoogleFormsToMetabaseRisks(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));                
                        break;        

                    case "--load-googleforms-families":
                    case "-lg-families":      
                        GoogleFormsToMetabaseFamilies(Directory.GetFiles(Path.GetDirectoryName(args[i+1]) ?? "", Path.GetFileName(args[i+1])));                
                        break;                           
                }  

                i++;
            }
        }
        Console.WriteLine();        
    }

#region Information
    /// <summary>
    /// Displays the app info (authors, licenses, etc.).
    /// </summary>
    private static void DisplayInfo(){
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Teaching Stats: ");
        Console.ResetColor();
        Console.WriteLine($"LimeSurvey (v{Version})");
        
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Copyright © {DateTime.Now.Year}: ");
        Console.ResetColor();
        Console.WriteLine($"Marcos Alcocer Gil");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Copyright © {DateTime.Now.Year}: ");
        Console.ResetColor();
        Console.WriteLine($"Fernando Porrino Serrano");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Under the AGPL license: ");
        Console.ResetColor();
        Console.WriteLine($"https://github.com/FherStk/teaching-stats-limesurvey/blob/main/LICENSE");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the cli arguments list.
    /// </summary>
    private static void Help(){
        Console.WriteLine();
        Info("dotnet run [arguments] <FILE_PATH>: ");
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.DarkBlue;        
        Console.WriteLine("Allowed arguments: ");
        Highlight("  -sc <FILE_PATH>, --saga-convert <FILE_PATH>", "parses SAGA's CSV files and creates some YML file which can be used to create new surveys (school, mentoring and subject) on LimeSurvey, a CSV file must be provided.");
        Highlight("  -cs <FILE_PATH>, --create-surveys <FILE_PATH>", "creates a new survey, a YML file must be provided.");    
        Highlight("  -ss, --start-surveys", "enables all the surveys at limesurvey for the given group (none means all) and sends the invitations to the participants.");
        Highlight("  -es, --expire-surveys", "stops and expires all the surveys at limesurvey for the given group (none means all) so no pending participants will be able to answer the surveys (no data will be removed).");
        Highlight("  -si, --send-invitations", "send the invitations for the given group (none means all), but just for whom has not received any yet (and only active surveys).");
        Highlight("  -sr, --send-reminders", "send survey reminders to all the participants for the given group (none means all) that still has not responded the surveys.");
        //Highlight("  -lt, --load-teachingstats", "loads to Metabase all pending reporting data from 'teaching-stats'.");
        Highlight("  -ll, --load-limesurvey", "loads to Metabase for the given group (none means all) all pending reporting data from expired surveys in 'lime-survey'.");
        //TODO: remove surveys
        Console.WriteLine();    
    }

    /// <summary>
    /// Used to check if the current computer has the prerrequisites needed to run the app.
    /// </summary>
    /// <returns>true or false.</returns>
    private static bool CheckConfig(){    
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
            Error($"ERROR: {ex}");
        }
        
        Console.WriteLine();

        return false;   
    }
#endregion
#region Output
    /// <summary>
    /// Sends any kind of text to the output, using the default color.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="newLine">If a new line should be added at the end.</param>
    private static void Info(string text, bool newLine = true){
        Console.ResetColor();    
        if(newLine) Console.WriteLine(text);
        else Console.Write(text);
    }

    /// <summary>
    /// Sends any success text to the output, in green.
    /// </summary>
    /// <param name="text">The text to display ("OK" by default).</param>
    private static void Success(string text = "OK"){
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ResetColor();    
    }

    /// <summary>
    /// Sends any warning text to the output, in orange.
    /// </summary>
    /// <param name="text">The text to display ("WARNING" by default).</param>
    private static void Warning(string text = "WARNING", bool newLine = true){
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(text);
        Console.ResetColor();    
    }

    /// <summary>
    /// Sends any error text to the output, in red.
    /// </summary>
    /// <param name="text">The text to display ("ERROR" by default).</param>
    private static void Error(string text = "ERROR"){
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();    
    }

    /// <summary>
    /// Sends a text as a request to the output, in pink, and returns a response.
    /// </summary>
    /// <param name="text">The question to display.</param>
    /// <param name="default">The default answer.</param>
    private static string Question(string text, string @default = ""){
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(text);
        Console.ResetColor();

        var response = Console.ReadLine();
        return (string.IsNullOrEmpty(response) ? @default : response);
    }

    /// <summary>
    /// Sends a highlighted text to the output, in yellow.
    /// </summary>
    /// <param name="high">The hightlighted text to display.</param>
    /// <param name="description">An associated description.</param>
    private static void Highlight(string high, string description){   
        Console.ForegroundColor = ConsoleColor.Yellow;        
        Console.Write(high);
        Console.ResetColor();
        Console.WriteLine($": {description}");    
    }
#endregion
#region SAGA to LimeSurvey
    /// <summary>
    /// Converts all the provided SAGA's CSV files to LimeSurvey compatible YAML files.
    /// </summary>
    /// <param name="files">A set of CSV file paths.</param>
    private static void SagaCsvToYml(string[] files){ 
        if(files.Length == 0) Error("Unable to find the specified file");
        else{
            foreach (var f in files.OrderBy(x => x))
            {
                //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
                //generates the 2nd level file and updates the 1st level ones).
                if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
                SagaCsvToYml(f);                    
            }     
        }                                   
    }

    /// <summary>
    /// Converts the provided SAGA's CSV file to a LimeSurvey compatible YAML file.
    /// </summary>
    /// <param name="files">A single CSV file path.</param>
    private static void SagaCsvToYml(string filePath){     
        var currentGroupName = Path.GetFileNameWithoutExtension(filePath);  //Must be like ASIX2B
        EnrollmentWarnings = new Dictionary<Survey.Participant, List<Settings.SubjectData>>();
        
        Info($"Converting from SAGA's CSV to a LimeSurvey compatible YAML file ({Path.GetFileName(filePath)}):");
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
            var degree = Utils.Settings.Data.Degrees.Where(x => x.Acronym == degreeName).SingleOrDefault();    

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
                    if(coma == -1) throw new InvalidDataException("NAME fields should be formated as 'Surname(s), Name'");

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
                            
                            if(topics.Where(x => x.SubjectAcronym == subjectData.Acronym).Count()  == 0){
                                //Enrollment data is loaded by UF, but topics must be stored by MP with no duplications.
                                topics.Add(new Survey.SurveyTopic(){                        
                                    SubjectAcronym = subjectData.Acronym,
                                    SubjectName = subjectData.Name,
                                    Topic = (subjectData.Acronym == "FCT" ? "FCT" : "SUBJECT-CCFF"),
                                    TrainerName = GetTrainerName(subjectData, p, currentGroupName)
                                });
                            }                        
                        }

                        //Adding the school survey
                        topics.Add(new Survey.SurveyTopic(){                                                    
                            Topic = "SCHOOL"
                        });

                        //Adding the school survey
                        topics.Add(new Survey.SurveyTopic(){                                                    
                            Topic = "SERVICES"
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
                }, Path.Combine(Utils.DataFolder, $"create-surveys-{currentGroupName}.yml")
            );

            if(EnrollmentWarnings.Count == 0) Success();    
            else{
                string info = $"WARNING: the following students are enrolled on '{currentGroupName}' but have been assigned to different groups. Please, fix them manually if needed (possibly are repeater students).\n";            
                foreach(var student in EnrollmentWarnings.Keys){
                    info += $"      - {student.Firstname} {student.Lastname}:\n";                
                    foreach(var subject in EnrollmentWarnings[student].Distinct()){
                        info += $"         - {subject.Acronym}: {subject.Name}\n";                
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

    /// <summary>
    /// Imports to LimeSurvey all the provided YAML files.
    /// </summary>
    /// <param name="files">A set of YAML file paths.</param>
    private static void YmlToLimeSurvey(string[] files){ 
        if(files.Length == 0) Error("Unable to find the specified file");
        else{
            foreach (var f in files.OrderBy(x => x))
            {
                //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
                //generates the 2nd level file and updates the 1st level ones).
                if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
                YmlToLimeSurvey(f);                    
            }     
        }                                   
    }

    /// <summary>
    /// Imports to LimeSurvey the provided YAML file, creating a new survey within a concrete survey group (check the relation between students groups and the survey groups at 'settings.yml'). YAML data files can be also created manually, follow the 'data/create-survey.yml.template' as a template.
    /// </summary>
    /// <param name="files">A single YAML file path.</param>
    private static void YmlToLimeSurvey(string filePath){
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
                    Error($"ERROR: {ex}");
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
#endregion
#region Import to Metabase
    /// <summary>
    /// Imports all the GoogleForms results to Metabase.
    /// </summary>
    /// <param name="files">A set of CSV file paths, the group will be taken from the file name.</param>
    private static void GoogleFormsToMetabaseFamilies(string[] files){ 
        if(files.Length == 0) Error("Unable to find the specified file");
        else{
            foreach (var f in files.OrderBy(x => x))
            {
                //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
                //generates the 2nd level file and updates the 1st level ones).
                if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
                GoogleFormsToMetabaseFamilies(f);                    
            }     
        }                                   
    }

    /// <summary>
    /// Imports to Metabase the provided Google Forms CSV file. The group data will be taken from the file's name.
    /// </summary>
    /// <param name="files">A single CSV file path.</param>
    private static void GoogleFormsToMetabaseFamilies(string filePath){
        Info($"Importing Google Froms CSV data from ({Path.GetFileName(filePath)})... ");        

        using(var ts = new TeachingStats()){      
            try{
                ts.ImportFromGoogleFormsFamilies(filePath);           
                Success($"OK");
            }
            catch(Exception ex){
                Error($"ERROR: {ex}");
            }
        }        
    }
    
    /// <summary>
    /// Imports all the GoogleForms results to Metabase.
    /// </summary>
    /// <param name="files">A set of CSV file paths, the group will be taken from the file name.</param>
    private static void GoogleFormsToMetabaseRisks(string[] files){ 
        if(files.Length == 0) Error("Unable to find the specified file");
        else{
            foreach (var f in files.OrderBy(x => x))
            {
                //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
                //generates the 2nd level file and updates the 1st level ones).
                if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
                GoogleFormsToMetabaseRisks(f);                    
            }     
        }                                   
    }

    /// <summary>
    /// Imports to Metabase the provided Google Forms CSV file. The group data will be taken from the file's name.
    /// </summary>
    /// <param name="files">A single CSV file path.</param>
    private static void GoogleFormsToMetabaseRisks(string filePath){
        Info($"Importing Google Froms CSV data from ({Path.GetFileName(filePath)})... ");        

        using(var ts = new TeachingStats()){      
            try{
                ts.ImportFromGoogleFormsRisks(filePath);           
                Success($"OK");
            }
            catch(Exception ex){
                Error($"ERROR: {ex}");
            }
        }        
    }
    
    /// <summary>
    /// Imports all the GoogleForms results to Metabase.
    /// </summary>
    /// <param name="files">A set of CSV file paths, the group will be taken from the file name.</param>
    private static void GoogleFormsToMetabaseESO(string[] files){ 
        if(files.Length == 0) Error("Unable to find the specified file");
        else{
            foreach (var f in files.OrderBy(x => x))
            {
                //Conversions must be done first for 1st level (which generates the 1st level file) and then for 2nd level (which
                //generates the 2nd level file and updates the 1st level ones).
                if(!File.Exists(f)) throw new FileNotFoundException("File not found!", f);
                GoogleFormsToMetabaseESO(f);                    
            }     
        }                                   
    }

    /// <summary>
    /// Imports to Metabase the provided Google Forms CSV file. The group data will be taken from the file's name.
    /// </summary>
    /// <param name="files">A single CSV file path.</param>
    private static void GoogleFormsToMetabaseESO(string filePath){
        Info($"Importing Google Froms CSV data from ({Path.GetFileName(filePath)})... ");        

        using(var ts = new TeachingStats()){      
            try{
                ts.ImportFromGoogleFormsESO(filePath);           
                Success($"OK");
            }
            catch(Exception ex){
                Error($"ERROR: {ex}");
            }
        }        
    }
    
    /// <summary>
    /// Imports all the LimeSurvey's results to Metabase.
    /// </summary>
    /// <param name="groupName">Only the surveys within this group will be affected (an empty string means all).</param>
    private static void LimeSurveyToMetabase(string groupName){
        LimeSurveyToMetabase(GetLimeSurveyGroupID(groupName));
    }
    
    /// <summary>
    /// Imports all the LimeSurvey's results to Metabase.
    /// </summary>
    /// <param name="groupID">The survey group to process.</param>
    private static void LimeSurveyToMetabase(int groupID = 0){        
        Info($"Loading data from LimeSurvey:");
        using(var ls = new LimeSurvey()){
            Info($"   Loading the survey list... ", false);        
            var list = ls.ListSurveys(groupID, LimeSurvey.Status.EXPIRED);
            Success();
            Console.WriteLine();

            int i = 1;    
            using(var ts = new TeachingStats()){                                
                foreach(var s in list){
                    int surveyID = int.Parse((s["sid"] ?? "").ToString());
                    // var type = ls.GetSurveyTopic(surveyID);
                    
                    // if(type != null){
                    var id = int.Parse((s["sid"] ?? "").ToString());
                    Info($"Importing all answers from LimeSurvey to Metabase, {i++}/{list.Count} with id={id}:");
                    
                    try{
                        Info("   Downloading data from LimeSurvey... ", false);
                        var answers = ls.GetSurveyResponses(surveyID);

                        if(answers == null) Info("   No answers received for this survey, skipping... ", false);
                        else{
                            var questions = ls.GetSurveyQuestions(surveyID);
                            Success();

                            Info("   Importing data into Metabase... ", false);
                            ts.ImportFromLimeSurvey(questions, answers);
                        }
                        
                        Success();                  
                    }
                    catch(Exception ex){
                        Error($"ERROR: {ex}");
                    }
                    finally{
                        Console.WriteLine();
                    }
                    // }
                }
            }
        }    
    }

    /// <summary>
    /// Imports all the Teaching-Stats results to Metabase.
    /// </summary>
    [Obsolete("The Theaching-Stats app should not be used anymore in order to generate surveys.")]
    private static void TeachingStatsToMetabase(){
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
                    Error($"ERROR: {ex}");
                }        

            }     
        }
    }
#endregion
#region Survey actions
    /// <summary>
    /// Starts all the surveys sending also the email invitations to the participants.
    /// </summary>
    /// <param name="groupName">Only the surveys within this group will be affected (an empty string means all).</param>
    private static void StartSurveys(string groupName){
        StartSurveys(GetLimeSurveyGroupID(groupName));
    }

    /// <summary>
    /// Starts all the surveys sending also the email invitations to the participants.
    /// </summary>
    /// <param name="groupID">Only the surveys within this group will be affected (0 means all).</param>
    private static void StartSurveys(int groupID = 0){
        Info($"Starting surveys from LimeSurvey:");
        using(var ls = new LimeSurvey()){            
            Info($"   Loading the survey list... ", false);        
            var list = ls.ListSurveys(groupID, LimeSurvey.Status.STOPPED);
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
                    Error($"ERROR: {ex}");

                    //TODO: wait and retry
                }
                finally{
                    Console.WriteLine();
                }
            }

            if(list.Count == 0) Warning($"Unable to load any non-active survey from limesurvey (within the current app group).");
            else{
                Console.WriteLine();
                Success("Process finished, all the non-active surveys for the given group have been activated.");  
            }  
        }
    }

    /// <summary>
    /// Expires (stops) all the surveys.
    /// </summary>
    /// <param name="groupName">Only the surveys within this group will be affected (an empty string means all).</param>
    private static void ExpireSurveys(string groupName){
        ExpireSurveys(GetLimeSurveyGroupID(groupName));
    }

    /// <summary>
    /// Expires (stops) all the surveys.
    /// </summary>
    /// <param name="groupID">Only the surveys within this group will be affected (0 means all).</param>
    private static void ExpireSurveys(int groupID = 0){
        Info($"Expiring surveys from LimeSurvey:");
        using(var ls = new LimeSurvey()){            
            Info($"   Loading the survey list... ", false);        
            var list = ls.ListSurveys(groupID, LimeSurvey.Status.ACTIVE);
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
                    Error($"ERROR: {ex}");

                    //TODO: wait and retry
                }            
            }

            if(list.Count == 0) Warning($"Unable to load any active survey from limesurvey (within the current app group).");
            else{
                Console.WriteLine();
                Success("Process finished, all the non-active surveys for the given group have been expired.");  
            }  
        }
    }

    /// <summary>
    /// Sends the invitations for all the surveys.
    /// </summary>
    /// <param name="groupName">Only the surveys within this group will be affected (an empty string means all).</param>
    private static void SendInvitations(string groupName){
        SendInvitations(GetLimeSurveyGroupID(groupName));
    }

    /// <summary>
    /// Sends the invitations for all the surveys.
    /// </summary>
    /// <param name="groupID">Only the surveys within this group will be affected (0 means all).</param>
    private static void SendInvitations(int groupID = 0){
        Info($"Sending invitations from LimeSurvey:");

        using(var ls = new LimeSurvey()){    
            Info($"   Loading the survey list... ", false);        
            var list = ls.ListSurveys(groupID, LimeSurvey.Status.ACTIVE);
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
                    Error($"ERROR: {ex}");

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

    /// <summary>
    /// Sends the reminders for all the surveys.
    /// </summary>
    /// <param name="groupName">Only the surveys within this group will be affected (an empty string means all).</param>
    private static void SendReminders(string groupName){
        SendReminders(GetLimeSurveyGroupID(groupName));
    }

    /// <summary>
    /// Sends the reminders for all the surveys.
    /// </summary>
    /// <param name="groupID">Only the surveys within this group will be affected (0 means all).</param>
    private static void SendReminders(int groupID = 0){
        //This option will start all the 'limesurvey' surveys (only for the surveys within the definded app setting's group, which should be the surveys created with this tool) sending also the email invitations to the participants.
        Info($"Sending reminders from LimeSurvey:");
        
        using(var ls = new LimeSurvey()){           
            Info($"   Loading the survey list... ", false);        
            var list = ls.ListSurveys(groupID, LimeSurvey.Status.ACTIVE);
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
                    Error($"ERROR: {ex}");
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
#endregion
#region Helpers 
    /// <summary>
    /// Translates the group name to the LimeSurvey0s group ID
    /// </summary>
    /// <param name="group">The group's name (like DAW1A)</param>
    /// <returns>The group ID</returns>
    private static int GetLimeSurveyGroupID(string group){
        if(string.IsNullOrEmpty(group)) return 0;
        else  if(Utils.Settings.LimeSurvey == null || Utils.Settings.LimeSurvey.Groups == null) return -1;
        else {
            var grp = Utils.Settings.LimeSurvey.Groups.SingleOrDefault(x => x.Group == group);
            if(grp == null) throw new UnableToFindGroupException($"Unable to find a LimeSurvey group ID for the given group name '{group}'.");
            else return grp.Id;
        }         
    }
    
    /// <summary>
    /// Returns the subject data.
    /// </summary>
    /// <param name="degreeData">The degree data containing all subject data for this degree.</param>
    /// <param name="subjectCode">The degree code which data should be returned.</param>
    /// <returns>The subject data.</returns>
    private static Settings.SubjectData GetSubjectData(Settings.DegreeData degreeData, string subjectCode){
        if(degreeData.Subjects == null) throw new Exception($"Unable to load any subject data.");

        var subjectData = degreeData.Subjects.SingleOrDefault(x => x.Ids != null && x.Ids.Contains(subjectCode));
        if(subjectData == null) throw new Exception($"Unable to find any subject with the code '{subjectCode}'");

        return subjectData;
    }

    /// <summary>
    /// Returns a trainer name.
    /// </summary>
    /// <param name="subjectData">The subject data.</param>
    /// <param name="participant">A participant data.</param>
    /// <param name="groupName">A group name.</param>
    /// <returns>A trainer name.</returns>
    private static string GetTrainerName(Settings.SubjectData subjectData, Survey.Participant participant, string groupName){
        var trainerName = string.Empty;
        if(EnrollmentWarnings == null) EnrollmentWarnings = new Dictionary<Survey.Participant, List<Settings.SubjectData>>();

        if(subjectData.Trainers != null){
            var trainerData = subjectData.Trainers.Where(x => x.Groups != null && x.Groups.Contains(groupName)).SingleOrDefault();
            trainerName = (trainerData == null ? string.Empty : trainerData.Name ?? string.Empty);

            if(string.IsNullOrEmpty(trainerName)){
                //If a 2nd course student is repeating a 1st course subject, a 1st group teacher will be got, but a warning must be displayed if there's more than one option.            
                trainerData = subjectData.Trainers.FirstOrDefault();
                trainerName = (trainerData == null ? string.Empty : trainerData.Name ?? string.Empty);

                if(subjectData.Trainers.Count() > 1){
                    List<Settings.SubjectData> subjects;

                    if(EnrollmentWarnings.ContainsKey(participant)) subjects = EnrollmentWarnings[participant];
                    else{
                        subjects = new List<Settings.SubjectData>();
                        EnrollmentWarnings.Add(participant, subjects);
                    }

                    subjects.Add(subjectData);
                }            
            }   
        } 

        if(string.IsNullOrEmpty(trainerName)) throw new Exception("Unable to get any trainer for the subject '{subjectData.Code}' within the group '{currentGroupName}'");
        return trainerName;
    }
#endregion
}