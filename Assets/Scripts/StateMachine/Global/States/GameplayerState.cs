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

        private bool _isHost;
        
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

            Debug.Log($"Enter GameplayerState | IsHost = {_isHost}");
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

                _gameplayMediator.StartNetwork(_isHost);

                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void Subscribe()
        {
            Application.quitting += Exit;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Unsubscribe()
        {
            Application.quitting -= Exit;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public override void Exit()
        {
            _uiGameMediator.Dispose();
            _gameplayMediator.Dispose();
            Unsubscribe();
            
            _inputService.Dispose();
            Debug.Log("exit application");
        }

        public class Factory : PlaceholderFactory<GlobalStateMachine, GameplayerState> { }
    }
}