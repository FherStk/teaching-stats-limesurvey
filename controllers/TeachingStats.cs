using Npgsql;
using System;
using Newtonsoft.Json.Linq;

public class TeachingStats : System.IDisposable{    
    public NpgsqlConnection Connection {get; private set;}

    public enum QuestionType{
        Numeric,
        Text
    }   

    public TeachingStats(){
        var settings = Utils.Settings;  
        if(settings == null || settings.TeachingStats == null) throw new IncorrectSettingsException();      
        
        this.Connection = new NpgsqlConnection($"Server={settings.TeachingStats.Host};User Id={settings.TeachingStats.Username};Password={settings.TeachingStats.Password};Database=teaching-stats;");
    }

    public void ImportFromLimeSurvey(JArray questions, JObject answers){
        //TODO: with the new "import CSV survey" mechanism, no quesion data will be needed. Everything will come at answer level.
        //      the only problem with the "import CSV mechanism" is that the "survey group/section" cannot be assigned automatically.
        var data = ParseFromLimeSurveyToTeachingStats(questions, answers);
        
        using(var context = new EF.TeachingStatsContext()){
            var lastID = context.Answers.OrderByDescending(x => x.EvaluationId).Select(x => x.EvaluationId).FirstOrDefault();            

            foreach(var item in data){                
                item.EvaluationId += lastID;
                item.Level = Cut(item.Level ?? "", 3);
                item.Department = Cut(item.Department ?? "", 75);
                item.Degree = Cut(item.Degree ?? "", 4);
                item.Group = Cut(item.Group ?? "", 11);
                item.SubjectCode = Cut(item.SubjectCode ?? "", 10);
                item.SubjectName = Cut(item.SubjectName ?? "", 75);
                item.Trainer = Cut(item.Trainer ?? "", 75);
                item.Topic = Cut(item.Topic ?? "", 25);
                item.QuestionType = Cut(item.QuestionType ?? "", 25);
            }
            
            context.Answers.AddRange(data);
            context.SaveChanges();
        }
    }

    private string Cut(string text, int maxLength){
        return (!string.IsNullOrEmpty(text) && text.Length > maxLength ? text.Substring(0, maxLength) : text);        
    }

    private List<EF.Answer> ParseFromLimeSurveyToTeachingStats(JArray questions, JObject answers){
        //NOTE: the questions json is needed because the LimeSurvey API is not exporting the question statement even when requested for...
        
        //Setup global data
        var statements = new Dictionary<string, string>();
        foreach(var q in questions){
            var title = (q["title"] ?? "").ToString();
            if(title.StartsWith("SQ") || title == "comments") statements.Add(title, (q["question"] ?? "").ToString());
        }            

        //Setting up responses
        var list = answers["responses"];
        if(list == null) throw new Exception("Unable to parse, the 'responses' array seems to be missing.");

        int evalID = 1;
        var importData = new List<EF.Answer>();
        foreach(var item in list){
            //TODO: check this "1" for multiple responses
            if(item.First == null || item.First.First == null) throw new Exception("Unable to parse, the 'responses' array seems to be empty.");
            var data = item.First.First;            

            //Load the responses
            var numeric = data.Children().Where(x => x.GetType() == typeof(JProperty)).Where(x => ((JProperty)x).Name.StartsWith("questions")).Cast<JProperty>().ToList();
            var comments = data.Children().Where(x => x.GetType() == typeof(JProperty)).Where(x => ((JProperty)x).Name.StartsWith("comments")).Cast<JProperty>().ToList();
            
            //Setup the question shared values
            //Unable to get only the completed ones (the API fails on filtering, so it will be filtered here)                                    
            //Timestamp and year
            var timeStamp = (data["submitdate"] ?? "").ToString();
            if(!string.IsNullOrEmpty(timeStamp)){
                var parsedDateTime = DateTime.Now;
                DateTime.TryParse(timeStamp, out parsedDateTime);
                var year = parsedDateTime.Year;
            
                //Store the splitted answers            
                short sort = 1;
                foreach(var answer in numeric.OrderBy(x => x.Name))
                    importData.Add(ParseAnswer(evalID, statements, data, answer, sort++, timeStamp, year, QuestionType.Numeric));

                foreach(var answer in comments.OrderBy(x => x.Name))
                    importData.Add(ParseAnswer(evalID, statements, data, answer, sort++, timeStamp, year, QuestionType.Text));
                
                //All the responses from the same group will share the ID;
                evalID++;    
            }                  
        }

        return importData;
    }

    private EF.Answer ParseAnswer(int evalID, Dictionary<string, string> statements, JToken data, JProperty answer, short sort, string timeStamp, int year, QuestionType type){
        var code = (type == QuestionType.Numeric ? answer.Name.Split(new char[]{'[', ']'})[1] : answer.Name);

        //Note: the answers will come as teaching-stats database needs, because has been setup like this within the 'equation' property.
        return new EF.Answer(){
            EvaluationId = evalID,
            QuestionSort = sort,
            Timestamp = DateTime.Parse(timeStamp),
            Year = year,
            Value = answer.Value.ToString(),
            QuestionStatement = statements[code],
            QuestionType = type.ToString(),
            Degree = (data["degree"] ?? "").ToString(),
            Department = (data["department"] ?? "").ToString(),
            Group = (data["group"] ?? "").ToString(),
            Level = (data["level"] ?? "").ToString(),
            SubjectCode = (data["subjectcode"] ?? "").ToString(),
            SubjectName = (data["subjectname"] ?? "").ToString(),
            Topic = (data["topic"] ?? "").ToString(),
            Trainer = (data["trainer"] ?? "").ToString()
        };
    }

    public void ImportFromTeachingStats(){        
        NpgsqlTransaction? trans = null;

        try{            
            this.Connection.Open();   //closed when disposing
            trans = this.Connection.BeginTransaction();
            
            //TODO: Teaching-Stats should not be used anymore, but if does:
            //      The evaluation_id could conflict with other sources, like lime-survey.
            //      The last evaluation_id must be found, and ensure that all are consecutive.
            //      Example: last evaluation_id from teaching-stats is 123; last evaluation_id from reports is 214 so: 214-123 = 91; all evaluation_id from teaching-stats must be ID+91+1.            
            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                INSERT INTO reports.answer
                SELECT * FROM reports.answer_all;
                ALTER TABLE reports.answer ADD COLUMN id SERIAL PRIMARY KEY;", this.Connection, trans)){
                
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

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                ALTER VIEW reports.answer RENAME TO answer_all;
                SELECT * INTO reports.answer FROM reports.answer_all;     
                ALTER TABLE reports.answer ADD id serial NOT NULL;     
                ALTER TABLE reports.answer ADD CONSTRAINT answer_pk PRIMARY KEY (id);              
                TRUNCATE TABLE public.forms_answer;
                TRUNCATE TABLE public.forms_participation;
                TRUNCATE TABLE public.forms_evaluation CASCADE;                        
                CREATE INDEX answer_year_idx ON reports.answer (""year"");", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                UPDATE master.degree SET ""code""='AIF' WHERE ""code""='AF';
                UPDATE reports.answer SET ""group""='AIF1A' WHERE ""group""='AIF1';
                UPDATE reports.answer SET ""group""='AIF2A' WHERE ""group""='AIF2';
                UPDATE reports.answer SET ""group""='ASIX1A' WHERE ""group""='ASIX1';
                UPDATE reports.answer SET ""group""='ASIX2A' WHERE ""group""='ASIX2';
                UPDATE reports.answer SET ""group""='DAM1A' WHERE ""group""='DAM1';
                UPDATE reports.answer SET ""group""='GA2A' WHERE ""group""='GA2';", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP14', 'FCT', 1, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP15', 'FCT', 3, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP16', 'Anglès tècnic', 3, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP14', 'FCT', 4, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP15', 'Anglès tècnic', 4, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP13', 'Anglès tècnic', 2, 4);
                INSERT INTO master.degree (code, name, degree_id, topic_id) VALUES ('MP13', 'FCT', 5, 4);

                UPDATE reports.answer SET ""group""='GA2A' WHERE ""group""='GA2';", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                DROP VIEW public.forms_subject;
                DROP VIEW public.forms_student;
                DROP VIEW reports.participation;
                DROP VIEW reports.answer_all CASCADE;
                ALTER TABLE master.""group"" ALTER COLUMN degree_id TYPE int4 USING degree_id::int4;
                ALTER TABLE master.""degree"" ALTER COLUMN department_id TYPE int4 USING department_id::int4;
                ALTER TABLE master.""degree"" ALTER COLUMN level_id TYPE int4 USING level_id::int4;
                ALTER TABLE master.question ALTER COLUMN type_id TYPE int4 USING type_id::int4;
                ALTER TABLE master.question ALTER COLUMN level_id TYPE int4 USING level_id::int4;
                ALTER TABLE master.question ALTER COLUMN topic_id TYPE int4 USING topic_id::int4;
                ALTER TABLE master.student ALTER COLUMN group_id TYPE int4 USING group_id::int4;
                ALTER TABLE master.subject ALTER COLUMN degree_id TYPE int4 USING degree_id::int4;
                ALTER TABLE master.subject ALTER COLUMN topic_id TYPE int4 USING topic_id::int4;
                ALTER TABLE master.subject_student ALTER COLUMN subject_id TYPE int4 USING subject_id::int4;
                ALTER TABLE master.subject_trainer_group ALTER COLUMN subject_id TYPE int4 USING subject_id::int4;
                ALTER TABLE master.subject_trainer_group ALTER COLUMN trainer_id TYPE int4 USING trainer_id::int4;
                ALTER TABLE master.subject_trainer_group ALTER COLUMN group_id TYPE int4 USING group_id::int4;
                ALTER TABLE public.forms_participation ALTER COLUMN student_id TYPE int4 USING student_id::int4;
                ALTER TABLE public.forms_evaluation ALTER COLUMN group_id TYPE int4 USING group_id::int4;
                ALTER TABLE public.forms_evaluation ALTER COLUMN subject_id TYPE int4 USING subject_id::int4;
                ALTER TABLE public.forms_evaluation ALTER COLUMN trainer_id TYPE int4 USING trainer_id::int4;
                ALTER TABLE public.forms_answer ALTER COLUMN question_id TYPE int4 USING question_id::int4;", this.Connection, trans)){                
                cmd.ExecuteNonQuery();                
            }
            
            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW public.forms_subject
                AS SELECT sb.id,
                    sb.code,
                        CASE
                            WHEN tr.name IS NULL THEN sb.name::text
                            ELSE concat(sb.name, ' (', tr.name, ')')
                        END AS name,
                    dg.id AS degree_id,
                    dg.code AS degree_code,
                    dg.name AS degree_name,
                    tr.id AS trainer_id,
                    st.group_id
                FROM master.subject sb
                    LEFT JOIN master.degree dg ON dg.id = sb.degree_id
                    LEFT JOIN master.subject_trainer_group st ON st.subject_id = sb.id
                    LEFT JOIN master.trainer tr ON tr.id = st.trainer_id;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW public.forms_student
                AS SELECT st.id,
                    st.email,
                    st.name,
                    st.surname,
                    lv.id AS level_id,
                    lv.code AS level_code,
                    lv.name AS level_name,
                    gr.id AS group_id,
                    gr.name AS group_name,
                    dg.id AS degree_id,
                    dg.code AS degree_code,
                    subjects.subjects
                FROM master.student st
                    LEFT JOIN master.""group"" gr ON gr.id = st.group_id
                    LEFT JOIN master.degree dg ON dg.id = gr.degree_id
                    LEFT JOIN master.level lv ON lv.id = dg.level_id
                    LEFT JOIN ( SELECT ss.student_id,
                            string_agg(su.code::text, ','::text) AS subjects
                        FROM master.subject_student ss
                            LEFT JOIN master.subject su ON ss.subject_id = su.id
                        GROUP BY ss.student_id) subjects ON subjects.student_id = st.id;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.participation
                AS SELECT pa.""timestamp"",
                    st.email,
                    st.surname,
                    st.name,
                    gr.name AS group_name,
                    dg.name AS degree_name,
                    lv.name AS level_name,
                    de.name AS department_name
                FROM forms_participation pa
                    LEFT JOIN master.student st ON st.id = pa.student_id
                    LEFT JOIN master.""group"" gr ON gr.id = st.group_id
                    LEFT JOIN master.degree dg ON dg.id = gr.degree_id
                    LEFT JOIN master.level lv ON lv.id = dg.level_id
                    LEFT JOIN master.department de ON de.id = dg.department_id;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_all
                AS SELECT ev.id AS evaluation_id,
                    ev.""timestamp"",
                    date_part('year'::text, ev.""timestamp"") AS year,
                    lv.code AS level,
                    de.name AS department,
                    dg.code AS degree,
                    gr.name AS ""group"",
                    su.code AS subject_code,
                    su.name AS subject_name,
                    tr.name AS trainer,
                    tp.name AS topic,
                    qu.sort AS question_sort,
                    ty.name AS question_type,
                    qu.statement AS question_statement,
                    an.value
                FROM forms_evaluation ev
                    LEFT JOIN master.""group"" gr ON gr.id = ev.group_id
                    LEFT JOIN master.trainer tr ON tr.id = ev.trainer_id
                    LEFT JOIN master.subject su ON su.id = ev.subject_id
                    LEFT JOIN forms_answer an ON an.evaluation_id = ev.id
                    LEFT JOIN master.question qu ON qu.id = an.question_id
                    LEFT JOIN master.degree dg ON dg.id = su.degree_id
                    LEFT JOIN master.department de ON de.id = dg.department_id
                    LEFT JOIN master.level lv ON lv.id = dg.level_id
                    LEFT JOIN master.topic tp ON tp.id = qu.topic_id
                    LEFT JOIN master.type ty ON ty.id = qu.type_id;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_cf_mp
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                FROM reports.answer_all
                WHERE answer_all.level::text = 'CF'::text AND answer_all.topic::text = 'Assignatura'::text;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_dept_adm
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                FROM reports.answer_all
                WHERE answer_all.department::text = 'Administració i gestió'::text;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_dept_adm_mp
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                FROM reports.answer_all
                WHERE answer_all.department::text = 'Administració i gestió'::text AND answer_all.topic::text = 'Assignatura'::text;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_dept_inf
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                FROM reports.answer_all
                WHERE answer_all.department::text = 'Informàtica i comunicacions'::text;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_dept_inf_mp
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                FROM reports.answer_all
                WHERE answer_all.department::text = 'Informàtica i comunicacions'::text AND answer_all.topic::text = 'Assignatura'::text;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                CREATE OR REPLACE VIEW reports.answer_all
                AS SELECT evaluation_id,
                    ""timestamp"",
                    year,
                    level,
                    department,
                    degree,
                    ""group"",
                    subject_code,
                    subject_name,
                    trainer,
                    topic,
                    question_sort,
                    question_type,
                    question_statement,
                    value
                FROM reports.answer;", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"                
                CREATE OR REPLACE VIEW reports.answer_dept_inf
                AS SELECT answer_all.evaluation_id,
                    answer_all.""timestamp"",
                    answer_all.year,
                    answer_all.level,
                    answer_all.department,
                    answer_all.degree,
                    answer_all.""group"",
                    answer_all.subject_code,
                    answer_all.subject_name,
                    answer_all.trainer,
                    answer_all.topic,
                    answer_all.question_sort,
                    answer_all.question_type,
                    answer_all.question_statement,
                    answer_all.value
                    FROM reports.answer_all
                WHERE answer_all.department in ('Informàtica i comunicacions', 'Informàtica');", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"                
                CREATE OR REPLACE VIEW reports.answer_dept_adm_mp
                AS SELECT *
                    FROM reports.answer_dept_adm
                WHERE topic = 'Assignatura';", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"                
                CREATE OR REPLACE VIEW reports.answer_dept_inf_mp
                AS SELECT *
                    FROM reports.answer_dept_inf
                WHERE topic = 'Assignatura';", this.Connection, trans)){
                
                cmd.ExecuteNonQuery();                
            }



            trans.Commit();                
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