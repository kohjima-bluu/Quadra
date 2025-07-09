using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int playerId; 
    [SerializeField] private GameManager gameManager; // ← 名称統一
    [SerializeField] private GameFlowManager gameFlowManager;

    [SerializeField] private float repeatDelay = 0.5f; // 連続左右入力の間隔

    private bool isHoldingMove = false;
    private int moveDirection = 0;
    private float moveTimer = 0f;

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        if (context.started)
        {
            moveDirection = Mathf.RoundToInt(input.x);
            if (moveDirection != 0)
            {
                HandleMove(moveDirection);
                isHoldingMove = true;
                moveTimer = 0f;
            }
        }

        if (context.canceled)
        {
            isHoldingMove = false;
            moveDirection = 0;
        }
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            gameManager.TryDrop(playerId);
        }
    }

    public void OnInvert(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // ゲーム中以外は無視
            if (gameFlowManager.currentState != GameState.Playing) return;

            if (!gameManager.IsPlayerTurn(playerId)) return;

            gameManager.StartInvert();
            Debug.Log($"Player {playerId} pressed Invert");
        }
    }

    private void Update()
    {
        // 左右の連続入力処理
        if (isHoldingMove && moveDirection != 0)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= repeatDelay)
            {
                HandleMove(moveDirection);
                moveTimer = 0f;
            }
        }
    }

    // GameManagerへの左右処理
    private void HandleMove(int dir)
    {
        gameManager.TryMove(playerId, dir);
    }
}
