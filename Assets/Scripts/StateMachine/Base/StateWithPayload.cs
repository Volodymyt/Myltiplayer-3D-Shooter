namespace StateMachine.Base
{
    public abstract class StateWithPayload<TPayload> : StateBase where TPayload : PayloadBase
    {
        protected readonly StateMachineBase StateMachine;

        public StateWithPayload(StateMachineBase stateMachine)
        {
            StateMachine = stateMachine;
        }

        public abstract void Enter(TPayload payload);
    }
}