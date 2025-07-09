using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TitleUIController : MonoBehaviour
{
    [SerializeField] private GameFlowManager gameFlowManager;

    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private TextMeshProUGUI winText;


    private int selectedIndex = 0;

    private int turnTime = 0;
    private bool invert = false;
    private int blindH = 0;
    private int blindV = 0;

    private readonly int turnTimeMin = 3;
    private readonly int turnTimeMax = 300;

    private bool isHoldingLeft = false;
    private bool isHoldingRight = false;
    private float holdTimer = 0f;
    private float timeSinceLastRepeat = 0f;
    private float repeatDelay = 0.4f;   // 最初の待機
    private float repeatRate = 0.2f;    // 初期繰り返し速度
    private float repeatMin = 0.05f;    // 最短間隔

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        HandleArrowInput();

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            gameFlowManager.PlaySE(SEType.Setting);
            UpdateUI();
        }

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            selectedIndex = Mathf.Min(selectedIndex + 1, optionTexts.Length - 1);
            gameFlowManager.PlaySE(SEType.Setting);
            UpdateUI();
        }
    }

    private void HandleArrowInput()
    {
        if (gameFlowManager == null || gameFlowManager.currentState != GameState.Title) return;

        bool leftDown = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
        bool rightDown = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;

        // 初回押下
        if ((Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame))
        {
            ModifyValue(-1);
            gameFlowManager.PlaySE(SEType.Setting);
            UpdateUI(); 
            isHoldingLeft = true;
            holdTimer = 0f;
            timeSinceLastRepeat = 0f;
        }
        else if (!leftDown)
        {
            isHoldingLeft = false;
        }

        if ((Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame))
        {
            ModifyValue(1);
            gameFlowManager.PlaySE(SEType.Setting);
            UpdateUI(); 
            isHoldingRight = true;
            holdTimer = 0f;
            timeSinceLastRepeat = 0f;
        }
        else if (!rightDown)
        {
            isHoldingRight = false;
        }

        // 長押し処理
        if (isHoldingLeft || isHoldingRight)
        {
            holdTimer += Time.deltaTime;
            timeSinceLastRepeat += Time.deltaTime;

            if (holdTimer > repeatDelay && timeSinceLastRepeat >= repeatRate)
            {
                ModifyValue(isHoldingLeft ? -1 : 1);
                timeSinceLastRepeat = 0f;
                repeatRate = Mathf.Max(repeatRate * 0.95f, repeatMin); // 少しずつ加速
            }
        }
        else
        {
            repeatRate = 0.2f; // リセット
        }
    }

    private void ModifyValue(int delta)
    {
        switch (selectedIndex)
        {
            case 0: // Turn Time
                if (turnTime == 0 && delta > 0)
                {
                    turnTime = 3;
                }
                else if (turnTime == 0 && delta < 0)
                {
                    // 何もしない（OFFより下なし）
                }
                else
                {
                    turnTime += delta;
                    if (turnTime < turnTimeMin)
                        turnTime = 0; // OFF
                    else if (turnTime > turnTimeMax)
                        turnTime = turnTimeMax;
                }
                break;
            case 1: // Invert
                if (delta != 0) invert = !invert;
                break;
            case 2: // Blind Horizontal
                blindH = Mathf.Clamp(blindH + delta, 0, 7);
                break;
            case 3: // Blind Vertical
                blindV = Mathf.Clamp(blindV + delta, 0, 6);
                break;
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            string prefix = (i == selectedIndex) ? "> " : "  ";
            switch (i)
            {
                case 0:
                    string timeDisplay = (turnTime == 0) ? "OFF" : $"{turnTime} sec";
                    optionTexts[i].text = $"{prefix}Time Limit       : {timeDisplay}";
                    break;
                case 1:
                    optionTexts[i].text = $"{prefix}Invert           : {(invert ? "ON" : "OFF")}";
                    break;
                case 2:
                    optionTexts[i].text = $"{prefix}Horizontal Blind : {(blindH == 0 ? "OFF" : blindH.ToString())}";
                    break;
                case 3:
                    optionTexts[i].text = $"{prefix}Vertical Blind   : {(blindV == 0 ? "OFF" : blindV.ToString())}";
                    break;
            }
        }
    }

    // 設定リセット
    // public void ResetToDefaults()
    // {
    //     turnTime = 0;
    //     invert = false;
    //     blindH = 0;
    //     blindV = 0;
    //     selectedIndex = 0;

    //     isHoldingLeft = false;
    //     isHoldingRight = false;
    //     holdTimer = 0f;
    //     timeSinceLastRepeat = 0f;
    //     repeatRate = 0.2f;

    //     UpdateUI();
    // }

    public int GetTurnTime() => turnTime; // 0 = 無制限として扱う
    public bool IsInvertOn() => invert;
    public int GetBlindHorizontal() => blindH;
    public int GetBlindVertical() => blindV;
}
