using UnityEngine;
using System.Collections;

public class ExecutionerController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool isRightSide = false;
    private Vector3 initialPosition = new Vector3(-10f, 2f, 0f); // 初期位置（左側想定）
    private Vector3 entryTarget = new Vector3(-5f, 2f, 0f);      // 登場位置
    private float entryDuration = 1.0f;

    private bool isEntering = false;

    void Awake()
    {
        if (isRightSide)
        {
            initialPosition = new Vector3(10f, 2f, 0f);
            entryTarget = new Vector3(5f, 2f, 0f);
        }
        else
        {
            initialPosition = new Vector3(-10f, 2f, 0f);
            entryTarget = new Vector3(-5f, 2f, 0f);
        }
        ResetPosition();
    }

    public void ResetPosition()
    {
        gameObject.SetActive(true); 
        transform.position = initialPosition;
        animator.Play("idle1_1", 0, 0f);
    }

    public void PlayEntry(System.Action onEntryComplete = null)
    {
        isEntering = true;
        StartCoroutine(MoveToPosition(entryTarget, entryDuration, () =>
        {
            //animator.SetTrigger("Idle");
            isEntering = false;
            onEntryComplete?.Invoke(); // ゲーム開始通知
        }));
    }

    public void PlayAttack()
    {
        if (!isEntering)
            animator.SetTrigger("Attack");
    }

    public void PlayDie()
    {
        StartCoroutine(DieAndDisappear());
    }

    private IEnumerator DieAndDisappear()
    {
        animator.SetTrigger("Die");

        // 現在のアニメーションの長さを取得して待機
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float waitTime = stateInfo.length;

        // ただし遷移に遅れがあるため少し待って再取得
        yield return new WaitForSeconds(0.1f);
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        waitTime = stateInfo.length;

        yield return new WaitForSeconds(waitTime);
        gameObject.SetActive(false);
    }
    public void PlayWin()
    {
        animator.SetTrigger("Win");
    }

    private IEnumerator MoveToPosition(Vector3 target, float duration, System.Action onComplete)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        onComplete?.Invoke();
    }
}
