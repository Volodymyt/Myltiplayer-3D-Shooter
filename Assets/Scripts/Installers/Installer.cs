using Gameplay;
using Services;
using StateMachine.Global;
using StateMachine.Global.States;
using UI;
using UI.MainMenu;
using Zenject;

namespace Installers
{
    public class Installer : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindGlobalStateMachine();
            BindMainMenu();
            BindGameplay();
            BindUI();
            BindServices();
        }

        private void BindGlobalStateMachine()
        {
            Container.Bind<GlobalStateMachine>().AsSingle();
            Container.BindFactory<GlobalStateMachine, BootState, BootState.Factory>().AsSingle();
            Container.BindFactory<GlobalStateMachine, GameplayerState, GameplayerState.Factory>().AsSingle();
            Container.BindFactory<GlobalStateMachine, MainMenuState, MainMenuState.Factory>().AsSingle();
        }

        private void BindGameplay()
        {
            Container.Bind<NetworkManagerFactory>().AsSingle();
            Container.Bind<GameplayMediator>().AsSingle();
        }

        private void BindUI()
        {
            Container.Bind<UIGameMediator>().AsSingle();
            Container.Bind<LobbyFactory>().AsSingle();
        }

        private void BindMainMenu()
        {
            Container.Bind<MainMenuMediator>().AsSingle();
        }

        private void BindServices()
        {
            Container.Bind<PlayerInputActions>().AsSingle().NonLazy();
            Container.Bind<InputService>().AsSingle();
            
            Container.BindInterfacesAndSelfTo<PlayerMovement>().AsSingle();
            
            Container.Bind<IAssetProviderService>().To<AssetProviderService>().AsSingle();
        }
    }
}