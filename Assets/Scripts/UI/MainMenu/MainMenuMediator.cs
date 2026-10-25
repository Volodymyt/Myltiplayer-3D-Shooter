using System;
using Services;
using UnityEngine;

namespace UI.MainMenu
{
    public class MainMenuMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private readonly MenuMessageService _menuMessages;
        private Lobby _lobby;

        public event Action HostSelected;
        public event Action<string> ClientSelected;

        public MainMenuMediator(GenericFactory genericFactory, MenuMessageService menuMessages)
        {
            _genericFactory = genericFactory;
            _menuMessages = menuMessages;
        }

        public void Construct()
        {
            _lobby = _genericFactory.Create<Lobby>(Constants.LobbyPath);

            _lobby.OnHostAddRequest += OnHost;
            _lobby.OnClientAddRequest += OnClient;

            ShowPendingMessage();
        }

        private void ShowPendingMessage()
        {
            var message = _menuMessages.Take();
            var statusText = _lobby.errorMessage;

            if (statusText != null)
            {
                statusText.Show(message);
                return;
            }

            if (!string.IsNullOrEmpty(message))
                Debug.LogWarning($"[Menu] MenuStatusText not found in the scene, message: {message}");
        }

        private void OnHost() => HostSelected?.Invoke();
        private void OnClient(string code) => ClientSelected?.Invoke(code);

        public void Dispose()
        {
            if (_lobby == null)
                return;

            _lobby.OnHostAddRequest -= OnHost;
            _lobby.OnClientAddRequest -= OnClient;
        }
    }
}
