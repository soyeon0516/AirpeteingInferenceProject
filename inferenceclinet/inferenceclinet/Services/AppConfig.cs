namespace inferenceclinet.Services;

public static class AppConfig
{
    public const string MiddlewareBaseUrl = "http://localhost:5073";

    // Middleware의 RelayOptions.ClientBaseUrl 기본값(http://localhost:5091/)과 맞춰야 함.
    public const string ClientListenUrl = "http://localhost:5091";

    // ARCHITECTURE.md/DATAFLOW.md 기준: 이미지 요청은 Middleware를 거치지 않고 MainServer 직접.
    public const string MainServerBaseUrl = "http://localhost:5181";

    // TODO: 로그인의 ID(문자열, 예 "e2etest")와 Request/MainResponse의 client(정수) 체계가 서로 다르다.
    // MainServer의 Client 테이블/로그인 Login.UID 중 무엇을 재사용할지 팀과 확정 전까지 임시 고정값 사용.
    public const int NumericClientId = 1;

    public const int CaptureIntervalMs = 3000;
    public const int ResponseWaitTimeoutMs = 5000;
}
