using System;
using Gameplay;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIGameMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private readonly GameplayMediator _gameplayMediator;
        private GameHud _gameHud;
        private Slider _healthSlider;
        private PlayerView _localPlayer;

        public UIGameMediator(GenericFactory genericFactory, GameplayMediator gameplayMediator)
        {
            _genericFactory = genericFactory;
            _gameplayMediator = gameplayMediator;
        }

        public void Construct()
        {
            _gameHud = _genericFactory.Create<GameHud>(Constants.GameHudPath);
            _gameHud.Hide();
            
            Transform healthBarCanvas = _genericFactory.Create<Transform>(Constants.LocalHealthBarPath);
            _healthSlider = healthBarCanvas.GetComponentInChildren<Slider>();
            _healthSlider.maxValue = Constants.PlayerSettings.MaxHealth;
            _healthSlider.value = Constants.PlayerSettings.MaxHealth;
    
            _gameplayMediator.RoomCodeReady += OnRoomCodeReady;
            PlayerView.LocalPlayerStarted += OnLocalPlayerStarted;
        }

        private void OnLocalPlayerStarted(PlayerView playerView)
        {
            _localPlayer = playerView;
            _localPlayer.HealthChanged += OnHealthChanged;

            OnHealthChanged(_localPlayer.Health);
        }

        private void OnRoomCodeReady(string code)                    
        {
            _gameHud.ShowRoomCode(code);
        }

        private void OnHealthChanged(int newHealth)
        {
            _healthSlider.value = newHealth;
        }

        public void Dispose()
        {
            _gameplayMediator.RoomCodeReady -= OnRoomCodeReady;
            PlayerView.LocalPlayerStarted -= OnLocalPlayerStarted;

            if (_localPlayer != null)
                _localPlayer.HealthChanged -= OnHealthChanged;
        }
    }
}