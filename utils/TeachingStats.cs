using Npgsql;

public class TeachingStats : System.IDisposable{
    public NpgsqlConnection Connection {get; private set;}

    public TeachingStats(){
        this.Connection = new NpgsqlConnection(File.ReadAllText(Path.Combine(Utils.ConfigFolder, "teaching-stats-connection-string.txt")));
    }

    public void Dispose()
    {
        if(Connection.State != System.Data.ConnectionState.Closed)
            Connection.Close();
        
        Connection.Dispose();
    }
}