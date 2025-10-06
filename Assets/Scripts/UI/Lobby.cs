using System;
using UnityEngine;

namespace UI
{
    public class Lobby : MonoBehaviour
    {
        public event Action OnHostAddRequest;
        
        public event Action OnClientAddRequest;
        
        public void OnHostButton()
        {
            OnHostAddRequest?.Invoke();
        }

        public void OnClientButton()
        {
            OnClientAddRequest?.Invoke();
        }
    }
}