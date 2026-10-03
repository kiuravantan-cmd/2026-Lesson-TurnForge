using System;
using Cysharp.Threading.Tasks;
using TF.Battle.Commands;
using TF.Battle.Flow;
using TF.Battle.Models;
using TF.Infrastructure.Updating;
using UnityEngine.Events;

namespace TF.Battle.AI
{
    /// <summary>
    /// CPUの番でコマンドを選び、行動実行を指示する
    /// </summary>
    public sealed class BattleCpuController : IUpdateTickable, IDisposable
    {
        /// <summary>
        /// 現在のHPや行動する番を覚えておくモデル
        /// </summary>
        private readonly BattleModel _model;

        /// <summary>
        /// 行動受付と演出待ちの管理を行うフロー
        /// </summary>
        private readonly BattleFlowController _flow;

        /// <summary>
        /// CPUの行動を決定するためのコマンド選択ロジック
        /// </summary>
        private readonly IBattleCommandSelector _selector;

        /// <summary>
        /// CPUが操作する側の陣営
        /// </summary>
        private readonly BattleSide _cpuSide;

        /// <summary>
        /// 行動の実行と演出完了まで担当する非同期関数
        /// </summary>
        private readonly Func<BattleActionRequest, UniTask<bool>> _executeAsync;

        /// <summary>
        /// CPUの行動と演出が実行中か
        /// </summary>
        private bool _isExecuting;

        /// <summary>
        /// このControllerが破棄済みかどうか
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// CPUの番かどうかの確認を有効にするかどうか。戦闘画面への遷移後に有効化する
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 行動を選択または実行できず、CPUを停止した時に通知する
        /// </summary>
        public event UnityAction<string> Failed;

        /// <summary>
        /// 状態・進行管理・選択方法・操作側・実行処理を受け取る
        /// </summary>
        public BattleCpuController(
            BattleModel model,
            BattleFlowController flow,
            IBattleCommandSelector selector,
            BattleSide cpuSide,
            Func<BattleActionRequest, UniTask<bool>> executeAsync)
        {
            _model = model;
            _flow = flow;
            _selector = selector;
            _cpuSide = cpuSide;
            _executeAsync = executeAsync;
        }

        /// <summary>
        /// CPUの番で、演出中でなければ行動を開始する
        /// </summary>
        public void Tick(UpdateContext context)
        {
            if (_isDisposed || !IsEnabled || _isExecuting)
            {
                return;
            }

            if (_model == null || _flow == null || _selector == null || _executeAsync == null)
            {
                StopWithFailure("CPUの依存関係が設定されていません。");
                return;
            }

            if (_cpuSide != BattleSide.First && _cpuSide != BattleSide.Second)
            {
                StopWithFailure("無効なCPUの陣営が設定されています。");
                return;
            }

            // このフレームで確認する戦闘状態
            BattleState state = _model.CurrentState;

            if (state == null)
            {
                StopWithFailure("戦闘状態が取得できません。");
                return;
            }

            if (state.IsFinished || !_flow.CanAcceptInput || state.ActionSide != _cpuSide)
            {
                return;
            }

            // 選択方法を差し替えても、実行までの流れは共通にする
            if (!_selector.TrySelectCommand(state, _cpuSide, out BattleCommand command))
            {
                StopWithFailure("行動を選択できませんでした。");
                return;
            }

            // 選択した時点のターン番号を指示へ含める
            var request = new BattleActionRequest(_cpuSide, state.TurnNumber, command);

            // 非同期処理を開始する前に、二重実行を防止
            _isExecuting = true;
            ExecuteAsync(request).Forget();
        }

        /// <summary>
        /// CPUの行動と演出の完了を待つ
        /// </summary>
        private async UniTaskVoid ExecuteAsync(BattleActionRequest request)
        {
            // 実行処理が返す成功・失敗
            bool succeeded = await _executeAsync(request);

            _isExecuting = false;

            if (_isDisposed || !IsEnabled)
            {
                return;
            }

            if (!succeeded)
            {
                StopWithFailure($"CPUの行動を完了できませんでした：{request.Command}");
            }
        }

        /// <summary>
        /// 実行できない指示を毎フレーム繰り返さないように停止する
        /// </summary>
        private void StopWithFailure(string message)
        {
            IsEnabled = false;
            _isExecuting = false;
            Failed?.Invoke(message);
        }

        /// <summary>
        /// CPUの番かどうかの確認を有効化または無効化する
        /// </summary>
        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// 新しい行動の指示を停止し、通知先への参照を解除する
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            IsEnabled = false;
            Failed = null;
        }
    }
}
