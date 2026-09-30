using System;
using Gameplay;
using Services;
using UnityEngine;

namespace UI
{
    public class UIGameMediator : IDisposable
    {
        private readonly GenericFactory _genericFactory;
        private readonly GameplayMediator _gameplayMediator;
        private GameHud _gameHud;

        public UIGameMediator(GenericFactory genericFactory, GameplayMediator gameplayMediator)
        {
            _genericFactory = genericFactory;
            _gameplayMediator = gameplayMediator;
        }

        public void Construct()
        {
            _gameHud = _genericFactory.Create<GameHud>(Constants.GameHudPath);
            _gameHud.Hide();

            _gameplayMediator.RoomCodeReady += OnRoomCodeReady;
        }

        private void OnRoomCodeReady(string code)
        {
            _gameHud.ShowRoomCode(code);
        }

        public void Dispose()
        {
            _gameplayMediator.RoomCodeReady -= OnRoomCodeReady;
        }
    }
}