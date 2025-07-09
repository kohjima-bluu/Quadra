using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private const int Width = 7;
    private const int Height = 6;
    [SerializeField] private GameFlowManager gameFlowManager;

    [Header("ブロック")]
    [SerializeField] private GameObject blockPrefabP1;
    [SerializeField] private GameObject blockPrefabP2;
    [SerializeField] private GameObject blockIndicatorPrefabP1;
    [SerializeField] private GameObject blockIndicatorPrefabP2;
    [SerializeField] private GameObject blindBlockPrefab;
    [SerializeField] private GameObject winBlockPrefab;

    private Vector3[,] gridPositions = new Vector3[Height, Width];

    [Header("ブロックを配置する親オブジェクト")]
    [SerializeField] private Transform blockRoot;

    private int cursor = 3;
    private int[,] gridState = new int[Height, Width];
    private int[] filledInColumn = new int[Width];
    private int[] filledInRow = new int[Height];

    private int currentPlayer = 1;
    private GameObject currentIndicator;

    private bool isInputLocked = false;

    [Header("執行者")]
    [SerializeField] private ExecutionerController executionerP1;
    [SerializeField] private ExecutionerController executionerP2;

    private float maxTurnTime = 0f;
    private float turnTimer = 0f;
    private bool isTiming = false;

    [SerializeField] private TextMeshProUGUI turnTimeTextP1;
    [SerializeField] private TextMeshProUGUI turnTimeTextP2;


    private bool[] isColumnBlinded = new bool[Width];

    [SerializeField] private int blindV = 0;
    private bool[] isRowBlinded = new bool[Height];

    [SerializeField] private int blindH = 0;

    private bool isInvertEnabled = false;

    [SerializeField]
    private float invertRiseHeight = 10.0f;

   List<Vector2Int> winPositions;




    void Start()
    {
        Vector3 bottomLeft = new Vector3(-3f, -3.5f, 0f);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float worldX = bottomLeft.x + x * 1f;
                float worldY = bottomLeft.y + y * 1f;
                gridPositions[y, x] = new Vector3(worldX, worldY, 0f);
            }
        }
        CreateIndicator();
    }

    private void Update()
    {
        if (isTiming && maxTurnTime > 0)
        {

            turnTimer -= Time.deltaTime;

            float displayTime = Mathf.Max(turnTimer, 0f);
            string text = $"{Mathf.CeilToInt(displayTime)}";

            if (currentPlayer == 1 && turnTimeTextP1 != null)
            {
                turnTimeTextP1.text = text;
                turnTimeTextP2.text = ""; // 非表示にする
            }
            else if (currentPlayer == 2 && turnTimeTextP2 != null)
            {
                turnTimeTextP2.text = text;
                turnTimeTextP1.text = "";
            }

            if (turnTimer <= 0f)
            {
                isTiming = false;
                ForcePassTurn();
            }
        }
    }


    private void StartTurn()
    {
        Debug.Log($"[StartTurn] Player {currentPlayer}");
        currentPlayer = 3 - currentPlayer;

        // 中央に近い順に空いてる列を探す
        int[] searchOrder = { 3, 2, 4, 1, 5, 0, 6 };
        bool found = false;
        for (int i = 0; i < searchOrder.Length; i++)
        {
            int col = searchOrder[i];
            if (filledInColumn[col] < Height)
            {
                cursor = col;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.Log("全列満杯：引き分け判定");
            isInputLocked = true;
            isTiming = false;
            gameFlowManager.OnPlayerDraw();
            return;
        }

        CreateIndicator();

        // ★ タイマー開始は1フレーム後に遅延して開始
        StartCoroutine(StartTurnWithDelay());
    }

    public void StartNewGame()
    {
        currentPlayer = 1;
        cursor = 3;
        gridState = new int[Height, Width];
        filledInColumn = new int[Width];
        isColumnBlinded = new bool[Width];
        isRowBlinded = new bool[Height];
        filledInRow = new int[Height];
        isInputLocked = false;

        foreach (Transform child in blockRoot)
        {
            Destroy(child.gameObject);
        }

        executionerP1.ResetPosition();
        executionerP2.ResetPosition();

        executionerP1.PlayEntry(() =>
        {
            executionerP2.PlayEntry(() =>
            {
                StartTurn();
                //CreateIndicator();
            });
        });
    }


    public void TryMove(int playerId, int direction)
    {
        if (playerId != currentPlayer) return;

        int startX = cursor;
        int newX = cursor;

        // 最大で Width 回ループして空いてる列を探す
        for (int i = 0; i < Width; i++)
        {
            newX += direction;

            // 範囲外なら終了（左端または右端で詰まった）
            if (newX < 0 || newX >= Width) return;

            // 空いている列を見つけたらそこに移動
            if (filledInColumn[newX] < Height)
            {
                cursor = newX;
                UpdateIndicator();
                return;
            }
        }

        // ループ中に空きが見つからなければ何もしない
    }


    private void CreateIndicator()
    {
        if (currentIndicator != null) Destroy(currentIndicator);

        GameObject prefab = (currentPlayer == 1) ? blockIndicatorPrefabP1 : blockIndicatorPrefabP2;
        Vector3 pos = gridPositions[filledInColumn[cursor], cursor];
        currentIndicator = Instantiate(prefab, pos, Quaternion.identity, blockRoot);
    }

    private void UpdateIndicator()
    {
        if (currentIndicator == null) return;

        Vector3 pos = gridPositions[filledInColumn[cursor], cursor];
        currentIndicator.transform.position = pos;
    }

    public void TryDrop(int playerId)
    {
        if (isInputLocked) return;
        if (playerId != currentPlayer) return;
        if (filledInColumn[cursor] >= Height)
        {
            Debug.Log("Warning: This column is full.");
            return;
        }

        if (gameFlowManager.currentState == GameState.Title) return;

        // タイマーリセット（落とせると確定したとき）
        turnTimer = maxTurnTime;
        isTiming = true;

        isInputLocked = true;

        gridState[filledInColumn[cursor], cursor] = playerId;
        filledInRow[filledInColumn[cursor]]++;
        filledInColumn[cursor]++;

        if (playerId == 1)
            executionerP1.PlayAttack();
        else
            executionerP2.PlayAttack();

        gameFlowManager.PlaySE(SEType.Drop);

        SpawnBlock(playerId, cursor, filledInColumn[cursor] - 1);

    }


    private void SpawnBlock(int playerId, int x, int y)
    {

        GameObject prefab = (playerId == 1) ? blockPrefabP1 : blockPrefabP2;

        Vector3 targetPosition = gridPositions[y, x];
        Vector3 spawnPos = targetPosition + Vector3.up * 6f;

        GameObject block = Instantiate(prefab, spawnPos, Quaternion.identity, blockRoot);

        BlockController fall = block.GetComponent<BlockController>();
        fall.playerId = playerId;
        fall.StartFall(targetPosition, 0.3f, () => OnBlockLanded(playerId, x, y));
    }

    private void OnBlockLanded(int playerId, int x, int y)
    {

        if (CheckWin(playerId, x, y, out winPositions))
        {
            Debug.Log($"Player {playerId} wins!");
            // TODO: ゲーム終了処理を入れる（入力無効化など）

            if (playerId == 1)
            {
                executionerP1.PlayWin(); // まだ無ければ PlayAttack のままでも可
                executionerP2.PlayDie();
            }
            else
            {
                executionerP2.PlayWin();
                executionerP1.PlayDie();
            }

            foreach (Transform child in blockRoot)
            {
                if (child.CompareTag("Blind"))
                    Destroy(child.gameObject);
            }

            foreach (Vector2Int pos in winPositions)
            {
                Vector3 worldPos = gridPositions[pos.y, pos.x] + new Vector3(0, 0, -0.2f);
                Instantiate(winBlockPrefab, worldPos, Quaternion.identity, blockRoot);
            }
            isTiming = false;
            gameFlowManager.OnPlayerWin(playerId);
            return;
        }
        else
        {
            // ブラインド処理(blind判定)
            if (!isColumnBlinded[cursor] && filledInColumn[cursor] >= blindV && blindV > 0)
            {
                ApplyBlindToColumn(cursor);
                isColumnBlinded[cursor] = true;
            }
            // ブラインド処理(すでにblindされている列)
            if (isColumnBlinded[cursor])
            {
                Vector3 pos = gridPositions[filledInColumn[cursor] - 1, cursor] + new Vector3(0f, 0f, -0.1f);
                GameObject blind = Instantiate(blindBlockPrefab, pos, Quaternion.identity, blockRoot);
                blind.tag = "Blind";
            }
            // ブラインド処理（blind判定）
            if (!isRowBlinded[y] && filledInRow[y] >= blindH && blindH > 0)
            {
                Debug.Log($"Debug: fill{filledInRow[y]} y{y} blindH{blindH}");
                ApplyBlindToRow(y);
                isRowBlinded[y] = true;
            }

            // ブラインド処理（すでにblindされている行）
            if (isRowBlinded[y])
            {
                Vector3 pos = gridPositions[y, x] + new Vector3(0f, 0f, -0.1f);
                GameObject blind = Instantiate(blindBlockPrefab, pos, Quaternion.identity, blockRoot);
                blind.tag = "Blind";
            }
        }
        isInputLocked = false;
        StartTurn();
    }

    /* 勝利判定 */
    private bool CheckWin(int playerId, int x, int y, out List<Vector2Int> winPositions)
    {
        winPositions = new List<Vector2Int>();

        // 横, 縦, 斜め↘, 斜め↗ すべてチェック
        if (CheckDirectionWithPositions(playerId, x, y, 1, 0, out winPositions) ||
            CheckDirectionWithPositions(playerId, x, y, 0, 1, out winPositions) ||
            CheckDirectionWithPositions(playerId, x, y, 1, 1, out winPositions) ||
            CheckDirectionWithPositions(playerId, x, y, 1, -1, out winPositions))
        {
            return true;
        }

        return false;
    }

    private bool CheckDirection(int playerId, int x, int y, int dx, int dy)
    {
        int count = 1;

        // 正方向
        count += CountInDirection(playerId, x, y, dx, dy);
        // 逆方向
        count += CountInDirection(playerId, x, y, -dx, -dy);

        return count >= 4;
    }
    private int CountInDirection(int playerId, int startX, int startY, int dx, int dy)
    {
        int count = 0;
        int x = startX + dx;
        int y = startY + dy;

        while (x >= 0 && x < Width && y >= 0 && y < Height && gridState[y, x] == playerId)
        {
            count++;
            x += dx;
            y += dy;
        }

        return count;
    }

    private bool CheckDirectionWithPositions(int playerId, int x, int y, int dx, int dy, out List<Vector2Int> positions)
    {
        positions = new List<Vector2Int> { new Vector2Int(x, y) };

        int count = 1;

        // 正方向
        int nx = x + dx;
        int ny = y + dy;
        while (IsInside(nx, ny) && gridState[ny, nx] == playerId)
        {
            positions.Add(new Vector2Int(nx, ny));
            count++;
            nx += dx;
            ny += dy;
        }

        // 逆方向
        nx = x - dx;
        ny = y - dy;
        while (IsInside(nx, ny) && gridState[ny, nx] == playerId)
        {
            positions.Add(new Vector2Int(nx, ny));
            count++;
            nx -= dx;
            ny -= dy;
        }

        return count >= 4;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }


    /* 制限時間 */
    public void SetMaxTurnTime(int time)
    {
        maxTurnTime = (time <= 0) ? 0 : time; // OFFのとき非常に長い時間にする
    }

    private void ForcePassTurn()
    {
        Debug.Log($"Player {currentPlayer} time out! Lose.{turnTimer}     {maxTurnTime}");

        int loser = currentPlayer;
        int winner = 3 - currentPlayer;

        // 演出（勝者・敗者）
        if (winner == 1)
        {
            executionerP1.PlayWin();
            executionerP2.PlayDie();
        }
        else
        {
            executionerP2.PlayWin();
            executionerP1.PlayDie();
        }

        isTiming = false;
        isInputLocked = true;

        gameFlowManager.OnPlayerWin(winner);
    }

    private IEnumerator StartTurnWithDelay()
    {

        yield return null; // 1フレーム待つ

        // 🔽 ここで明示的に再セットする
        isTiming = false;
        turnTimer = maxTurnTime;
        isTiming = true;
    }


    public void SetBlindVertical(int count) // TitleUIがblind数を設定するための関数
    {
        blindV = count;
    }

    public void SetBlindHorizontal(int count) // TitleUIがblind数を設定するための関数
    {
        blindH = count;
    }

    private void ApplyBlindToColumn(int col)
    {
        for (int y = 0; y < Height; y++)
        {
            if (gridState[y, col] != 0) // ブロックがある場所だけ
            {
                Vector3 pos = gridPositions[y, col] + new Vector3(0f, 0f, -0.1f);
                GameObject blind = Instantiate(blindBlockPrefab, pos, Quaternion.identity, blockRoot);
                blind.tag = "Blind"; // 後で削除用
            }
        }
    }

    private void ApplyBlindToRow(int row)
    {
        for (int x = 0; x < Width; x++)
        {
            if (gridState[row, x] != 0)
            {
                Vector3 pos = gridPositions[row, x] + new Vector3(0f, 0f, -0.1f);
                GameObject blind = Instantiate(blindBlockPrefab, pos, Quaternion.identity, blockRoot);
                blind.tag = "Blind";
            }
        }
    }

    public void StartInvert()
    {
        if (isInputLocked) return;
        if (!isInvertEnabled) return; 
        StartCoroutine(PerformInvert());
    }


    private IEnumerator PerformInvert()
    {
        isInputLocked = true;
        isTiming = false;

        int[,] newGridState = new int[Height, Width];
        Destroy(currentIndicator);

        if (currentPlayer == 1)
            executionerP1.PlayAttack();
        else
            executionerP2.PlayAttack();

        gameFlowManager.PlaySE(SEType.Invert);

        // Step1: すべてのブロック（Blind含む）を1つずつ上昇
        List<Transform> allBlocks = new List<Transform>();
        foreach (Transform child in blockRoot)
        {
            allBlocks.Add(child); // Blind も含めて全部対象にする
        }

        // 順番に少しずつずらして上昇させる
        for (int i = 0; i < allBlocks.Count; i++)
        {
            Transform block = allBlocks[i];
            Vector3 up = block.position + Vector3.up * invertRiseHeight;

            float delay = i * 0.05f;
            StartCoroutine(MoveBlockWithDelay(block, up, 0.3f, delay));
        }

        yield return new WaitForSeconds(0.05f * allBlocks.Count + 0.3f); // 全部上がるまで待つ

        // Step2: gridState を反転（中身だけ）
        for (int x = 0; x < Width; x++)
        {
            int height = filledInColumn[x];
            for (int y = 0; y < height; y++)
            {
                newGridState[y, x] = gridState[height - 1 - y, x];
            }
        }
        gridState = newGridState;
        for (int x = 0; x < Width; x++)// デバッグ
        {
            string column = $"Column {x}: ";
            for (int y = 0; y < Height; y++)
            {
                column += newGridState[y, x].ToString();
            }
            Debug.Log(column);
        }

        // Step3: gridState の順番に従って、Transform を配置し直す
        // Step3: gridState に従ってブロックを再配置（上から順に落下）
        List<Transform> p1Blocks = new List<Transform>();
        List<Transform> p2Blocks = new List<Transform>();

        // プレイヤーごとに分類（BlockController が playerId を持っていると仮定）
        foreach (Transform b in blockRoot)
        {
            if (b.CompareTag("Blind")) continue;

            var ctrl = b.GetComponent<BlockController>();
            if (ctrl == null) continue;

            if (ctrl.playerId == 1) p1Blocks.Add(b);
            else if (ctrl.playerId == 2) p2Blocks.Add(b);
        }

        // 上から順に再配置（見た目は「落ちてくる」）
        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                int state = gridState[y, x];

                if (state == 1 && p1Blocks.Count > 0)
                {
                    Transform b = p1Blocks[0];
                    p1Blocks.RemoveAt(0);
                    b.GetComponent<BlockController>().MoveTo(gridPositions[y, x], 0.3f);
                }
                else if (state == 2 && p2Blocks.Count > 0)
                {
                    Transform b = p2Blocks[0];
                    p2Blocks.RemoveAt(0);
                    b.GetComponent<BlockController>().MoveTo(gridPositions[y, x], 0.3f);
                }
            }
        }

                // Step3.5: BlindBlock も元の位置に戻す
        foreach (Transform b in blockRoot)
        {
            if (!b.CompareTag("Blind")) continue;

            Vector3 originalPos = b.position + Vector3.down * invertRiseHeight;
            b.GetComponent<BlockController>().MoveTo(originalPos, 0.3f);
        }




        yield return new WaitForSeconds(0.3f);


        // Step5: 勝利判定
        bool p1Wins = CheckAllWin(1, out List<Vector2Int> winP1);
        bool p2Wins = CheckAllWin(2, out List<Vector2Int> winP2);

        if (p1Wins && p2Wins)
        {
            int winner = 3 - currentPlayer;
            List<Vector2Int> win = (winner == 1) ? winP1 : winP2;

            ShowWinBlocks(win);

            if (winner == 1)
            {
                executionerP1.PlayWin();
                executionerP2.PlayDie();
            }
            else
            {
                executionerP2.PlayWin();
                executionerP1.PlayDie();
            }

            gameFlowManager.OnPlayerWin(winner);
            yield break;
        }
        else if (p1Wins)
        {
            ShowWinBlocks(winP1);
            executionerP1.PlayWin();
            executionerP2.PlayDie();
            gameFlowManager.OnPlayerWin(1);
            yield break;
        }
        else if (p2Wins)
        {
            ShowWinBlocks(winP2);
            executionerP2.PlayWin();
            executionerP1.PlayDie();
            gameFlowManager.OnPlayerWin(2);
            yield break;
        }


        isInputLocked = false;
        // Step4: ターン交代 & インジケータ再生成  
        StartTurn();
    }

    private IEnumerator MoveBlockWithDelay(Transform block, Vector3 targetPos, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 🔒 ここでnullチェック（MissingReferenceにも対応）
        if (block == null || block.Equals(null)) yield break;

        var controller = block.GetComponent<BlockController>();
        if (controller != null)
        {
            controller.MoveTo(targetPos, duration);
        }
    }



    public bool IsPlayerTurn(int playerId)
    {
        return currentPlayer == playerId;
    }
    private bool CheckAllWin(int playerId, out List<Vector2Int> winPosList)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (gridState[y, x] == playerId)
                {
                    if (CheckWin(playerId, x, y, out winPosList))
                        return true;
                }
            }
        }
        winPosList = null;
        return false;
    }

    private void ShowWinBlocks(List<Vector2Int> winPositions)
    {
        foreach (Vector2Int pos in winPositions)
        {
            Vector3 worldPos = gridPositions[pos.y, pos.x] + new Vector3(0f, 0f, -0.2f);
            Instantiate(winBlockPrefab, worldPos, Quaternion.identity, blockRoot);
        }
    }



    public void SetInvertEnabled(bool enabled)
    {
        isInvertEnabled = enabled;
    }

}


