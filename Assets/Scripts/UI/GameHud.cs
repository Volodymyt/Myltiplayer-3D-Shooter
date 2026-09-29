using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // або TMPro, якщо у вас TextMeshPro

namespace UI
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private GameObject roomCodePanel;
        [SerializeField] private TMP_Text roomCodeText; // замініть на TMP_Text, якщо TextMeshPro

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                OnCopyCodeButton();
            }
        }

        public void ShowRoomCode(string code)
        {
            if (roomCodePanel != null)
                roomCodePanel.SetActive(true);

            if (roomCodeText != null)
                roomCodeText.text = $"Код кімнати: {code}";
        }

        public void OnCopyCodeButton()
        {
            if (roomCodeText != null)
                GUIUtility.systemCopyBuffer = roomCodeText.text.Replace("Код кімнати: ", "");
        }

        public void Hide()
        {
            if (roomCodePanel != null)
                roomCodePanel.SetActive(false);
        }
    }
}