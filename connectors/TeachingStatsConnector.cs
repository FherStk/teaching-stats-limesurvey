using Npgsql;

public class TeachingStatsConnector{
    public NpgsqlConnection Connection {get; private set;}

    public TeachingStatsConnector(){
        this.Connection = new NpgsqlConnection(File.ReadAllText(Path.Combine(Utils.ConfigFolder, "teaching-stats-connection-string.txt")));
    }

}