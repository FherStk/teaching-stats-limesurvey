using Npgsql;
using Newtonsoft.Json.Linq;

public class TeachingStats : System.IDisposable{    
    public NpgsqlConnection Connection {get; private set;}

    private class ImportData {
        //int evaluation_id
    }

    public TeachingStats(){
        var settings = Utils.Settings;  
        if(settings == null || settings.TeachingStats == null) throw new IncorrectSettingsException();      
        
        this.Connection = new NpgsqlConnection($"Server={settings.TeachingStats.Host};User Id={settings.TeachingStats.Username};Password={settings.TeachingStats.Password};Database=teaching-stats;");
    }

    public void ImportFromLimeSurvey(JArray questions, JObject answers){
        //NOTE: the questions json is needed because the LimeSurvey API does not allow changing the 'equation' property.
        //This is the 'cook' needed in order to import LimeSurvey data into the teaching-stats database
        //1. Repeat the question block for each "questions[CODE] and comments"
        //2. id -> evaluation_id (get the next participationID, the same for each repeated block))
        //3. submitdate -> timestamp
        //4. null -> year (extract from timestamp)
        //5. level -> level
        //6. department -> departament (get the 'question' value for the 'title=department' within questions json)
        //7. degree -> degree (get the 'question' value for the 'title=degree' within questions json)
        //8. group -> group (get the 'question' value for the 'title=group' within questions json)
        //9. subjectcode -> subject_code (get the 'question' value for the 'title=subjectcode' within questions json)
        //10. subjectname -> subject_name (get the 'question' value for the 'title=subjectname' within questions json)
        //11. trainer -> trainer subject_name (get the 'question' value for the 'title=trainer' within questions json)
        //12. topic -> topic
        //13. null -> question_sort (get the order from 'questions[SQ00x]' and add the 'comments' at the end)
        //14. aaa -> value (get it from 'questions[SQ00x]' and the 'comments' fields)


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