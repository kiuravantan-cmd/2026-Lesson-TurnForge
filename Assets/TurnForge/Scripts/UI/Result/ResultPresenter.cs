using System;
using Cysharp.Threading.Tasks;
using TF.Battle;
using TF.Battle.Models;
using TF.GameFlow;

namespace TF.UI.Result
{
    /// <summary>
    /// 戦闘結果の表示と、再戦・タイトルへの遷移を接続
    /// </summary>
    public sealed class ResultPresenter : IDisposable
    {
        /// <summary>
        /// 結果画面の表示と操作通知
        /// </summary>
        private readonly IResultView _view;

        /// <summary>
        /// ゲーム全体の進行管理
        /// </summary>
        private readonly GameFlowController _gameFlow;

        /// <summary>
        /// ローディングを経由して戦闘を開始する処理
        /// </summary>
        private readonly Func<UniTask<bool>> _startBattleAsync;

        /// <summary>
        /// 勝敗表示の基準となるプレイヤーの陣営
        /// </summary>
        private readonly BattleSide _playerSide;

        /// <summary>
        /// イベントを購読しているか
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// 表示する結果が設定されているか
        /// </summary>
        private bool _hasResult;

        /// <summary>
        /// 再戦の開始処理が進行中か
        /// </summary>
        private bool _isStarting;

        /// <summary>
        /// このPresenterが破棄されたか
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 結果画面の操作を受け付けられるか
        /// </summary>
        private bool CanAcceptInput =>
            _isDisposed == false &&
            _isInitialized &&
            _hasResult &&
            _isStarting == false &&
            _gameFlow != null &&
            _gameFlow.CurrentState == GameState.Result;

        /// <summary>
        /// 表示・進行管理・再戦処理・プレイヤーの陣営を受け取る
        /// </summary>
        public ResultPresenter (
            IResultView view,
            GameFlowController gameFlow,
            Func<UniTask<bool>> startBattleAsync,
            BattleSide playerSide)
        {
            _view = view;
            _gameFlow = gameFlow;
            _startBattleAsync = startBattleAsync;
            _playerSide = playerSide;
        }

        public bool TryInitialize()
        {
            if (_isDisposed ||
                _view == null ||
                _gameFlow == null ||
                _startBattleAsync == null)
            {
                return false;
            }

            if (_playerSide != BattleSide.First && _playerSide != BattleSide.Second)
            {
                return false;
            }

            if (_isInitialized)
            {
                return true;
            }

            _view.RetryRequested += HandleRetryRequested;
            _view.ReturnToTitleRequested += HandleReturnToTitleRequested;
            _gameFlow.StateChanged += HandleStateChanged;
            _isInitialized = true;

            RefreshInput();
            return true;
        }

        /// <summary>
        /// バトル終了時の勝敗とターン番号を画面に表示する
        /// </summary>
        public bool TrySetResult(BattleState state)
        {
            if (_isDisposed ||
                _isInitialized == false ||
                state == null ||
                state.IsFinished == false ||
                state.FirstCombatant == null ||
                state.SecondCombatant == null)
            {
                return false;
            }

            // 戦闘終了時または結果画面でのみ設定を受け付ける
            switch (_gameFlow.CurrentState)
            {
                case GameState.Battle:
                case GameState.Result:
                    break;

                default:
                    return false;
            }

            // プレイヤーの陣営を基準に双方の状態を取り出す
            CombatantState player = _playerSide == BattleSide.First
                ? state.FirstCombatant
                : state.SecondCombatant;

            CombatantState opponent = _playerSide == BattleSide.First
                ? state.SecondCombatant
                : state.FirstCombatant;

            // 現在のルールでは、どちらかの戦闘不能で決着する
            if (!player.IsDefeated && !opponent.IsDefeated)
            {
                return false;
            }

            // 両者が戦闘不能の場合は引き分けとして表示する
            string resultMessage = player.IsDefeated
                ? (opponent.IsDefeated ? "引き分け" : "敗北")
                : "勝利";

            _view.Render(resultMessage, state.TurnNumber);
            _hasResult = true;

            RefreshInput();
            return true;
        }

        /// <summary>
        /// 結果画面からの再戦指示を受け付ける
        /// </summary>
        private void HandleRetryRequested()
        {
            if (!CanAcceptInput)
            {
                return;
            }

            // 非同期処理の開始前に両方のボタンを無効
            _isStarting = true;
            RefreshInput();

            StartBattleAsync().Forget();
        }

        /// <summary>
        /// 再戦処理の完了後、現在の状態に合わせて入力を更新
        /// </summary>
        private async UniTaskVoid StartBattleAsync()
        {
            // 成功・失敗に伴う画面遷移は開始処理側が担当
            await _startBattleAsync();

            _isStarting = false;

            if (_isDisposed)
            {
                return;
            }

            RefreshInput();
        }

        /// <summary>
        /// 結果画面からタイトルへ戻る
        /// </summary>
        private void HandleReturnToTitleRequested()
        {
            if (!CanAcceptInput)
            {
                return;
            }

            _gameFlow.TryReturnToTitle();
        }

        /// <summary>
        /// 画面遷移に合わせて結果の有効性と入力受付を更新
        /// </summary>
        private void HandleStateChanged(GameState state)
        {
            if (_isDisposed)
            {
                return;
            }

            // タイトルへ戻った場合や次の戦闘開始時に古い結果を無効化
            switch (state)
            {
                case GameState.Title:
                case GameState.Battle:
                    _hasResult = false;
                    break;
            }

            RefreshInput();
        }

        /// <summary>
        /// 結果設定済みで、再戦処理中でない場合だけ操作を許可
        /// </summary>
        private void RefreshInput()
        {
            _view.SetInputEnabled(CanAcceptInput);
        }

        /// <summary>
        /// 操作を停止し、イベントの購読を解除
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
                _view.RetryRequested -= HandleRetryRequested;
                _view.ReturnToTitleRequested -= HandleReturnToTitleRequested;
                _gameFlow.StateChanged -= HandleStateChanged;
                _isInitialized = false;

                _view.SetInputEnabled(false);
            }
        }
    }
}
