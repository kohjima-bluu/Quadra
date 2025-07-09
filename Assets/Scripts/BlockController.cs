using UnityEngine;
using System;
using System.Collections;


public class BlockController : MonoBehaviour
{
    private Action onFallComplete;
    public int playerId;

    public void StartFall(Vector3 targetPosition, float duration, Action onComplete)
    {
        onFallComplete = onComplete;
        StartCoroutine(FallCoroutine(targetPosition, duration));
    }

    private System.Collections.IEnumerator FallCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        onFallComplete?.Invoke();
    }

    public void MoveTo(Vector3 target, float duration)
    {
        StartCoroutine(MoveRoutine(target, duration));
    }

    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, timer / duration);
            yield return null;
        }
        transform.position = target;
    }

}
