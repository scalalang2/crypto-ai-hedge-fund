# Crypto AI Hedge Fund
[ai-hedge-fund](https://github.com/virattt/ai-hedge-fund)룰 암호화폐 버전으로 만든 실험용 프로젝트입니다.

절대 투자에 사용하지 마세요

<img width="1224" alt="image" src="https://github.com/user-attachments/assets/25f49753-3a3b-4512-ba9f-962cf50a357a" />

# 아키텍처
```
Participangs
- LeaderAgent : 총괄 책임자
- MarketAgent : 시장을 분석하는 에이전트
- SentimentAgent : 시장의 감정을 분석하는 에이전트
- RiskManagerAgent : 펀드 매니저의 결정 사항에 대해 리스크를 평가하고 최소화 하기
- CriticAgent : 펀드 매니저의 행동을 평가하는 에이전트
- TraderAgent : 실제로 거래를 수행하는 에이전트
```

# 실행하기
## 사전 준비
- 알림 수신용 디스코드 봇 토큰 생성
- OPEN AI API 키 발급
- 업비트 API 키 발급
- 구글 클라우드 GCP 계정 발급

## VM 인스턴스 생성하기
내가 자고 있는 동안에도 계속 트레이딩을 시키기 위해<br/>
구글 클라우드 GCP 계정으로 들어가서 VM 인스턴스를 생성합니다.
- OS는 Ubuntu 22.04 LTS를 선택합니다.
- [us-west1, us-central1, us-east1 에서 e2-micro 타입을 이용하는 경우 무료로 이용할 수 있습니다](https://cloud.google.com/free/docs/free-cloud-features#compute)
- 업비트 API는 고정 IP를 요구하기 때문에, 인스턴스를 생성한 뒤에는 고정 IP를 발급해줍니다.

## .NET 9 SDK 설치하기
프로젝트를 컴파일하고 실행시키기 위해서 [.NET 9 SDK를 설치합니다.](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2404)

```sh
$ sudo add-apt-repository ppa:dotnet/backports
$ sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-9.0
$ dotnet --version
9.0.105
```

프로젝트 빌드하기
```sh
$ dotnet publish AutogenCryptoTrader.sln -c Release -o ./publish --self-contained -r linux-x64
```

빌드를 모두 마치고 나면 `publish` 폴더에 실행파일과 설정파일이 생성됩니다.<br/>
`vim` 명령어로 `appsettings.json` 파일을 변경합니다.

```sh
vim appsettings.json

{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AgentRuntime": {
    "OpenAIApiKey": "<OpenAI Api 키>",
    "UpbitAccessKey": "<업비트 Access Key>",
    "UpbitSecretKey": "<업비트 Secret Key>",
    "DiscordBotToken": "<디스코드 봇 토큰>",
    "DiscordChannelId": <알림을 게시할 채널 ID>
  }
}
```

## 실행하기
```sh
$ cd publish
$ ./Server
```
