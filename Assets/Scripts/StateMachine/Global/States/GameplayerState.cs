using System.Threading.Tasks;
using Gameplay;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using StateMachine.Base;
using UI;

namespace StateMachine.Global.States
{
    public class GameplayerState : StateWithPayload<GameplayPayload>
    {
        private const string SceneName = "Game Scene";
        
        private readonly StateMachineBase _stateMachine;
        private readonly InputService _inputService;
        private readonly UIGameMediator _uiGameMediator;
        private readonly GameplayMediator _gameplayMediator;

        private GameplayPayload _payload;
        
        private bool _isHost;
        private string _joinCode;
        private bool _quitConfirmed;
        
        public GameplayerState(
            StateMachineBase stateMachine, 
            InputService inputService,
            UIGameMediator uiGameMediator,
            GameplayMediator gameplayMediator) : base(stateMachine)
        {
            _stateMachine = stateMachine;
            _inputService = inputService;
            _uiGameMediator = uiGameMediator;
            _gameplayMediator = gameplayMediator;
        }

        public override void Enter(GameplayPayload payload)
        {
            _isHost = payload.IsHost;
            _joinCode = payload.JoinCode;

            Subscribe();
            SceneManager.LoadScene(SceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName)
            {
                _uiGameMediator.Construct();
                _gameplayMediator.Construct();
                _inputService.Construct();

                _ = _gameplayMediator.StartNetworkAsync(_isHost, _joinCode);

                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void Subscribe()
        {
            Application.wantsToQuit += HandleWantsToQuit;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Unsubscribe()
        {
            Application.wantsToQuit -= HandleWantsToQuit;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private bool HandleWantsToQuit()
        {
            if (_quitConfirmed)
                return true;

            _ = QuitAfterCleanupAsync();
            return false;
        }

        private async Task QuitAfterCleanupAsync()
        {
            var cleanupTask = _gameplayMediator.CleanupBeforeQuitAsync();
            var timeoutTask = Task.Delay(3000);
            await Task.WhenAny(cleanupTask, timeoutTask);

            Exit();
            _quitConfirmed = true;
            Application.Quit();
        }

        public override void Exit()
        {
            _uiGameMediator.Dispose();
            _gameplayMediator.Dispose();
            _inputService.Dispose();
            
            Unsubscribe();
            
            Debug.Log("exit application");
        }

        public class Factory : PlaceholderFactory<GlobalStateMachine, GameplayerState> { }
    }
}