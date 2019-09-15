using UnityEngine;

public class LockCircleElement : MonoBehaviour
{
    public int Index { get; set; }

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    private RectTransform _rectTransform;
}
