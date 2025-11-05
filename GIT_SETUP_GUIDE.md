# Git 저장소 설정 가이드

## 📋 개요

Unity MB 프로젝트의 Git 저장소 설정 및 사용 가이드입니다.

---

## 🚀 초기 설정

### 1. Git 저장소 초기화
```powershell
# MB 프로젝트 루트 디렉토리에서
cd D:\Data\02_Unity\03_MB\MB
git init
```

### 2. 사용자 정보 설정 (처음 한 번만)
```powershell
git config user.name "Your Name"
git config user.email "your.email@example.com"
```

### 3. 첫 커밋
```powershell
# 모든 파일 추가 (.gitignore 적용됨)
git add .

# 첫 커밋
git commit -m "Initial commit: Unity MB Project setup"
```

---

## 📁 .gitignore 적용 내역

### ✅ Git에 포함되는 것
- ✅ **모든 C# 스크립트** (`*.cs`)
- ✅ **문서 파일** (`*.md`, `Assets/@Scripts/docs/`)
- ✅ **프로젝트 설정** (`ProjectSettings/`)
- ✅ **패키지 매니페스트** (`Packages/manifest.json`)
- ✅ **Addressable 설정** (`Assets/AddressableAssetsData/*.asset`)
- ✅ **씬 파일** (`*.unity`)
- ✅ **프리팹** (`*.prefab`)
- ✅ **머티리얼** (`*.mat`)
- ✅ **애니메이션** (`*.anim`, `*.controller`)
- ✅ **모든 .meta 파일** (Unity 필수)

### ❌ Git에서 제외되는 것
- ❌ **Library/** (Unity 캐시, 용량 매우 큼)
- ❌ **Temp/** (임시 파일)
- ❌ **Obj/** (빌드 임시 파일)
- ❌ **Builds/** (빌드 결과물)
- ❌ **Logs/** (로그 파일)
- ❌ **대용량 비디오** (`.mp4`, `.mov`, `.avi`)
- ❌ **3D 모델 원본** (`.blend`, `.max`, `.ma`)
- ❌ **빌드 결과물** (`.exe`, `.apk`, `.ipa`)
- ❌ **IDE 설정** (`.vs/`, `.idea/`, `.vscode/`)
- ❌ **OS 생성 파일** (`.DS_Store`, `Thumbs.db`)

---

## 🔧 일반적인 Git 명령어

### 현재 상태 확인
```powershell
# 변경된 파일 확인
git status

# 변경 내용 상세 확인
git diff
```

### 파일 추가 및 커밋
```powershell
# 특정 파일 추가
git add Assets/@Scripts/Managers/GameManager.cs

# 특정 폴더 전체 추가
git add Assets/@Scripts/Managers/

# 모든 변경 사항 추가
git add .

# 커밋
git commit -m "feat: BrickGameManager 리팩토링 완료"
```

### 브랜치 관리
```powershell
# 브랜치 목록 확인
git branch

# 새 브랜치 생성 및 이동
git checkout -b feature/ball-auto-placement

# 브랜치 이동
git checkout main

# 브랜치 병합
git merge feature/ball-auto-placement
```

### 원격 저장소 연결 (GitHub)
```powershell
# 원격 저장소 추가
git remote add origin https://github.com/yourusername/MB.git

# 원격 저장소에 푸시
git push -u origin main

# 이후 푸시
git push
```

---

## 📊 커밋 메시지 컨벤션

### Conventional Commits 사용
```
feat: 새로운 기능 추가
fix: 버그 수정
refactor: 코드 리팩토링
docs: 문서 수정
style: 코드 포맷팅 (기능 변경 없음)
test: 테스트 코드 추가/수정
chore: 빌드 설정, 패키지 업데이트 등
```

### 예시
```powershell
git commit -m "feat: 공 자동 배치 시스템 구현"
git commit -m "fix: 패들이 방향키로 움직이지 않는 문제 수정"
git commit -m "refactor: BrickGameManager를 Non-MonoBehaviour로 전환"
git commit -m "docs: BALL_AUTO_PLACEMENT.md 가이드 추가"
```

---

## ⚠️ 주의사항

### 1. Library 폴더 절대 커밋 금지!
```powershell
# 만약 실수로 추가했다면
git rm -r --cached Library/
git commit -m "chore: Remove Library folder"
```

### 2. .meta 파일은 반드시 포함!
- Unity에서 Asset의 GUID를 관리
- .meta 파일 없으면 참조가 깨짐

### 3. 큰 파일 확인
```powershell
# 100MB 이상 파일 확인
git ls-files -s | awk '{if ($4 > 100000000) print $4, $2}'
```

### 4. Git LFS 사용 (대용량 파일)
```powershell
# Git LFS 설치 (한 번만)
git lfs install

# 특정 확장자 LFS로 관리
git lfs track "*.psd"
git lfs track "*.wav"
git lfs track "*.fbx"

# .gitattributes 커밋
git add .gitattributes
git commit -m "chore: Add Git LFS tracking"
```

---

## 🔍 유용한 명령어

### 히스토리 확인
```powershell
# 커밋 로그 확인
git log --oneline

# 그래프로 보기
git log --oneline --graph --all

# 특정 파일 히스토리
git log -- Assets/@Scripts/Managers/GameManager.cs
```

### 변경 사항 되돌리기
```powershell
# 작업 디렉토리 변경 취소
git checkout -- filename.cs

# 스테이징 취소
git reset HEAD filename.cs

# 마지막 커밋 수정
git commit --amend
```

### 파일 무시 추가 (이미 커밋된 파일)
```powershell
# 캐시에서 제거 (파일은 유지)
git rm --cached filename

# 폴더 전체
git rm -r --cached foldername/
```

---

## 📈 프로젝트 크기 최적화

### 현재 저장소 크기 확인
```powershell
git count-objects -vH
```

### 불필요한 파일 정리
```powershell
# GC 실행
git gc --aggressive --prune=now

# Reflog 정리 (신중하게!)
git reflog expire --expire=now --all
```

---

## 🌐 GitHub 워크플로우

### 1. Fork → Clone → Branch → PR 방식
```powershell
# 저장소 클론
git clone https://github.com/yourusername/MB.git

# 작업 브랜치 생성
git checkout -b feature/new-feature

# 작업 후 커밋
git add .
git commit -m "feat: 새 기능 추가"

# 원격 브랜치에 푸시
git push origin feature/new-feature

# GitHub에서 Pull Request 생성
```

### 2. 메인 브랜치 업데이트
```powershell
# main 브랜치로 이동
git checkout main

# 원격 저장소에서 최신 상태 가져오기
git pull origin main
```

---

## 📝 .gitattributes (선택사항)

대용량 파일이 있다면 추가:

```gitattributes
# Unity YAML
*.mat merge=unityyamlmerge eol=lf
*.anim merge=unityyamlmerge eol=lf
*.unity merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf
*.asset merge=unityyamlmerge eol=lf
*.meta merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf

# Git LFS (대용량 파일)
*.psd filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.exr filter=lfs diff=lfs merge=lfs -text
```

---

## 🎯 MB 프로젝트 전용 팁

### 브랜치 전략
```
main          - 안정 버전
develop       - 개발 통합
feature/*     - 새 기능 개발
bugfix/*      - 버그 수정
refactor/*    - 리팩토링
docs/*        - 문서 작업
```

### 커밋 예시
```powershell
# 기능 추가
git commit -m "feat: SessionManager 시스템 통합"

# 버그 수정
git commit -m "fix: WebSocket 모드 충돌 문제 해결"

# 리팩토링
git commit -m "refactor: BrickGameManager를 Non-MonoBehaviour로 전환"

# 문서
git commit -m "docs: BALL_AUTO_PLACEMENT.md 가이드 추가"
```

---

## 🚨 트러블슈팅

### Library 폴더를 실수로 커밋한 경우
```powershell
git rm -r --cached Library/
echo "Library/" >> .gitignore
git add .gitignore
git commit -m "chore: Remove Library folder and update .gitignore"
```

### 대용량 파일 경고
```
error: GH001: Large files detected.
```
→ Git LFS 사용 또는 해당 파일 제외

### 머지 충돌
```powershell
# 충돌 파일 확인
git status

# 수동으로 충돌 해결 후
git add conflicted-file.cs
git commit -m "fix: Resolve merge conflict"
```

---

**작성일**: 2025-10-20  
**프로젝트**: Unity MB (Unity 6000.0.56f1)

