using TMPro;
using UnityEngine;

namespace UI.MainMenu
{
    [RequireComponent(typeof(TMP_Text))]
    public class ErrorMessageText : MonoBehaviour
    {
        private TMP_Text _text;

        public void Show(string message)
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();

            if (string.IsNullOrEmpty(message))
            {
                _text.text = string.Empty;
                return;
            }

            _text.text = message;
            gameObject.SetActive(true);
        }
    }
}
