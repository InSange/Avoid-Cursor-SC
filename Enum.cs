public enum BossPhase
{
    Intro,
    Phase1,
    Phase2,
    Phase3,
    Die
}

public enum CursorState
{
    None,
    Intro,
    Idle,
    Walk,
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    SpecialAttack1,
    Skill1,
    Teleport,
    AutoAttack,
    Die,
    Hit,
}


public enum PlayerCursorState
{
    Idle,
    Grab,
    Wait,
    WaveAttack
}

public enum PanelState
{
    Hidden,
    Previewing,
    Expanded
}

/// <summary>
/// SystemFeature 상세 정의
/// 기능명 설명	게임 내 역할 아이디어	컨셉 키워드
/// CPU 중앙 처리 장치, 연산과 제어의 중심	스테이지 허브, 전체 기능 연결 / 로직 흐름	회로 중심, 심장, 전력 제어 노드
/// Memory 임시 데이터 저장소, 실행 정보 보관	일정 시간 후 증폭/재사용 / 과부하 시 폭주	램칩, 회로 트래픽, 휘발성 연출
/// IOSystem 외부와의 입력/출력 담당	플레이어 입력, 트리거/이벤트 중계	포트 연결, 입출력 인터페이스
/// Color	디스플레이 색상 정보 처리	컬러 복원 기능, 화면/이펙트 해금	RGB 전개, 채널 해방, 광원
/// GPU	그래픽 처리, 시각 연산	이펙트 강화, 패턴 해금 / 후반전 연출	시각 폭발, 픽셀 트위스트
/// Storage	장기 데이터 보관 (HDD/SSD)	체크포인트 / 스테이지 진행 저장	섹터 접근, 단편화된 데이터 조각
/// Network	외부 연결 및 통신 담당	적 스폰 / 실시간 전송 패턴 / 동기화	패킷 전송, 연결 불안정 연출
/// Security	시스템 보호 및 인증	적 감염 제거 / 감염된 기능 복원 해금	방화벽, 백신, 자물쇠 & 해킹 UI
/// Power	에너지 공급 / 회로 동작 유지	제한 시간 / 스킬 사용 제한 / 배터리 구역	발전기, 배터리 셀, 암전 연출
/// BIOS	부팅 & 시스템 초기화 펌웨어	전체 시스템 리셋 or 엔딩 연결	스타트 시점, 무한 루프 구간
/// Bus	컴포넌트 간 데이터 전달선	기능 간 동시 작동 제어 / 교통 혼잡	노드, 트래픽, 큐 처리
/// Scheduler	실행 우선순위 제어	적 스폰 순서 / 동시 다발 이벤트	타이머, 큐, 트리거 스케쥴
/// Cache	빠른 재사용 데이터 임시 저장	스킬 쿨타임 단축 / 공격 버퍼링	템포 강약 조절, 즉
/// VirtualMemory	스왑 메모리	과부하 상태, 리스크/보상 균형 요소
/// </summary>
public enum SystemFeature
{
    CPU, //
    Memory,
    IOSystem,
    Color,
    GPU,
    Storage,
    Network, //
    Security, //
    Power,
    BIOS,
    Bus,
    Scheduler,
    Cache,
    VirtualMemory,
    AI
}


public enum LogiEmotion
{
    Neutral,
    Happy,
    Surprised,
    Angry,
    Malicious,
    Hit
}

public enum EnvironmentPhase
{
    Phase1,
    Phase2,
    Phase3,
    Phase4,
    Phase5,
    Phase6,
    Phase7,
    Phase8,
    Phase9,
    Phase10,
}

/// <summary>
/// 게임의 모든 영구 해금 요소를 정의합니다.
/// (패시브, 아이템, 보스 NPC, 퀘스트 등)
/// </summary>
public enum UnlockID
{
    // --- 0. 기본/시스템 ---
    None,

    // --- 1. 영구 패시브 ---
    Passive_ColorRestored,      // 컬러 화면 해금
    Passive_MoveSpeedUp1,     // 이동 속도 증가 1단계
    Passive_DamageUp1,        // 공격력 증가 1단계

    // --- 2. '보스 갤러리' NPC 해금 ---
    BossNPC_NullFragment,       // 튜토리얼 보스
    BossNPC_CommandLineWarden,  // IO 시스템 보스
    BossNPC_DemonSword,         // Power 보스
    // (추후 보스 추가)

    // --- 3. 신규 커서 (캐릭터) 해금 ---
    Cursor_Default,
    Cursor_Angry,             // 예시: 공격형 커서
    Cursor_Shield,            // 예시: 방어형 커서

    // --- 4. 장비/아이템 해금 ---
    Item_WaveAttack_Wide,     // WaveAttack 범위 증가
    Item_FireSword,           // DemonSword가 드랍하는 아이템
    Item_Barrier,             // 1회성 방어막

    // --- 5. 퀘스트 해금 (퀘스트 자체를 해금) ---
    Quest_Survive10Min,         // 10분 생존 퀘스트
    Quest_Defeat10Bosses,       // 보스 10마리 처치 퀘스트
    Quest_Dodge1000Bullets,      // 총알 1000개 회피 퀘스트

    Item_CursorBlade,
    Item_MacroBeam,
    Item_CtrlZ,
    Item_FormatC,
    Item_ZipBomb,
    Item_Firewall,
    Item_Overclock,
    Item_VaccineShot,
    Item_SpamMail,
    Item_BlueScreen,
    Item_RecycleBin,
    Item_DDOS,
    Item_Incognito,
    Item_CopyPaste,
    Item_TaskKill,
    Item_Defrag,
    Item_VPN,
    Item_Screenshot,
    Item_CookieRun,
    Item_404,
    Art_RamStick,
    Art_SSD,
    Art_GPU,
    Art_Cooler,
    Art_PowerSupply,
    Art_MechanicalKey,
    Art_MousePad,
    Art_WifiAntenna,
    Art_USB,
    Art_Webcam,
    Art_Motherboard,
    Art_SoundCard,
    Art_LanCable,
    Art_ThermalPaste,
    Art_BackupHDD,
    Art_RGB,
    Art_BitCoin,
    Art_FloppyDisk,
    Art_OldCRT,
    Art_CleanMouse,
    Boss_DemonSword,
    Boss_Firewall,
    Boss_NullFragment,
    Boss_TrojanHorse,
    Boss_WormVirus,
    Boss_Ransomware,
    Boss_Spyware,
    Boss_MiningBot,
    Boss_SpamKing,
    Boss_DarkWeb,
    Boss_GlitchGhost,
    Boss_CpuOverheat,
    Boss_LogicBomb,
    Boss_Keylogger,
    Boss_PhishingSite,
    Boss_ZombiePC,
    Boss_DeepFake,
    Boss_Adware,
    Boss_Mainframe,
    Boss_CursorGod,

}

public enum AugmentType
{
    // --- Player Stats (Buff) ---
    Stat_MaxHPUp,
    Stat_MaxStaminaUp,
    Stat_MoveSpeedUp,
    Stat_CooldownReduction, // (Value: 10 -> 10%)
    Stat_HPRecover,         // (Value: 10 -> 10 회복)
    Stat_StaminaRegenUp,    // (Value: 0.5 -> 0.5 증가)

    // --- Artifacts & Passives (Buff) ---
    Artifact_SpeedBoostCycle, // 바람의 반지
    Passive_OrbitShield,      // 여신의 방패
    Passive_PoisonTrail,      // 포이즌 스킨

    // --- Artifact Upgrades ---
    // (바람의 반지 강화)
    Upgrade_SpeedBoost_Cooldown, // 쿨타임 감소
    Upgrade_SpeedBoost_Power,    // 속도 증가량 강화

    // --- Meta (Buff) ---
    Meta_ChoiceCountUp,       // 선택지 증가

    // --- Game Debuffs (Auto Applied) ---
    Debuff_BulletSpeedUp,
    Debuff_BulletCountUp,
    Debuff_BossCountUp,
    Debuff_PlayerSpeedDown,
    Debuff_ChoiceCountDown
}