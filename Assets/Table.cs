using System.Collections;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

public class Table : MonoBehaviour
{
    public TutorialSlideUIQueue TutorialSlideUIQueue;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pickable") && TutorialSlideUIQueue)
        {
            TutorialSlideUIQueue.CompleteCurrentByIndex(3);
            StartCoroutine(Next());
        }
    }
    IEnumerator Next()
    {
        yield return new WaitForSeconds(0.3f);
        TutorialSlideUIQueue.EnqueueTutorialByIndex(4);
    }


}
