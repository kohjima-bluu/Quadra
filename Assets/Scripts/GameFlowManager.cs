using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameFlowManager : MonoBehaviour
{
    public GameState currentState = GameState.Title;

    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject drawUI;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private GameObject timeUI;
    [SerializeField] private TitleUIController titleUIC;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip titleBGM;
    [SerializeField] private AudioClip battleBGM_Timed;
    [SerializeField] private AudioClip battleBGM_Untimed;
    [SerializeField] private AudioClip winBGM;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip moveSE;
    [SerializeField] private AudioClip dropSE;
    [SerializeField] private AudioClip invertSE;
    [SerializeField] private AudioClip startSE;
    [SerializeField] private AudioClip settingSE;
    [SerializeField] private AudioClip winSE;


    private int blindV;
    private int blindH;


    private void Start()
    {
        SetState(GameState.Title);
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.Title:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    // ブラインド数の受け渡し
                    blindV = titleUIC.GetBlindVertical();
                    if (blindV == 0) gameManager.SetBlindVertical(8);
                    else gameManager.SetBlindVertical(blindV);
                    blindH = titleUIC.GetBlindHorizontal();
                    if (blindH == 0) gameManager.SetBlindHorizontal(0);
                    else gameManager.SetBlindHorizontal(blindH);
                    bool invert = titleUIC.IsInvertOn(); // ← すでにある getter 
                    gameManager.SetInvertEnabled(invert);
                    this.PlaySE(SEType.Start);
                    StartGame();
                }
                break;

            case GameState.WinResult:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    this.PlaySE(SEType.Start);
                    SetState(GameState.Title);
                }
                break;
            case GameState.DrawResult:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    this.PlaySE(SEType.Start);
                    SetState(GameState.Title);
                }
                break;
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        titleUI?.SetActive(newState == GameState.Title);
        winUI?.SetActive(newState == GameState.WinResult);
        drawUI?.SetActive(newState == GameState.DrawResult);
        timeUI?.SetActive(newState == GameState.Playing);

        // if (newState == GameState.Title)
        // {
        //     titleUIC?.ResetToDefaults(); // ←ここで毎回初期化
        // }

        // BGMの切り替え
        switch (newState)
        {
            case GameState.Title:
                PlayBGM(titleBGM);
                break;
            case GameState.Playing:
                int turnTime = titleUIC.GetTurnTime();
                if (turnTime == 0)
                    PlayBGM(battleBGM_Untimed);
                else
                    PlayBGM(battleBGM_Timed);
                break;
            case GameState.WinResult:
                PlayBGM(winBGM);
                break;
            case GameState.DrawResult:
                PlayBGM(winBGM); // 無音 or 好みに応じて設定
                break;
        }
    }

    private void StartGame()
    {
        int limit = titleUIC.GetTurnTime();
        gameManager.SetMaxTurnTime(limit);

        SetState(GameState.Playing);
        gameManager.StartNewGame(); // GameManager側に追加する関数
    }

    public void OnPlayerWin(int winnerPlayerId)
    {
        currentState = GameState.WinAnimation;

        if (winText != null)
        {
            winText.text = $"Player {winnerPlayerId} Wins!";
        }

        Invoke(nameof(ShowWinScreen), 1.0f); // 勝利アニメーションの後に画面切り替え
    }


    private void ShowWinScreen()
    {
        this.PlaySE(SEType.Win);
        SetState(GameState.WinResult);
    }

    public void OnPlayerDraw()
    {
        
        SetState(GameState.DrawResult);
    }

    private void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;

        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }

        if (bgmSource.clip != clip)
            bgmSource.clip = clip;

        bgmSource.Stop();  // ★ 明示的に止めてから
        bgmSource.Play();  // ★ 毎回頭から再生
    }
    
    public void PlaySE(SEType type)
    {
        //Debug.Log($"[SE] 再生要求: {type}");

        if (seSource == null)
        {
             //Debug.LogWarning("[SE] seSource is null!");
            return;
        }

        if (type == SEType.Setting && currentState != GameState.Title)
        {
            //Debug.Log($"[SE] SettingSEはタイトル画面でのみ再生されます（現在: {currentState}）");
            return;
        }

        AudioClip clip = null;

        switch (type)
        {
            case SEType.Setting: clip = settingSE; break;
            case SEType.Move:    clip = moveSE;    break;
            case SEType.Drop:    clip = dropSE;    break;
            case SEType.Invert:  clip = invertSE;  break;
            case SEType.Start:   clip = startSE;   break;
            case SEType.Win:     clip = winSE;     break;
        }

        if (clip != null)
            seSource.PlayOneShot(clip);
    }

}
