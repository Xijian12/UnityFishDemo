using System.Collections;
using UnityEngine;

public class CoinUIFx : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 targetPos;
    private float speed = 1500f; // 提高速度

    public void Init(Vector2 startPos, Vector2 targetPos)
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = startPos;
        this.targetPos = targetPos;

        StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        while (Vector2.Distance(rectTransform.anchoredPosition, targetPos) > 10f)
        {
            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition,
                targetPos,
                speed * Time.deltaTime
            );

            yield return null;
        }

        Destroy(gameObject);
    }
}