using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MoonSharp.Interpreter;
using Coroutine = UnityEngine.Coroutine;

public class ScreenLock : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform _gridTransform;
    [SerializeField] private RectTransform _canvasRectTransform;
    [SerializeField] private RectTransform _linesTransform;
    [SerializeField] private LockCircleElement _circleElement;
    [SerializeField] private int _circleElementsAmount;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _dateText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private List<int> _correctSequence;

    private readonly List<LockCircleElement> _circleElements = new List<LockCircleElement>();
    private readonly List<int> _currentLockSequence = new List<int>();
    private readonly List<RectTransform> _lines = new List<RectTransform>();

    private Coroutine _resultCoroutine;

    private void Awake()
    {
        for (var i = 0; i < _circleElementsAmount; ++i)
        {
            var element = Instantiate(_circleElement, _gridTransform);
            element.Index = i;
            _circleElements.Add(element);
        }
    }

    private void Update()
    {
        var ci = new CultureInfo("en-US");
        _timeText.text = DateTime.Now.ToString("HH:mm", ci);
        _dateText.text = DateTime.Now.ToString("dddd, dd MMMM", ci);
    }

    private void CheckIndex(int index)
    {
        if (_currentLockSequence.Count == 0 || _currentLockSequence[_currentLockSequence.Count - 1] != index)
        {
            if (_lines.Count > 0)
            {
                AdjustLastLine(_circleElements[index].RectTransform.anchoredPosition);
            }
            
            _currentLockSequence.Add(index);
            _lines.Add(CreateLineObject());
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _currentLockSequence.Clear();
        
        _lines.ForEach(line => Destroy(line.gameObject));
        _lines.Clear();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_lines.Count > 0)
        {
            Destroy(_lines[_lines.Count - 1].gameObject);
            _lines.RemoveAt(_lines.Count - 1);
        }
        
        if (_resultCoroutine != null)
        {
            StopCoroutine(_resultCoroutine);
        }
        _resultCoroutine = StartCoroutine(ResultCoroutine(CheckSequence()));
    }

    private bool CheckSequence()
    {
        const string scriptString = @"
            function check(sequence)
                if #rightSequence ~= #sequence then
                    return false
                end
                for i = 1, #rightSequence do
                    if rightSequence[i] ~= sequence[i] then
                        return false
                    end
                end
                return true
            end

            return check(currentSequence)
        ";
            
        var script = new Script();
        script.Globals["currentSequence"] = _currentLockSequence;
        script.Globals["rightSequence"] = _correctSequence;

        return script.DoString(scriptString).Boolean;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var pointerPosition = ScreenToCanvasPosition(eventData.position);
        
        if (_lines.Count > 0)
        {
            AdjustLastLine(pointerPosition);
        } 
        
        var circleInPointer = FindCircleInPointer(pointerPosition);
        if (circleInPointer != null)
        {
            CheckIndex(circleInPointer.Index);
        }
    }

    private void AdjustLastLine(Vector2 lineEndPosition)
    {
        var lastLine = _lines[_lines.Count - 1];
        lastLine.gameObject.SetActive(true);
        
        var lastCircle = _circleElements[_currentLockSequence[_currentLockSequence.Count - 1]];
        var lastCirclePosition = lastCircle.RectTransform.anchoredPosition;
        lastLine.anchoredPosition = (lineEndPosition + lastCirclePosition) / 2f;
        lastLine.sizeDelta = new Vector2(Vector2.Distance(lineEndPosition, lastCirclePosition), 5f);
        
        var deltaVector = lineEndPosition - lastCirclePosition;
        lastLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(deltaVector.y, deltaVector.x) * Mathf.Rad2Deg);
    }

    private LockCircleElement FindCircleInPointer(Vector2 position)
    {
        return _circleElements.FirstOrDefault(element =>
        {
            var elementRectTransform = element.GetComponent<RectTransform>();
            return Vector2.Distance(position, element.GetComponent<RectTransform>().anchoredPosition) < elementRectTransform.sizeDelta.x / 2f;
        });
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition) =>
        new Vector2(screenPosition.x / Screen.width * _canvasRectTransform.rect.width, -(1 - screenPosition.y / Screen.height) * _canvasRectTransform.rect.height);

    private IEnumerator ResultCoroutine(bool isCorrect)
    {
        var duration = 0.5f;
        var delay = 2f;
        var startColor = Color.clear;
        var endColor = isCorrect ? new Color(0f, 0.7f, 0f) : new Color(0.75f, 0f, 0f);
        
        _resultText.color = startColor;
        _resultText.text = isCorrect ? "Correct" : "Error";
        
        for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            _resultText.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        yield return new WaitForSeconds(delay);
        
        for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            _resultText.color = Color.Lerp(endColor, startColor, t);
            yield return null;
        }
    }

    private RectTransform CreateLineObject()
    {
        var line = new GameObject();
        var lineTransform = line.AddComponent<RectTransform>();
        
        lineTransform.anchorMin = lineTransform.anchorMax = Vector2.up;
        
        lineTransform.SetParent(_linesTransform);
        
        line.SetActive(false);
        
        line.AddComponent<Image>();
        
        return lineTransform;
    }
}