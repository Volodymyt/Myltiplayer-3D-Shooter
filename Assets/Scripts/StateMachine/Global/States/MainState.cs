using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using StateMachine.Base;

namespace StateMachine.Global.States
{
    public class MainState : State
    {
        private StateMachineBase _stateMachine;

        public MainState(StateMachineBase stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            Debug.Log("enter main state");
            SceneManager.LoadScene(1);
            Subscribe();
        }

        private void Subscribe()
        {
            Application.quitting += Exit;
        }

        private void Unsubscribe()
        {
            Application.quitting -= Exit;
        }

        public override void Exit()
        {
            Unsubscribe();
            Debug.Log("exit application");
        }

        public class Factory : PlaceholderFactory<GlobalStateMachine, MainState> { }
    }
}