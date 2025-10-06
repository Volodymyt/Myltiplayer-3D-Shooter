using Gameplay;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using StateMachine.Base;
using UI;
using Zenject.SpaceFighter;

namespace StateMachine.Global.States
{
    public class MainState : State
    {
        private const string SceneName = "Game Scene";
        
        private readonly StateMachineBase _stateMachine;
        private readonly InputService _inputService;
        private readonly UIGameMediator _uiGameMediator;

        public MainState(
            StateMachineBase stateMachine, 
            InputService inputService,
            UIGameMediator uiGameMediator)
        {
            _stateMachine = stateMachine;
            _inputService = inputService;
            _uiGameMediator = uiGameMediator;
        }

        public override void Enter()
        {
            Debug.Log("enter main state");
            Subscribe();
            SceneManager.LoadScene(SceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName)
            {
                _uiGameMediator.Construct();
                _inputService.Construct();
                
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
            Unsubscribe();
            
            _inputService.Dispose();
            Debug.Log("exit application");
        }

        public class Factory : PlaceholderFactory<GlobalStateMachine, MainState> { }
    }
}