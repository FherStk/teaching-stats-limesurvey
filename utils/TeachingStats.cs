using Npgsql;
using System;
using Newtonsoft.Json.Linq;

public class TeachingStats : System.IDisposable{    
    public NpgsqlConnection Connection {get; private set;}

    public enum QuestionType{
        Numeric,
        Text
    }

    private class ImportData {
        public string? TimeStamp {get; set;}   //keeps the original format (no reformating needed)
        public int Year {get; set;}
        public string? Level {get; set;}
        public string? Departament {get; set;}
        public string? Degree {get; set;}
        public string? Group {get; set;}
        public string? SubjectCode {get; set;}
        public string? SubjectName {get; set;}
        public string? Trainer {get; set;}
        public string? Topic {get; set;}
        public int QuestionSort {get; set;}
        public QuestionType Type {get; set;}
        public string? QuestionStatement {get; set;}
        public object? Value {get; set;}
    }

    public TeachingStats(){
        var settings = Utils.Settings;  
        if(settings == null || settings.TeachingStats == null) throw new IncorrectSettingsException();      
        
        this.Connection = new NpgsqlConnection($"Server={settings.TeachingStats.Host};User Id={settings.TeachingStats.Username};Password={settings.TeachingStats.Password};Database=teaching-stats;");
    }

    public void ImportFromLimeSurvey(JArray questions, JObject answers){
        var importData = ParseFromLimeSurveyToTeachingStats(questions, answers);
        
        //TODO: import into the databse. It could be nice to use an EntityFramework.
    }

    private List<ImportData> ParseFromLimeSurveyToTeachingStats(JArray questions, JObject answers){
        //NOTE: the questions json is needed because the LimeSurvey API does not allow changing the 'equation' property.
        
        //Setup global data
        var surveyData = new Dictionary<LimeSurvey.Question, string>();
        var statements = new Dictionary<string, string>();
        foreach(var q in questions){
            //Load the global question value
            var val = (q["question"] ?? "").ToString();
            if(string.IsNullOrEmpty(val)) continue;
            
            LimeSurvey.Question type;            
            if(!Enum.TryParse<LimeSurvey.Question>((q["title"] ?? "").ToString(), true, out type))
                type = LimeSurvey.Question.QUESTIONS;    

            if(type != LimeSurvey.Question.QUESTIONS && type != LimeSurvey.Question.COMMENTS) surveyData.Add(type, val.Split("'")[1]);            
            else{
                statements.Add((q["title"] ?? "").ToString(), (q["question"] ?? "").ToString());
                continue;
            }
        } 

        //Setting up responses
        var list = answers["responses"];
        if(list == null) throw new Exception("Unable to parse, the 'responses' array seems to be missing.");

        var importData = new List<ImportData>();
        foreach(var ans in list){
            //TODO: check this "1" for multiple responses
            var info = ans["1"];
            if(info == null) throw new Exception("Unable to parse, the 'responses' array seems to be empty.");

            //Load the responses
            var numeric = info.Children().Where(x => x.GetType() == typeof(JProperty)).Where(x => ((JProperty)x).Name.StartsWith("questions")).Cast<JProperty>().ToList();
            var comments = info.Children().Where(x => x.GetType() == typeof(JProperty)).Where(x => ((JProperty)x).Name.StartsWith("comments")).Cast<JProperty>().ToList();
            
            //Setup the question shared values            
            //Timestamp and year
            var timeStamp = (info["submitdate"] ?? "").ToString();
            var parsedDateTime = DateTime.Now;
            DateTime.TryParse(timeStamp, out parsedDateTime);
            var year = parsedDateTime.Year;
        
            //Store the splitted answers            
            int sort = 1;
            foreach(var n in numeric.OrderBy(x => x.Name))
                importData.Add(ParseAnswer(statements, surveyData, n, sort++, timeStamp, year, QuestionType.Numeric));

            foreach(var n in comments.OrderBy(x => x.Name))
                importData.Add(ParseAnswer(statements, surveyData, n, sort++, timeStamp, year, QuestionType.Text));
                      
        }

        return importData;
    }

    private ImportData ParseAnswer(Dictionary<string, string> statements, Dictionary<LimeSurvey.Question, string> surveyData, JProperty answer, int sort, string timeStamp, int year, QuestionType type){
        var code = answer.Name.Split(new char[]{'[', ']'})[1];                

        return new ImportData(){
            QuestionSort = sort,
            TimeStamp = timeStamp,
            Year = year,
            Value = int.Parse(answer.Value.ToString()),
            QuestionStatement = statements[code],
            Type = type,
            Degree = surveyData[LimeSurvey.Question.DEGREE],
            Departament = surveyData[LimeSurvey.Question.DEPARTMENT],
            Group = surveyData[LimeSurvey.Question.GROUP],
            Level = surveyData[LimeSurvey.Question.LEVEL],
            SubjectCode = surveyData[LimeSurvey.Question.SUBJECTCODE],
            SubjectName = surveyData[LimeSurvey.Question.SUBJECTNAME],
            Topic = surveyData[LimeSurvey.Question.TOPIC],
            Trainer = surveyData[LimeSurvey.Question.TRAINER]
        };
    }

    public void ImportFromTeachingStats(){        
        NpgsqlTransaction? trans = null;

        try{            
            this.Connection.Open();   //closed when disposing
            trans = this.Connection.BeginTransaction();
                                    
            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                INSERT INTO reports.answer
                SELECT * FROM reports.answer_all;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                TRUNCATE TABLE public.forms_answer;
                TRUNCATE TABLE public.forms_participation;
                TRUNCATE TABLE public.forms_evaluation CASCADE;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();
            }

            trans.Commit();
        }
        catch{
            if(trans != null) trans.Rollback(); 
            throw;
        }        
    }

    public bool CheckIfUpgraded(){        
        try{
            //closed on dispose
            this.Connection.Open();   
                
            using (NpgsqlCommand existCmd = new NpgsqlCommand("SELECT EXISTS (SELECT relname FROM pg_class WHERE relname='answer' AND relkind = 'r');", this.Connection)){
                return (bool)(existCmd.ExecuteScalar() ?? false);
            }
        }
        finally{
            this.Connection.Close();   
        }       
    }

    public void PerformDataDaseUpgrade(){      
        NpgsqlTransaction? trans = null;  
        try{
            //closed on dispose
            this.Connection.Open();   
            trans = this.Connection.BeginTransaction();    
            using (NpgsqlCommand upgradeCmd = new NpgsqlCommand(@"
                ALTER VIEW reports.answer RENAME TO answer_all;
                SELECT * INTO reports.answer FROM reports.answer_all;                        
                TRUNCATE TABLE public.forms_answer;
                TRUNCATE TABLE public.forms_participation;
                TRUNCATE TABLE public.forms_evaluation CASCADE;                        
                CREATE INDEX answer_year_idx ON reports.answer (""year"");", this.Connection, trans)){
                
                upgradeCmd.ExecuteNonQuery();
                trans.Commit();                
            }
        }
        catch{
            if(trans != null) trans.Rollback();
            throw;
        }
        finally{
            this.Connection.Close();   
        }       
    }
    
    public void Dispose()
    {
        // if(Connection.State != System.Data.ConnectionState.Closed)
        //     Connection.Close();
        
        Connection.Dispose();
    }
}