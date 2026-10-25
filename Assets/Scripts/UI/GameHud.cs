using TMPro;
using UnityEngine;

namespace UI
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private GameObject roomCodePanel;
        [SerializeField] private TMP_Text roomCodeText;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                OnCopyCodeButton();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public void ShowRoomCode(string code)
        {
            if (roomCodePanel != null)
                roomCodePanel.SetActive(true);

            if (roomCodeText != null)
                roomCodeText.text = code;
        }

        public void OnCopyCodeButton()
        {
            if (roomCodeText != null)
                GUIUtility.systemCopyBuffer = roomCodeText.text;
        }

        public void Hide()
        {
            if (roomCodePanel != null)
                roomCodePanel.SetActive(false);
        }
    }
}