using StateMachine.Base;
using StateMachine.Global;
using States;
using UnityEngine;
using Zenject;

namespace Bootstrap
{
    public class Bootstrapper : MonoBehaviour
    {
        private StateMachineBase _globalStateMachine;

        [Inject]
        public void Construct(GlobalStateMachine globalStateMachine)
        {
            _globalStateMachine = globalStateMachine;
        }

        private void Start()
        {
            _globalStateMachine.ChangeState<BootState>();
            DontDestroyOnLoad(this);
        }
    }
}