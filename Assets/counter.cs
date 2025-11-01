using System.Collections;
using UnityEngine;

public class counter : MonoBehaviour
{
    IEnumerator Start()
    {

        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut(1.5f));
        Time.timeScale = 1f;
    }
}
