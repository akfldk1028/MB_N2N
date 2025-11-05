# 게임 모드 전환 가이드 (1인 ↔ 2인)

## 📋 현재 설정

**기본값: 1인 로컬 테스트**
- Start 버튼 클릭 → **1명만 접속해도 즉시 게임 시작** ✅
- 랜덤 매칭 시스템 유지 (Lobby, Relay, Sessions API 모두 사용)
- 단지 "필요 인원"만 1명으로 설정

---

## 🔄 모드 전환 방법

### **한 줄만 수정하면 됩니다!**

**파일**: `Assets/@Scripts/Network/Common/GameModeService.cs` (59라인)

```csharp
// 1인 테스트 (현재 설정):
_currentMode = GameMode.LocalTest;

// 2인 랜덤 매칭:
_currentMode = GameMode.Multiplayer;
```

---

## 🎮 동작 방식

### 1인 테스트 모드 (현재)
```
Start 버튼 클릭
    ↓
MaxPlayers = 1
MinPlayersToStart = 1
    ↓
현재 1명 / 필요 1명 ✅
    ↓
즉시 GameScene 전환!
```

**로그 확인**:
```
[GameModeService] 게임 모드 서비스 생성 (기본: LocalTest)
[ConnectionManagerEx] 매칭 대기 중... (현재 1명 / 필요 1명)
[ConnectionManagerEx] 매칭 완료! 즉시 게임 시작
```

---

### 2인 랜덤 매칭 모드

```
Start 버튼 클릭
    ↓
MaxPlayers = 2
MinPlayersToStart = 2
    ↓
현재 1명 / 필요 2명 ⏳
    ↓
2명째 접속 → 게임 시작! ✅
```

**로그 확인**:
```
[GameModeService] 게임 모드 서비스 생성 (기본: Multiplayer)
[ConnectionManagerEx] 매칭 대기 중... (현재 1명 / 필요 2명)
(다른 플레이어 접속 시)
[ConnectionManagerEx] 매칭 완료! (현재 2명 / 필요 2명) 즉시 게임 시작
```

---

## 📊 비교표

| 항목 | 1인 테스트 | 2인 매칭 |
|------|----------|---------|
| **MaxPlayers** | 1 | 2 |
| **MinPlayersToStart** | 1 | 2 |
| **게임 시작 조건** | 즉시 | 2명 매칭 대기 |
| **랜덤 매칭 시스템** | ✅ 사용 | ✅ 사용 |
| **Lobby 생성** | ✅ | ✅ |
| **Relay 사용** | ✅ | ✅ |
| **Sessions API** | ✅ | ✅ |

---

## 🎯 핵심 정리

### 변경되는 것
- **필요 인원 수**: 1명 / 2명

### 변경되지 않는 것
- 랜덤 매칭 로직
- Lobby/Relay/Sessions API
- Start 버튼 하나로 모든 처리
- 네트워크 연결 방식

---

## 📝 수정 위치

**단 하나의 파일, 단 한 줄만 수정**:

```csharp
// Assets/@Scripts/Network/Common/GameModeService.cs (59라인)

public GameModeService()
{
    // 🔧 여기만 수정!
    _currentMode = GameMode.LocalTest;  // ← LocalTest (1인) / Multiplayer (2인)
    GameLogger.SystemStart("GameModeService", $"게임 모드 서비스 생성 (기본: {_currentMode})");
}
```

---

## 🚀 권장 설정

- **개발 중**: `GameMode.LocalTest` (1인 즉시 시작)
- **배포 버전**: `GameMode.Multiplayer` (2인 매칭 대기)

---

**작성일**: 2025-10-20  
**마지막 수정**: 2025-10-20
