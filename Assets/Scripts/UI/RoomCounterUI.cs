using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Keeps the attached TextMeshProUGUI in sync with the number of rooms the
/// player has cleared in the current run (<see cref="RunManager.CurrentRoomIndex"/>).
/// Attach directly to the text GameObject so no serialized reference is needed.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class RoomCounterUI : MonoBehaviour
{
    private TextMeshProUGUI _label;
    private int _lastRoomIndex = -1;

    public GameObject RoomClearedText;

    private void Awake()
    {
        RoomClearedText.SetActive(false);
        _label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (RunManager.Instance == null) return;

        int current = RunManager.Instance.CurrentRoomIndex;
        if (current == _lastRoomIndex) return;

        if (current != _lastRoomIndex && current != 0)
        {
            StartCoroutine(ShowRoomClearedText());
        }

        _lastRoomIndex = current;
        _label.SetText(current.ToString());
    }

    IEnumerator ShowRoomClearedText()
    {
        RoomClearedText.SetActive(true);
        yield return new WaitForSeconds(1f);
        RoomClearedText.SetActive(false);
    }
}
