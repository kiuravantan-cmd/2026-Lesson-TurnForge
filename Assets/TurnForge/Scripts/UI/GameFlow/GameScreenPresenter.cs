using System;
using TF.GameFlow;
using UnityEditor;

namespace TF.UI.GameFlow
{
    /// <summary>
    /// ゲームの進行状態に応じて、表示する画面を切り替える
    /// </summary>
    public class GameScreenPresenter : IDisposable
    {
        /// <summary>
        /// ゲーム全体の進行管理
        /// </summary>
        private readonly GameFlowController _gameFlow;

        /// <summary>
        /// 画面の表示切替
        /// </summary>
        private readonly IGameScreenView _view;

        /// <summary>
        /// 状態変更を購読しているか
        /// </summary>
        private bool _isInitialized = false;
        
        /// <summary>
        /// このPresenterが破棄されたか
        /// </summary>
        private bool _isDisposed = false;
        
        /// <summary>
        /// 進行管理と画面表示を受け取る
        /// </summary>
        public GameScreenPresenter(GameFlowController gameFlow, IGameScreenView view)
        {
            _gameFlow = gameFlow;
            _view = view;
        }

        /// <summary>
        /// 状態変更の購読を開始し、現在の画面を表示
        /// </summary>
        public bool TryInitialize()
        {
            if (_isDisposed || _gameFlow == null || _view == null)
            {
                return false;
            }

            if (_isInitialized)
            {
                return true;
            }

            _gameFlow.StateChanged += HandleStateChanged;
            _isInitialized = true;
            
            // 通知を受け取る前の状態も、最初に画面へ表示する。
            HandleStateChanged(_gameFlow.CurrentState);
            return true;
        }

        /// <summary>
        /// 現在の状態に対応する画面を表示
        /// </summary>
        private void HandleStateChanged(GameState state)
        {
            if (_isDisposed)
            {
                return;
            }

            switch (state)
            {
                case GameState.Inactive:
                    _view.HideAll();
                    break;
                
                case GameState.Loading:
                    _view.ShowLoading("ゲームデータを読み込んでいます...");
                    break;
                
                case GameState.Title:
                    _view.ShowTitle();
                    break;
                
                case GameState.BattleLoading:
                    _view.ShowLoading("戦闘準備をしています...");
                    break;
                
                case GameState.Battle:
                    _view.ShowBattle();
                    break;
                
                case GameState.Result:
                    _view.ShowResult();
                    break;
                
                case GameState.Error:
                    _view.ShowError(
                        "初期化に失敗しました。設定を確認して再起動してください。");
                    break;
                
                default:
                    _view.ShowError($"画面の状態が不正です。{state}");
                    break;
            }
        }

        /// <summary>
        /// 状態変更の購読を解除
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            
            _isDisposed = true;

            if (_isInitialized)
            {
                _gameFlow.StateChanged -= HandleStateChanged;
                _isInitialized = false;
            }
        }
    }
}