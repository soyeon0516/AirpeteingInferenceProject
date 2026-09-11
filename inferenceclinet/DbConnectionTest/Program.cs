using inferenceclinet.Data;
using MySqlConnector;

// 접속 정보는 하드코딩하지 않고 환경변수로 주입 (Database__Host/Port/Database/User/Password).
string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("Database__Host") ?? "127.0.0.1";
    var port = Environment.GetEnvironmentVariable("Database__Port") ?? "3306";
    var database = Environment.GetEnvironmentVariable("Database__Database") ?? "inference_db";
    var user = Environment.GetEnvironmentVariable("Database__User") ?? string.Empty;
    var password = Environment.GetEnvironmentVariable("Database__Password") ?? string.Empty;

    return $"Server={host};Port={port};Database={database};User Id={user};Password={password};";
}

var ConnectionString = BuildConnectionString();

Console.WriteLine("1) 원시 연결 테스트 (SELECT 1)");
await using (var connection = new MySqlConnection(ConnectionString))
{
    await connection.OpenAsync();
    await using var command = new MySqlCommand("SELECT 1;", connection);
    var pingResult = await command.ExecuteScalarAsync();
    Console.WriteLine($"   연결 성공, SELECT 1 결과 = {pingResult}");
}

Console.WriteLine("2) Login 테이블 내 ID 목록 (최대 5개)");
await using (var connection = new MySqlConnection(ConnectionString))
{
    await connection.OpenAsync();
    await using var command = new MySqlCommand("SELECT `ID` FROM `Login` LIMIT 5;", connection);
    await using var reader = await command.ExecuteReaderAsync();
    var found = false;
    while (await reader.ReadAsync())
    {
        found = true;
        Console.WriteLine($"   - {reader.GetString(0)}");
    }
    if (!found)
    {
        Console.WriteLine("   (Login 테이블에 데이터가 없습니다)");
    }
}

Console.WriteLine("3) LoginRepository.GetHashPasswordAsync 테스트");
Console.Write("   조회할 ID 입력 (Enter만 누르면 'testuser'): ");
var inputId = Console.ReadLine();
var id = string.IsNullOrWhiteSpace(inputId) ? "testuser" : inputId;

var repository = new LoginRepository();
var hash = await repository.GetHashPasswordAsync(id);
Console.WriteLine(hash is null
    ? $"   ID '{id}' 없음 (HashPassword = null)"
    : $"   ID '{id}'의 HashPassword = {hash}");
