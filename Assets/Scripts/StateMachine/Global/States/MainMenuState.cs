using StateMachine.Base;
using UI.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace StateMachine.Global.States
{
    public class MainMenuState : State
    {
        private const string SceneName = "Main Menu Scene";

        private readonly GlobalStateMachine _stateMachine;
        private readonly MainMenuMediator _mainMenuMediator;

        public MainMenuState(GlobalStateMachine stateMachine, MainMenuMediator mainMenuMediator)
        {
            _stateMachine = stateMachine;
            _mainMenuMediator = mainMenuMediator;
        }

        public override void Enter()
        {
            Debug.Log("enter main menu state");
            Subscribe();
            SceneManager.LoadScene(SceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName)
            {
                _mainMenuMediator.Construct();
                
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }
        
        private void OnHostSelected()
        {
            _stateMachine.ChangeState<GameplayerState, GameplayPayload>(new GameplayPayload(true));
        }

        private void OnClientSelected()
        {
            _stateMachine.ChangeState<GameplayerState, GameplayPayload>(new GameplayPayload(false));
        }

        private void Subscribe()
        {
            Application.quitting += Exit;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _mainMenuMediator.HostSelected += OnHostSelected;
            _mainMenuMediator.ClientSelected += OnClientSelected;
        }

        private void Unsubscribe()
        {
            Application.quitting -= Exit;
            _mainMenuMediator.HostSelected -= OnHostSelected;
            _mainMenuMediator.ClientSelected -= OnClientSelected;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public override void Exit()
        {
            Unsubscribe();
            Debug.Log("exit main menu state");
        }
        
        public class Factory : PlaceholderFactory<GlobalStateMachine, MainMenuState> { }
    }
}