using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.UI;

public class TerminalLine : MonoBehaviour
{
    public TMP_Text Text;
    private Coroutine _typingRoutine;
    private bool _skipRequested;

    public IEnumerator TypeText(string fullText, float charInterval = 0.02f)
    {
        if (Text == null)
            Text = GetComponent<TMP_Text>();

        _skipRequested = false;
        _typingRoutine = StartCoroutine(TypeRoutine(fullText, charInterval));
        yield return _typingRoutine;
    }

    private IEnumerator TypeRoutine(string text, float interval)
    {
        Text.text = "";
        foreach (char c in text)
        {
            if (_skipRequested)
            {
                Text.text = text;
                break;
            }

            Text.text += c;
            yield return new WaitForSeconds(interval);
        }
    }

    public void Skip() => _skipRequested = true;
}

public class TerminalManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject TerminalCanvas; // 터미널 UI 전체 (켜고 끄기용)
    public TMP_Text InputDisplay;     // 입력 텍스트 시뮬레이터
    public RectTransform Content;     // 텍스트 라인이 생성될 부모
    public GameObject OutputLinePrefab; // 텍스트 라인 프리팹
    public ScrollRect ScrollRect;     // 스크롤 뷰

    [Header("Print Settings")]
    public int MaxLines = 100;
    private Queue<GameObject> _lineQueue = new();

    // 명령어 처리
    private Dictionary<string, System.Action> _commands;
    private string _inputBuffer = "";

    // 커서 깜빡임
    private bool _showCursor = true;
    private float _cursorBlinkTimer = 0f;
    private const float CursorBlinkSpeed = 0.5f;
    private const string Prefix = "PLAYER@terminal:~$ > ";

    // 상호작용 상태
    [SerializeField] private bool _interactable = true; // 기본적으로 입력 가능

    // 퀘스트 수락/거절용 상태 변수
    [SerializeField] private QuestData _pendingQuest; // 수락 대기 중인 퀘스트
    [SerializeField] private bool _awaitingQuestConfirmation = false;
    private BaseBoss _currentInterlocutor; // 현재 대화 중인 보스

    // 출력 큐 관리
    private Queue<(string prefix, string message, Color color, float sizeScale)> _outputQueue = new();
    private bool _isPrinting = false;
    private Coroutine _printingCoroutine;


    private void Start()
    {
        if (TerminalCanvas != null)
            TerminalCanvas.SetActive(true);

        _interactable = true;
        if (InputDisplay != null)
            InputDisplay.gameObject.SetActive(_interactable);

        InitializeCommands();

        PrintSystem("SYSTEM READY. TYPE 'help' FOR COMMANDS.");
        RefreshInputDisplay();
    }

    private void Update()
    {
        if (_interactable)
        {
            HandleInput();
            BlinkCursor();

            if (Input.GetKeyDown(KeyCode.Tab))
                TryAutoComplete(); // 사용자 명령어 자동완성.
        }


        if (_isPrinting && Input.GetMouseButtonDown(0))
        {
            if (Content.childCount > 0)
            {
                Transform lastChild = Content.GetChild(Content.childCount - 1);
                TerminalLine lastLine = lastChild.GetComponent<TerminalLine>();
                lastLine?.Skip();
            }
        }
    }

    #region Input Handling
    private void HandleInput()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b' && _inputBuffer.Length > 0)
                _inputBuffer = _inputBuffer[..^1];
            else if ((c == '\n' || c == '\r') && !string.IsNullOrWhiteSpace(_inputBuffer))
                SubmitInput();
            else if (!char.IsControl(c))
                _inputBuffer += c;
        }

        RefreshInputDisplay();
    }

    private void SubmitInput()
    {
        string command = _inputBuffer.Trim().ToLower();
        PrintPlayer(command);
        _inputBuffer = "";
        RefreshInputDisplay();

        if (_awaitingQuestConfirmation)
        {
            HandleQuestConfirmation(command);
            return;
        }

        if (_commands.TryGetValue(command, out var action))
            action.Invoke();
        else
            PrintSystem("Unknown command. Type 'help' for available commands.");
    }

    private void BlinkCursor()
    {
        _cursorBlinkTimer += Time.deltaTime;
        if (_cursorBlinkTimer >= CursorBlinkSpeed)
        {
            _cursorBlinkTimer = 0f;
            _showCursor = !_showCursor;
            RefreshInputDisplay();
        }
    }

    private void RefreshInputDisplay()
    {
        string cursor = _showCursor ? "<color=white>|</color>" : " ";
        InputDisplay.text = $"{Prefix}{_inputBuffer}{cursor}";
    }

    private void TryAutoComplete()
    {
        string current = _inputBuffer.Trim().ToLower();
        foreach (string cmd in _commands.Keys)
        {
            if (cmd.StartsWith(current))
            {
                _inputBuffer = cmd;
                RefreshInputDisplay();
                return;
            }
        }
    }
    #endregion

    #region Quest Interaction (신규/수정)

    /// <summary>
    /// (BossGalleryManager가 호출) 퀘스트 대화를 시작합니다.
    /// </summary>
    public void StartQuestDialogue(BaseBoss boss, QuestData quest)
    {
        _currentInterlocutor = boss;
        _pendingQuest = quest;

        // TODO: 퀘스트 스크립트(대사)를 QuestData에서 가져오기
        // (임시 대사)
        string bossName = boss.GetType().Name.ToUpper();
        Print(bossName, $"...자네에게 줄 퀘스트가 있네.");
        Print(bossName, $"[ {quest.QuestName} ]");
        Print(bossName, $"{quest.QuestDescription}");
        PrintSystem($"퀘스트를 수락하시겠습니까? (yes / no)");

        _awaitingQuestConfirmation = true;
    }

    /// <summary>
    /// 퀘스트 수락/거절 (yes/no)을 처리합니다.
    /// </summary>
    private void HandleQuestConfirmation(string command)
    {
        if (command == "yes")
        {
            _awaitingQuestConfirmation = false;
            // 1. 퀘스트 매니저에 퀘스트 등록 (또는 활성화)
            // QuestManager.Instance.AcceptQuest(_pendingQuest);

            // 2. GameManager에 퀘스트 해금(활성화) 저장
            AvoidCursorGameManager.Instance.AchieveUnlock(_pendingQuest.QuestID);

            PrintSystem($"퀘스트 [ {_pendingQuest.QuestName} ]을(를) 수락했습니다.");

            // 3. 보스에게 응답
            Print(_currentInterlocutor.GetType().Name.ToUpper(), "좋아. 기대하지.");
        }
        else if (command == "no")
        {
            _awaitingQuestConfirmation = false;
            PrintSystem("요청이 취소되었습니다.");
            Print(_currentInterlocutor.GetType().Name.ToUpper(), "...");
        }
        else
        {
            PrintSystem("'yes' 또는 'no'로 응답해주세요.");
        }
        _pendingQuest = null;
        _currentInterlocutor = null;
    }

    /// <summary>
    /// (BossGalleryManager가 호출) 보스가 적대화될 때 강제로 대화를 중단합니다.
    /// </summary>
    public void ForceCloseDialogue()
    {
        // 1. 진행 중인 타이핑 중단
        if (_printingCoroutine != null)
        {
            StopCoroutine(_printingCoroutine);
            _isPrinting = false;
        }

        // 2. 출력 큐 비우기
        _outputQueue.Clear();

        // 3. 퀘스트 확인 상태 리셋
        _awaitingQuestConfirmation = false;
        _pendingQuest = null;
        _currentInterlocutor = null;

        // 4. 플레이어 입력 비활성화 (보스전 시작)
        _interactable = false;
        InputDisplay.gameObject.SetActive(false);

        PrintSystem("--- CONNECTION TERMINATED ---");
    }

    /// <summary>
    /// (BossGalleryManager가 호출) 전투 시작 시 터미널을 강제 종료합니다.
    /// </summary>
    public void CloseTerminal()
    {
        AbortInteraction(); // 상태 초기화 재활용

        _currentInterlocutor = null;
        _interactable = false;

        // 터미널 UI 전체 비활성화
        if (TerminalCanvas != null)
            TerminalCanvas.SetActive(false);
    }

    /// <summary>
    /// (신규) 터미널 창은 켜둔 채로, 진행 중인 상호작용(퀘스트 수락 대기 등)만 취소합니다.
    /// 보스가 적대화될 때 대화 흐름을 끊기 위해 사용합니다.
    /// </summary>
    public void AbortInteraction()
    {
        // 1. 입력 대기 상태 해제
        _awaitingQuestConfirmation = false;
        _pendingQuest = null;

        // 2. (선택) 현재 타이핑 중인 내용이 있다면 중단하고 큐 정리
        // (보스의 분노 대사가 즉시 나오게 하기 위함)
        if (_printingCoroutine != null) StopCoroutine(_printingCoroutine);
        _isPrinting = false;
        _outputQueue.Clear();

        // 3. 커서 입력 비활성화 (플레이어가 명령어 못 치게)
        // _interactable = false; 
        // (InputDisplay를 끄진 않음, 메시지만 출력할 것이므로)
    }
    #endregion

    #region 출력 헬퍼
    private void Print(string prefix, string message, Color? color = null, float sizeScale = 1.0f)
    {
        // 색상이 null이면 기본 흰색(또는 초록색 등 기존 색) 사용
        Color targetColor = color ?? Color.white;

        _outputQueue.Enqueue((prefix, message, targetColor, sizeScale));

        if (!_isPrinting)
            _printingCoroutine = StartCoroutine(ProcessNextOutput());
    }

    private IEnumerator ProcessNextOutput()
    {
        _isPrinting = true;

        while (_outputQueue.Count > 0)
        {
            var data = _outputQueue.Dequeue();
            string prefix = data.prefix;
            string message = data.message;
            Color textColor = data.color;
            float textScale = data.sizeScale;

            if (OutputLinePrefab != null && Content != null)
            {
                GameObject line = Instantiate(OutputLinePrefab, Content);

                TMP_Text text = line.GetComponent<TMP_Text>();
                if (text == null) text = line.GetComponentInChildren<TMP_Text>();

                if (text != null)
                {
                    // 💥 3. (신규) 텍스트 스타일 적용
                    text.color = textColor;           // 색상 변경
                    text.fontSize *= textScale;       // 크기 변경 (기본 폰트 사이즈 * 비율)

                    TerminalLine lineTyper = line.AddComponent<TerminalLine>();
                    yield return lineTyper.TypeText($"{prefix} {message}");
                }

                _lineQueue.Enqueue(line);
                if (_lineQueue.Count > MaxLines)
                {
                    GameObject oldest = _lineQueue.Dequeue();
                    Destroy(oldest);
                }
            }
            if (ScrollRect != null) ScrollRect.verticalNormalizedPosition = 0f;
        }

        _isPrinting = false;
        _printingCoroutine = null;
    }

    // 기본 시스템 메시지 (흰색, 1배)
    public void PrintSystem(string msg) => Print("SYSTEM@terminal:~$", msg, Color.white, 1.0f);

    // 플레이어 입력 메시지 (흰색, 1배)
    public void PrintPlayer(string cmd) => Print("PLAYER@terminal:~$", $"> {cmd}", Color.white, 1.0f);

    // 일반 보스 대화 (노란색 등, 1배)
    public void PrintBoss(string name, string msg) => Print($"{name}:", msg, Color.yellow, 1.0f);

    // 🔴 경고 메시지 (빨간색, 1.2배)
    public void PrintWarning(string msg) => Print("!!! WARNING !!!", msg, Color.red, 1.2f);

    // 🔴 보스 분노 대사 (빨간색, 1.5배 크게!)
    public void PrintBossAngry(string name, string msg) => Print($"{name}:", msg, Color.red, 1.5f);

    #endregion

    #region 명령어 기능
    private void InitializeCommands()
    {
        _commands = new Dictionary<string, System.Action>
        {
            { "help", ShowHelp },
            { "reboot", RebootSystem },
            { "clear", ClearScreen },
            { "exit", ExitToHub }
        };
    }

    private void ShowHelp()
    {
        PrintSystem("Available commands:");
        foreach (var cmd in _commands.Keys)
            PrintSystem("- " + cmd);
    }

    private void RebootSystem()
    {
        PrintSystem("Rebooting...");
        // 추후 씬 재시작 처리
    }

    private void ClearScreen()
    {
        // 화면에 있는 모든 라인 삭제
        foreach (var line in _lineQueue)
        {
            Destroy(line);
        }
        _lineQueue.Clear();
        RefreshInputDisplay();
    }

    private void ExitToHub()
    {
        PrintSystem("Exiting Simulation...");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReturnToHub();
        }
        else
        {
            // 매니저가 없으면 그냥 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene("HubScene");
        }
    }
    #endregion
}
