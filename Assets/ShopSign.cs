using UnityEngine;
using System.Collections;

public class ShopSign : MonoBehaviour
{
    public GameManager gameManager;
    bool isOpen = false;
    Coroutine rotateCo;

    void Start()
    {
        isOpen = gameManager.shopIsOpen;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3f) && hit.collider.CompareTag("sign"))
            {
                Debug.Log("press");
                isOpen = !isOpen;
                gameManager.shopIsOpen = isOpen;
                UpdateSign();
            }
        }
    }

    void UpdateSign()
    {
        if (rotateCo != null) StopCoroutine(rotateCo);
        rotateCo = StartCoroutine(RotateThroughPoints());
    }

    IEnumerator RotateThroughPoints()
    {
        Vector3[] points = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 190, 0)
        };

        float stepDuration = 0.3f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Quaternion start = Quaternion.Euler(points[i]);
            Quaternion end = Quaternion.Euler(points[i + 1]);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / stepDuration;
                transform.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }
        }

        transform.localRotation = Quaternion.Euler(points[points.Length - 1]);
    }
}
