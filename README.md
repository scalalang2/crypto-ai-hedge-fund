> [!CAUTION]
> 절대 본인 투자에 사용하지 마세요

# Crypto AI Hedge Fund
[microsoft/AutoGen](https://github.com/microsoft/autogen)을 공부할 겸 시작한 실험용 프로젝트입니다.

[TradingAgents: Multi-Agents LLM Financial Trading Framework (AAAI'25)](https://openreview.net/attachment?id=4QPrXwMQt1&name=pdf) 논문과 [ai-hedge-fund](https://github.com/virattt/ai-hedge-fund) 프로젝트룰 참고하여 암호화폐 버전으로 암호화폐 트레이딩 멀티-에이전트 LLM을 만들고 있습니다.

![screenshot](https://github.com/user-attachments/assets/239a2abe-8643-4cc3-aa60-801226cc8719)

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
