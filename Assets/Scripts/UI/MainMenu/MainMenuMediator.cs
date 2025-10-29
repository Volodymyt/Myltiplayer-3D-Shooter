using System;
using Services;

namespace UI.MainMenu
{
    public class MainMenuMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private Lobby _lobby;

        public event Action HostSelected;
        public event Action ClientSelected;

        public MainMenuMediator(GenericFactory genericFactory)
        {
            _genericFactory = genericFactory;
        }

        public void Construct()
        {
            _lobby = _genericFactory.Create<Lobby>(Constants.LobbyPath);

            _lobby.OnHostAddRequest += OnHost;
            _lobby.OnClientAddRequest += OnClient;
        }

        private void OnHost() => HostSelected?.Invoke();
        private void OnClient() => ClientSelected?.Invoke();

        public void Dispose()
        {
            _lobby.OnHostAddRequest -= OnHost;
            _lobby.OnClientAddRequest -= OnClient;
        }
    }
}