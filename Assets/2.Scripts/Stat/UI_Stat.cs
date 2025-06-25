using TMPro;
using UnityEngine;
using System.Collections; // Required for Coroutines

public class UI_Stat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldTextUI;
    public PlayerStatManager Manager;

    private int _currentDisplayedGold = 0;
    [SerializeField] private float _animationDuration = 0.5f; 

    private Coroutine _goldAnimationCoroutine; 

    private void Start()
    {
        _currentDisplayedGold = Manager.MyStat.Gold;
        _goldTextUI.text = _currentDisplayedGold.ToString();

        Manager.OnDataChanged += Refresh;
    }

    private void Refresh()
    {
        if (_goldAnimationCoroutine != null)
        {
            StopCoroutine(_goldAnimationCoroutine);
        }

        _goldAnimationCoroutine = StartCoroutine(AnimateGoldCount(Manager.MyStat.Gold));
    }

    private IEnumerator AnimateGoldCount(int targetGold)
    {
        int startGold = _currentDisplayedGold;
        float startTime = Time.time;
        float endTime = startTime + _animationDuration;

        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / _animationDuration;
            t = t * t * (3f - 2f * t);

            int animatedValue = (int)Mathf.Lerp(startGold, targetGold, t);
            _goldTextUI.text = animatedValue.ToString();
            _currentDisplayedGold = animatedValue; 

            yield return null; 
        }

        _goldTextUI.text = targetGold.ToString();
        _currentDisplayedGold = targetGold;
    }

    private void OnDestroy()
    {
        if (Manager != null)
        {
            Manager.OnDataChanged -= Refresh;
        }
    }
}