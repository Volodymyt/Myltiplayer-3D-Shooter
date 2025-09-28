using System;
using System.Collections.Generic;
using UnityEngine;

namespace StateMachine.Base
{
    public class StateMachineBase
    {
        private StateBase _currentState;
        private readonly Dictionary<Type, StateBase> _states = new();

        protected void Add<TState>(TState state) where TState : StateBase
        {
            if (_states.ContainsKey(typeof(TState)))
            {
                Debug.LogError($"State Machine already contains {state}");
                return;
            }

            _states.Add(typeof(TState), state);
        }

        public void ChangeState<TState>() where TState : State
        {
            _currentState?.Exit();
            _currentState = _states[typeof(TState)];

            if (_currentState is State state)
                state.Enter();
            else
                Debug.LogError($"Unable to enter {_currentState}");
        }

        public void ChangeState<TState, TPayload>(TPayload payload) where TState : StateWithPayload<TPayload> where TPayload : PayloadBase
        {
            _currentState?.Exit();
            _currentState = _states[typeof(TState)];

            if (_currentState is StateWithPayload<TPayload> stateWithPayload)
                stateWithPayload.Enter(payload);
            else
                Debug.LogError($"Unable to enter {_currentState} with payload");
        }


        public StateBase GetCurrentState<TState>() where TState : StateBase => _currentState;

        public void ExitStateMachine()
        {
            _currentState?.Exit();
            _currentState = null;
        }
    }
}