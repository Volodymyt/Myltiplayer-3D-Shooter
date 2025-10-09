using System;

namespace UI.MainMenu
{
    public class MainMenuMediator : IDisposable
    {
        private readonly LobbyFactory _lobbyFactory;
        private Lobby _lobby;

        public event Action HostSelected;
        public event Action ClientSelected;

        public MainMenuMediator(LobbyFactory lobbyFactory)
        {
            _lobbyFactory = lobbyFactory;
        }

        public void Construct()
        {
            _lobby = _lobbyFactory.CreateLobby();

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