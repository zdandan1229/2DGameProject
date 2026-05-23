
이 폴더는 재사용하기 좋은 스크립트만 따로 모아 둔 정리용, 보관용, 이식 검토용 복사본 폴더입니다.

----------------------------------
1. 폴더 구성
----------------------------------

Data
- GameData.cs
  CharacterData, ItemData, DialogueData, MonsterData 같은
  게임 데이터 클래스들을 모아 둔 파일입니다.
  JSON 기반 데이터 구조를 한눈에 보기 좋습니다.

- GameUtil.cs
  데이터 로드 시작, 스프라이트 로드, 오디오 로드,
  대화 그룹 해석, 간단한 계산 같은 공용 유틸 함수가 들어 있습니다.

Managers
- GameDataManager.cs
  Resources/JsonOutput 안의 JSON 데이터를 읽어서
  Dictionary 형태로 보관하는 데이터 관리자입니다.
  다른 스크립트가 데이터를 찾을 때 중심이 되는 파일입니다.

- GameObjectManager.cs
  런타임에 생성된 오브젝트와 필드 오브젝트를 관리하는 파일입니다.
  SpawnSpot 같은 스크립트와 연결해서 보기 좋습니다.

- UIManager.cs
  UI를 실제로 생성하고, 열고, 닫고, 루트 Canvas 아래에 붙이는
  기본 UI 관리자입니다.
  UIManagerExtension.cs와 함께 봐야 흐름이 잘 보입니다.

- ResourceManager.cs
  Addressables 기반으로 에셋, 스프라이트, 프리팹을 로드하는
  리소스 관리자입니다.
  GameUtil.cs, GameObjectManager.cs 같은 파일이 이 매니저를 사용합니다.

- UIManagerExtension.cs
  UI enum, UI 경로 규칙, UI 열기/닫기 보조 메서드를 모아 둔 파일입니다.
  UIManager 본체가 있을 때 같이 쓰기 좋습니다.

UI
- UIBase.cs
  여러 UI 스크립트가 상속받는 가장 기본 베이스 클래스입니다.
  지금은 내용이 거의 없지만, 공통 UI 기능을 넣기 시작할 기준점이 됩니다.

- UIButton.cs
  버튼 클릭 바인딩, 기본 선택 상태 처리, 버튼 텍스트 변경을 도와주는
  버튼 래퍼 스크립트입니다.
  DialogueUI, MyProfilePopup, MainUI 같은 UI들이 이 스크립트를 기반으로 씁니다.

- DialogueUI.cs
  대화창 UI의 실제 동작을 담당합니다.
  Next 버튼, <np> 기준 페이지 분리, 다음 대사 연결 흐름이 들어 있습니다.

- MyProfilePopup.cs
  캐릭터 프로필 팝업을 표시하는 UI 스크립트입니다.
  캐릭터 데이터와 스킬 데이터를 읽어서 텍스트를 채웁니다.

World
- SpawnSpot.cs
  월드의 특정 지점에서 스폰 또는 이벤트를 시작하는 스크립트입니다.
  무엇을 시작할지, 언제 시작할지를 enum으로 나눠 둔 점이 재사용하기 좋습니다.

----------------------------------
2. 추천해서 보는 순서
----------------------------------

1) GameData.cs
   먼저 어떤 데이터 구조를 쓰는지 봅니다.

2) GameDataManager.cs
   그 데이터를 어디서 읽고 어떻게 보관하는지 봅니다.

3) GameUtil.cs
   실제 사용 시점에 데이터와 리소스를 어떻게 연결하는지 봅니다.

4) SpawnSpot.cs
   월드에서 이벤트가 어떻게 시작되는지 봅니다.

5) GameObjectManager.cs
   생성된 오브젝트를 어떻게 관리하는지 봅니다.

6) UIManager.cs
   UI가 실제로 어디에 생성되고 어떻게 열리는지 봅니다.

7) UIManagerExtension.cs
   UI 이름 규칙과 편의 메서드 흐름을 봅니다.

8) UIBase.cs, UIButton.cs
   UI 공통 베이스와 버튼 래퍼를 봅니다.

9) DialogueUI.cs, MyProfilePopup.cs
   마지막으로 데이터를 화면에 어떻게 보여주는지 봅니다.

----------------------------------
3. 이 폴더를 다른 프로젝트에 옮길 때
----------------------------------

이 파일들은 완전 독립형은 아닙니다.
아래 요소들이 같이 있어야 바로 쓰기 쉽습니다.

- UnityEngine
- Unity UI(Text, Image, RawImage 등)
- MonoBehaviour 기반 매니저 구조
- UIBase, UIButton, UIManager
- ResourceManager
- Cysharp UniTask
- Resources/JsonOutput 안의 JSON 파일
- 연결된 프리팹, 에셋, Inspector 참조

즉, 이 폴더는 "그대로 넣으면 완성"이라기보다
"재사용 가능한 핵심 코드 묶음"이라고 보는 편이 맞습니다.

----------------------------------
4. 활용 팁
----------------------------------

Data
- JSON 기반 게임 데이터 구조 예제로 쓰기 좋습니다.

Managers
- 작은 규모 또는 중간 규모 Unity 프로젝트의
  관리자 구조 템플릿으로 참고하기 좋습니다.

UI
- 데이터가 UI에 연결되는 흐름을 보는 예제로 좋습니다.
- UIBase, UIButton, UIManager까지 같이 있으므로
  작은 UI 프레임워크 묶음처럼 참고하기 좋습니다.

World
- 상호작용 지점, 이벤트 시작 지점, 스폰 포인트 설계 예제로 좋습니다.

다른 프로젝트로 옮길 때 추천 순서:
1) 프로젝트 규칙에 맞게 클래스명이나 enum 이름 정리
2) Resources 경로와 UI enum 값 확인
3) 매니저 이름과 싱글톤 접근 구조 연결
4) 프리팹, 씬 오브젝트, Inspector 참조 다시 연결

----------------------------------
5. 이 폴더의 성격
----------------------------------

- 원본은 여전히 Assets/Scripts 안에 있습니다.
- DNCodes는 따로 모아 둔 복사본 폴더입니다.
- 실제 실행용 소스라기보다, 정리된 참고용 코드 묶음에 가깝습니다.
