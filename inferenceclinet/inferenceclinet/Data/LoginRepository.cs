using MySqlConnector;

namespace inferenceclinet.Data;

public class LoginRepository
{
    // 접속 정보는 하드코딩하지 않고 환경변수로 주입 (MainServer의 Database__* 관례와 동일).
    private static string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("Database__Host") ?? "127.0.0.1";
        var port = Environment.GetEnvironmentVariable("Database__Port") ?? "3306";
        var database = Environment.GetEnvironmentVariable("Database__Database") ?? "inference_db";
        var user = Environment.GetEnvironmentVariable("Database__User") ?? string.Empty;
        var password = Environment.GetEnvironmentVariable("Database__Password") ?? string.Empty;

        return $"Server={host};Port={port};Database={database};User Id={user};Password={password};";
    }

    public async Task<string?> GetHashPasswordAsync(string id, CancellationToken ct = default)
    {
        const string sql = "SELECT `HashPassword` FROM `Login` WHERE `ID` = @ID;";

        await using var connection = new MySqlConnection(BuildConnectionString());
        await connection.OpenAsync(ct);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ID", id);

        var result = await command.ExecuteScalarAsync(ct);
        return result as string;
    }
}
