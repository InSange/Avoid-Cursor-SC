## 씬(Scene) 구성

### 1. Main Scene (대기실 / Hub)
![g4](https://github.com/user-attachments/assets/ab7bdd16-6f63-488e-b0b3-c5a1ea2033a9)

플레이어가 전투를 준비하고 세계관과 상호작용하는 메인 공간입니다.
- **캐릭터 캐러셀(Carousel):** 거리 기반 보간을 통해 동적으로 크기와 투명도가 조절되는 캐릭터 선택 창.
- **아티팩트 세팅:** 해금된 유물과 증강을 장착하여 다음 전투의 전략을 구상.

### 2. Infinite Mode (전투 씬)
![g1](https://github.com/user-attachments/assets/e4757993-5f7a-448b-b594-cd62d6d9476b)

웨이브 기반의 로그라이크 전투가 벌어지는 공간입니다.
- **동적 난이도 & 디버프:** 시간이 지날수록 시스템 오류(디버프)가 축적되며, 플레이어의 현재 스탯과 보유 아이템을 분석해 유효한 증강(Augment)만 제공하는 뱀파이어 서바이버식 루팅 시스템. (`InfiniteModeManager.cs`)

### 3. Boss Gallery (도감 & NPC 상호작용)
![g7](https://github.com/user-attachments/assets/cc76bee3-7aab-4b0d-bcba-f531ae72d4e4)

무한 모드에서 조우한 보스들과 상호작용하는 공간입니다.
- **터미널 연출:** OS 프롬프트 형태의 터미널 타이핑 연출로 몰입감 제공.
- **메타 프로그레션:** 대화를 통해 퀘스트를 받거나, 선제공격하여 보스를 '영구 삭제(Permanent Kill)'시켜 다음 무한 모드 스폰 테이블에서 지워버리는 시스템.

---

### 1. 자체 제작 2D 애니메이션 엔진 (3-Tier Architecture)
Unity 기본 Animator의 오버헤드를 줄이고 프레임 단위의 정교한 이벤트를 제어하기 위해, **데이터-뷰-컨트롤러**가 분리된 커스텀 애니메이션 시스템을 구축했습니다.
- 📄 `CursorAnimation.cs`: 애니메이션 프레임, 지속 시간, 이벤트를 담는 순수 데이터 (ScriptableObject).
- 📄 `CursorObject.cs`: 시간에 맞춰 스프라이트를 렌더링하고, 특정 프레임에 이벤트를 발생시키는 뷰 렌더러.
- 📄 `CursorStateController.cs`: 상태(State) 전환을 검사하고 애니메이션을 안전하게 매핑하는 FSM 두뇌.

### 2. 이벤트 주도형(Event-Driven) 보스/플레이어 설계
매니저가 객체를 직접 제어하는 대신, 객체의 행동 결과를 이벤트로 전달하여 결합도를 낮췄습니다.
- 📄 `BaseBoss.cs`: 페이즈 전환, 도발 연출, 환경 패턴(`EnvironmentPatternBase`) 주입을 관리하는 보스 추상 클래스. 템플릿 메서드 패턴을 적용하여 자식 클래스가 `UseSkillPattern`만 구현하도록 설계.
- 📄 `PlayerLogicBase.cs`: 플레이어의 체력, 스태미나, 버프 스탯을 중앙 제어하며, 프레임 이벤트(`HandleAnimationEvent`) 처리를 자식에게 위임(SRP 준수).

### 3. JIT(Just-In-Time) 제네릭 오브젝트 풀링
탄막 게임의 퍼포먼스를 위해 씬 로드 시점이 아닌, **최초 요청 시점에 동적으로 풀을 생성**하는 최적화된 풀링 인프라를 구축했습니다.
- 📄 `PoolingControllerBase.cs`: `Type`을 Key로 사용하는 제네릭 풀링 시스템. (ex. `GetOrCreatePool<T>`)

### 4. 물리 기반의 커스텀 UI 및 DOTween 연출
마우스 자체가 플레이어(물리 객체)인 게임 특성상, 기존 EventSystem 대신 **2D 트리거(Trigger) 기반의 상호작용**을 직접 구현했습니다.
- 📄 `BaseCursorButton.cs`: `RectTransform` 크기에 맞춰 `BoxCollider2D`를 자동 동기화하고, 물리 충돌을 UI 클릭 이벤트(`UnityEvent`)로 브릿징.
- 📄 `PanelReturnTrigger.cs`: 화면 가장자리 도달 시 카메라가 이동하는 '공간감(Spatial UX)' 연출 시스템.

### 5. 개발 생산성 툴 (Custom Editor)
방대한 해금 데이터와 보스 히트박스를 관리하기 위한 전용 에디터 확장 툴을 개발했습니다.
- 📄 `DeveloperFeatureDebugEditor.cs`: 인스펙터 내에서 수백 개의 `UnlockID`를 검색하고 즉시 토글할 수 있는 실시간 디버깅 툴.
- 📄 `Boss_DemonSword_Editor.cs`: Scene View 상에서 마우스 드래그로 보스의 히트박스를 직접 시각화 및 조절(Undo 지원)할 수 있는 에디터 툴.
