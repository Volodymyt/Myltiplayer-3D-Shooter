using StateMachine.Base;
using StateMachine.Global.States;

namespace StateMachine.Global
{
    public class GlobalStateMachine : StateMachineBase
    {
        private GlobalStateMachine(BootState.Factory bootStateFactory, GameplayerState.Factory mainStateFactory, MainMenuState.Factory mainMenuStateFactory)
        {
            Add(bootStateFactory.Create(this));
            Add(mainMenuStateFactory.Create(this));
            Add(mainStateFactory.Create(this));
        }
    }
}