# 통합 테스트 진행 상황 (이 PC: 10.10.10.151)

작성일: 2026-09-10
대상 PC: `10.10.10.151` (MainServer, MiddleWare 실행 PC)

## 1. 이 PC에서 무엇을 했나

`inferenceclinet`(Client, 별도 저장소 `C:\csharp\inferenceclinet`)의 로그인 기능을 만든 뒤, 실제 DB·MainServer·MiddleWare·InferenceServer를 전부 연결해서 끝까지 동작하는지 통합 테스트를 진행했다.

## 2. 현재 실행 중인 서버 (이 PC)

| 서버 | 바인딩 | 비고 |
|---|---|---|
| MainServer | `http://0.0.0.0:5181` | 실 DB(`10.10.10.141:3307`) 연결, `Database__*` 환경변수로 접속정보 주입 |
| MiddleWare | `http://0.0.0.0:5073` | `appsettings.json`의 `Relay:InferenceServerBaseUrl`을 `http://10.10.10.141:8000/`로 설정 |

둘 다 `dotnet run --urls http://0.0.0.0:포트`로 실행 중 (재부팅하거나 터미널이 죽으면 다시 켜야 함 — 5절 참고).

## 3. 완료된 것

### 3-1. 로그인(①) — 실 DB까지 end-to-end 검증 완료

```
Client --client/login--> MiddleWare --login/client--> MainServer --SELECT HashPassword--> 실 DB
                                                            │
Client <--loginResponse-- MiddleWare <--login/Response------┘
```

`Login` 테이블에 테스트 계정(`e2etest` / 비밀번호 `test1234`, 해시는 salt 없는 SHA-256)을 심어두고 실제 Client 앱으로 로그인 성공까지 확인함.

### 3-2. 연속 촬영 전송 — Client 코드 구현 완료

- OpenCvSharp4로 웹캠 프레임 캡처 → `MainServer POST /Request` 직접 전송 (Middleware 안 거침, `ARCHITECTURE.md` 검증된 경로 기준)
- 결과는 `MainServer → MiddleWare → Client(POST /MainResponse)`로 비동기 수신
- 상세 설계는 `inferenceclinet` 저장소의 `docs/03-continuous-capture-plan.md` 참고

### 3-3. 네트워크 연동 문제 발견 및 해결

InferenceServer(`10.10.10.141`)와 실제로 연동을 시도하는 과정에서 이 PC 쪽 문제 두 가지를 발견하고 고쳤다.

| 문제 | 증상 | 조치 |
|---|---|---|
| MiddleWare/MainServer가 loopback에만 바인딩 | `127.0.0.1:5073`, `[::1]:5073`로만 LISTEN — 외부 PC 접근 불가 | `--urls http://0.0.0.0:포트`로 재실행 |
| 방화벽 규칙이 Public 프로필엔 안 걸림 | 이 PC 네트워크가 "Public"으로 분류돼 있어 `-Profile Private` 규칙이 무시됨 | `Set-NetConnectionProfile`로 이더넷을 Private로 변경 (팀 내부망 확인 후 진행) |

조치 후 `10.10.10.151:5073`, `:5181`에 LAN IP로 직접 TCP 연결이 성공하는 것까지 확인함.

## 4. 아직 안 풀린 문제

**네트워크는 뚫렸는데, InferenceServer가 결과를 돌려주지 않는다.**

`MainServer POST /Request` → 정상 접수(202) → MiddleWare → InferenceServer(`10.10.10.141:8000/message`, 직접 테스트 시 `202 queued` 응답 확인)까지는 되는데, 그 이후 InferenceServer가 `MiddleWare POST /ResponSE`로 결과를 보내는 단계가 40초 넘게 기다려도 오지 않음 (DB에 새 결과 저장 안 됨, 에러 로그도 없음).

**가능한 원인 (확인 필요, 이 PC에서는 더 진단 불가 — InferenceServer PC 접근 권한 없음)**

1. InferenceServer(`10.10.10.141`) PC도 이 PC와 같은 문제(네트워크 Public 분류, 방화벽)를 겪고 있어서 콜백을 못 보내는 중일 수 있음
2. 테스트에 사용한 이미지가 1×1픽셀짜리 가짜 JPEG라서, 실제 추론 모델이 처리 중 조용히 실패했을 수 있음 (진짜 웹캠 사진으로 재시도 필요)

## 5. 서버 재실행 명령 (이 PC)

```powershell
# MainServer
$env:Database__Host = "10.10.10.141"
$env:Database__Port = "3307"
$env:Database__Database = "inference_db"
$env:Database__User = "<DB 사용자>"
$env:Database__Password = "<별도 전달받은 비밀번호>"
dotnet run --project "C:\Users\user\source\repos\InferenceProject\MainServer\MainServer.csproj" --urls http://0.0.0.0:5181

# MiddleWare (새 터미널)
dotnet run --project "C:\Users\user\source\repos\InferenceProject\MiddleWare\MiddleWare.csproj" --urls http://0.0.0.0:5073
```

## 6. 다음에 할 일

- [ ] InferenceServer 담당자에게 그쪽 PC의 네트워크 분류(Public/Private)·방화벽·자체 로그 확인 요청
- [ ] 실제 웹캠 사진으로 Client `분석` 버튼을 눌러 재시도 (가짜 테스트 이미지 배제)
- [ ] 문제 해결되면 DB에 남은 테스트 데이터(`SuccessRate` 테이블의 `ProductName='테스트제품'`, `ClientId=1`, 5건) 정리
- [ ] `inferenceclinet` 쪽 `AppConfig.NumericClientId` 임시 고정값(`1`) — 로그인 문자열 ID와 실제 매핑 방법 팀과 확정
