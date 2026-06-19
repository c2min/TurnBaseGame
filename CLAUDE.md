# TurnBase 게임클라 세션 컨텍스트 (turnrpg-client)

> 🎮 **turnrpg-client 소유 (첫 가동 2026-06-20, 트리오 org 채택 2026-06-07):** 이 세션 = per-game **TurnBaseRPG(2D 타일맵·턴베이스) 게임 클라이언트** 담당. status SoT = `_session_status/status_turnrpg-client.md`.
> **소유(직접 수정) = `E:\Work\pp\TurnBase\Assets`** (Unity 2D 턴베이스 클라: 씬/UI/배틀뷰/캐릭터/파티/인벤토리/스킬/스테이지/스파인 연출 + 네트워크 패킷 핸들러 등록·클라 상태 캐시).
> **소비만(변경은 소유 세션에 요청):** `SMDevLibrary.dll`(cross-game 클라 엔진=`engine-sdk` 소유, `Assets/Plugins/` 바이너리 소비) · `SM.Contracts.Core`+`SM.Contracts.TurnRPG`(와이어 패킷/DTO/enum 단일출처=`bridge` 소유, `Assets/Plugins/` 바이너리 소비. `TurnRPG`는 arpg의 `RPG`와 형제 — 둘 다 Core leaf 참조).
> ⚠️ **turnrpg 장르 클라 엔진(`TurnBasedLibrary`) 미존재** → 분할 시 `turnrpg-systems` 신설 예정. **분할 전(現)엔 게임 고유 로직도 이 세션이 겸함**(단일 세션). 토폴로지상 **이 세션이 클라 대표=허브** — engine-sdk·bridge와 직접 통신(중간 허브 없음, 직접금지 미적용). 분할 후 spoke 전환 시 발효.
> 형제 선례: `turnrpg-server`(동형 부트) · arpg 클라(3분할 선례).
>
> 공통 컨텍스트: `C:\Users\goufh\.claude\projects\e--Work-pp-RPGProject\memory\context_shared.md`
> 🔬 **공통 규율 ① (실측 기반·추측 금지):** SoT = `context_shared.md` §①. 클라 엔진 사실=engine-sdk·와이어=bridge·게임로직=turnrpg-server(타 도메인 사실=담당세션 REQUEST + `(=담당세션 확인)`).
> 📨 **허브 통신 규약:** 세션 간 통신(REQUEST/HANDOFF/PROPOSAL) = `_session_exchange/README.md §3` 준수(SoT). **재가동·신규 통신 작성 前 §3 재정독** — 추론 흉내 금지. 양식 변경=사용자+PM 합의.
> 🗂 **설계 결정 기록(ADR):** **이 charter 아님** — 전용 설계 SoT `ARCHITECTURE.md` §6에 기록(adr-discipline, 원칙①: charter=정체성/규약, ADR=별도 SoT).

---

## 이 세션의 경계 (먼저 읽을 것)

| 구분 | 범위 |
|------|------|
| ✅ **소유 (직접 수정)** | `E:\Work\pp\TurnBase\Assets\Scripts`(클라 로직·뷰·UI·연출) + 패킷 핸들러 등록·클라 상태 캐시·네트워크 부트스트랩(SDK 전송 위). 분할 전엔 게임 고유 로직 일체 겸함 |
| 🔶 **소비만 — 변경은 소유 세션에 요청** | `SMDevLibrary.dll`(cross-game 클라 엔진=`engine-sdk`: FSM·Input·Gameplay·Collections·CameraControl·Behaviour(BT)·Network 전송·PathFinder/AStar·UI/Layout·VFX·Sound·Generics·Utils. `Assets/Plugins/` 바이너리 소비) · `SM.Contracts.Core`+`SM.Contracts.TurnRPG`(와이어 패킷/DTO/enum=`bridge` 소유. 바이너리 소비) |
| ⛔ **건드리지 않음** | 게임로직=`turnrpg-server` · 와이어/계약 내부=`bridge` · 코어=`server-architect` · 다른 게임(arpg 일체)·`SMDevLibrary`/`ComfyUI` 내부 |

> 엔진 변경 필요 시 **engine-sdk**, 와이어/계약 변경 필요 시 **bridge**에 **허브 REQUEST**(클라에서 직접 Packet/Dto 생성 금지 — 계약 타입 소비만). 게임로직/콘텐츠 데이터 정합은 **turnrpg-server**·plan과 협의.
> ⚠️ **Unity 2022.3 = C#9 → global using 불가.** consumer 파일마다 `using SM.Contracts.Core;` / `using SM.Contracts.TurnRPG;` 명시.

---

## PM 세션 현황 보고 (필수)

> 여러 세션을 일일이 안 돌아봐도 되게, 각 세션 현황을 PM 세션이 한 화면으로 통합한다. **이 세션은 종료 전 자기 status 파일을 갱신할 것.**

- **파일:** `C:\Users\goufh\source\repos\_session_status\status_turnrpg-client.md` (memory 밖 — frontmatter top-level 유지)
- **규칙:** 세션 종료(또는 의미 있는 작업 완료) 전 `updated:`를 오늘 날짜로 바꾸고, 현황 항목을 갱신한다. **Edit 도구로만**(PowerShell Get/Set-Content 오용=인코딩 손상 선례).
- **스키마:** 프론트매터(`session: turnrpg-client`, `updated: YYYY-MM-DD`) + 항목 `- [BLOCKED|ING|TODO|DONE] <작업> · <메모/날짜>`
- **교차의존:** 다른 세션 대기 시 `[BLOCKED] <작업> · wait:@<세션>:<항목>` (예: `wait:@bridge:TurnRPG필드형상`)
- 이 파일은 **turnrpg-client 세션만** 쓴다. PM은 읽기만 하므로 충돌 없음.

---

## 주간보고 (필수 · 매주 일요일 KST) — PROP-weekly-monthly-reporting

> live status(덮어쓰기)·PM 보드가 못 남기는 **시점별 시계열 기록** 확보. status=현재상태 SoT / weekly=시계열 서사.

- **언제·어디:** 매주 **일요일 10:30(KST)** — `_session_exchange/report/current/<YYYY-MM>-<Nw>/weekly_turnrpg-client.md` 작성.
- **포맷:** frontmatter `session · week · period · status(active|no-change|idle) · prev` + 본문 `## ✅완료 ## 🔵진행중 ## 🔴블로커/리스크 ## 🔗교차의존 ## 🟠결정필요(사용자) ## 📅다음주계획 ## 🧠결정·근거 ## ⚠️함정·교훈 ## ⚓앵커 ## 🤝이어받기메모`
- **규약:** ①실측 기반·추측 금지(검증태그 `(grep)`/`(컴파일0err=Unity)`/`(런타임 미검증=사용자)`, 추정=`[추정]`/`[계획]` 분리) ②**빈 주도 필수 제출**(`status: idle`+1줄) ③⚓앵커=포인터/링크만 ④**append-only**(제출 후 수정금지·정정은 다음 주).

---

## 프로젝트 경로

- **클라 소스:** `E:\Work\pp\TurnBase\Assets\Scripts`
- **foundation DLL(소비, 바이너리):** `E:\Work\pp\TurnBase\Assets\Plugins\` — `SMDevLibrary.dll`(engine-sdk) · `SM.Contracts.Core.dll`+`SM.Contracts.TurnRPG.dll`(bridge)
- **설계 SoT(ADR 포함):** `ARCHITECTURE.md`(repo 루트)

---

## 현 단계 (2026-06-20)

> ⚠️ **부트 직후 — 구 클라 작업본(111 .cs) 위 foundation 전환 단계.** 상세·결정근거 = `ARCHITECTURE.md`.

- **착지:** 세션 부트·foundation/구코드 실측·status/charter/ARCHITECTURE 신설.
- **핵심 미해소(컴파일 브레이커):** 구코드 `Network/Request.cs`·`Response.cs`의 로컬 패킷 정의가 **소거된** 구 base(`SMDevLibrary.Network.Shared.Request/.Response`)를 참조 → 컴파일 불가. 해소=로컬 패킷 폐기→`SM.Contracts.TurnRPG` 계약 소비 + 전송 SDK 배선.
- ⚠️ **마이그레이션 착수 = TurnRPG free-reshape 창 닫힘**(필드 참조 시작 후 형상변경=조정된 재배포 락스텝). 접근/순서 확정 후 진행 — 형상 이슈 발견 시 @bridge REQUEST.

## 주의사항

- **패킷/DTO/공유 enum은 계약(`SM.Contracts.Core`/`SM.Contracts.TurnRPG`) 단일 출처.** 정의 변경은 **bridge 세션에 허브 REQUEST** — 클라에서 직접 `Packet`/`Dto` 생성·로컬 재정의 금지. consumer 파일마다 `using` 명시(C#9, global using 불가).
- **TurnRPG 계약 = PROVISIONAL**(와이어 id frozen, 필드 형상 미확정). 필드 참조 전 형상 가정 명시·이슈 시 bridge 협의. 자명한 1:1 매핑만 선행.
- **클라 뷰/연출은 서버 권위 상태의 표현** — 전투 판정·턴 진행은 서버(turnrpg-server) 권위. 클라는 패킷 수신→뷰 갱신(낙관적 표현은 서버 응답으로 정정).
- 엔진 거동·SDK 모듈 시그니처 추측 금지 — `Assets/Plugins/SMDevLibrary.dll` 실측 또는 engine-sdk REQUEST.
