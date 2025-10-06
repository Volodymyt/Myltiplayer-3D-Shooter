using Gameplay;
using Services;
using StateMachine.Global;
using StateMachine.Global.States;
using States;
using UI;
using Zenject;

namespace Installers
{
    public class Installer : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindGlobalStateMachine();
            BindGameplay();
            BindUI();
            BindServices();
        }

        private void BindGlobalStateMachine()
        {
            Container.Bind<GlobalStateMachine>().AsSingle();
            Container.BindFactory<GlobalStateMachine, BootState, BootState.Factory>().AsSingle();
            Container.BindFactory<GlobalStateMachine, MainState, MainState.Factory>().AsSingle();
        }

        private void BindGameplay()
        {
            Container.Bind<NetworkManagerFactory>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerMovement>().AsSingle();
        }

        private void BindUI()
        {
            Container.Bind<UIGameMediator>().AsSingle();
            Container.Bind<LobbyFactory>().AsSingle();
        }

        private void BindServices()
        {
            Container.Bind<PlayerInputActions>().AsSingle().NonLazy();
            Container.Bind<InputService>().AsSingle();
            
            Container.Bind<IAssetProviderService>().To<AssetProviderService>().AsSingle();
        }
    }
}