using System;
using TMPro;
using UI.MainMenu;
using UnityEngine;

namespace UI
{
    public class Lobby : MonoBehaviour
    {
        public ErrorMessageText errorMessage;
        
        public event Action OnHostAddRequest;
        public event Action<string> OnClientAddRequest;

        [SerializeField] private TMP_InputField joinCodeInput; 

        public void OnHostButton()
        {
            OnHostAddRequest?.Invoke();
        }

        public void OnClientButton()
        {
            var code = joinCodeInput != null ? joinCodeInput.text : string.Empty;
            OnClientAddRequest?.Invoke(code);
        }
    }
}