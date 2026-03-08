using UnityEngine;
using TMPro;
using System.Collections;

public class UIFxManager : MonoBehaviour
{
    public static UIFxManager Instance;

    [SerializeField] private Transform scoreTarget; // UI金币位置
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject floatingTextPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayScoreFx(Vector3 worldPos, int score)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        StartCoroutine(CoinFly(screenPos, score));
        StartCoroutine(FloatingText(screenPos, score));
    }

    private IEnumerator CoinFly(Vector3 start, int score)
    {
        GameObject coin = Instantiate(coinPrefab, start, Quaternion.identity, transform);

        Vector3 target = scoreTarget.position;

        float duration = 0.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            coin.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        Destroy(coin);
    }

    private IEnumerator FloatingText(Vector3 start, int score)
    {
        GameObject textObj = Instantiate(floatingTextPrefab, start, Quaternion.identity, transform);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

        tmp.text = "+" + score;

        float duration = 0.8f;
        float timer = 0f;

        Vector3 end = start + new Vector3(0, 80f, 0);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            textObj.transform.position = Vector3.Lerp(start, end, t);

            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            tmp.color = c;

            yield return null;
        }

        Destroy(textObj);
    }
}