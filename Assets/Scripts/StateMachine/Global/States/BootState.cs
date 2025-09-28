using UnityEngine;
using Zenject;
using StateMachine.Base;
using StateMachine.Global;
using StateMachine.Global.States;

namespace States
{
    public class BootState : State
    {
        private readonly StateMachineBase _stateMachine;

        public BootState(StateMachineBase stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public override void Enter()
        {
            Debug.Log("enter boot state");

            _stateMachine.ChangeState<MainState>();
        }

        public override void Exit()
        {
            Debug.Log("exit boot state");
        }

        public class Factory : PlaceholderFactory<GlobalStateMachine, BootState> { }
    }
}