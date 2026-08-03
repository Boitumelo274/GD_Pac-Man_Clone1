using UnityEngine;
using System.Collections;

public class CountDownController : MonoBehaviour
{
    [Header("Number 3")]
    public GameObject threeBlack;
    public GameObject threeGray;

    [Header("Number 2")]
    public GameObject twoBlack;
    public GameObject twoGray;

    [Header("Number 1")]
    public GameObject oneBlack;
    public GameObject oneGray;

    private const float DELAY_TIME = 0.33f;

    public void StartCount()
    {
        StartCoroutine(LoadCountDown());
    }

    private IEnumerator LoadCountDown()
    {
        yield return StartCoroutine(DisplayNumber(threeBlack, threeGray));
        yield return StartCoroutine(DisplayNumber(twoBlack, twoGray));
        yield return StartCoroutine(DisplayNumber(oneBlack, oneGray));
    }

    private IEnumerator DisplayNumber(GameObject blackNumber, GameObject grayNumber)
    {
        blackNumber.SetActive(true);
        grayNumber.SetActive(false);
        yield return new WaitForSecondsRealtime(DELAY_TIME);

        blackNumber.SetActive(false);
        grayNumber.SetActive(true);
        yield return new WaitForSecondsRealtime(DELAY_TIME);

        blackNumber.SetActive(true);
        grayNumber.SetActive(false);
        yield return new WaitForSecondsRealtime(DELAY_TIME);

        blackNumber.SetActive(false);
    }
}