using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TF.Battle;
using TF.Battle.Commands;
using TF.Battle.Flow;
using TF.Battle.Models;
using TF.GameFlow;
using UnityEngine.Events;

namespace TF.UI.Battle
{
    /// <summary>
    /// 戦闘操作・状態表示・結果演出を接続
    /// </summary>
    public sealed class BattlePresenter : IDisposable
    {
        /// <summary>
        /// 戦闘画面の操作と表示
        /// </summary>
        private readonly IBattleView _view;

        /// <summary>
        /// 現在の戦闘状態
        /// </summary>
        private readonly BattleModel _model;

        /// <summary>
        /// 行動受付と演出待ちの管理
        /// </summary>
        private readonly BattleFlowController _flow;

        /// <summary>
        /// この画面から操作するプレイヤー
        /// </summary>
        private readonly BattleSide _inputSide;

        /// <summary>
        /// Presenterの破棄時に演出を中断する通知元
        /// </summary>
        private readonly CancellationTokenSource _lifeTimeCts = new CancellationTokenSource();

        /// <summary>
        /// 選択中のコマンド。未選択の場合はnull
        /// </summary>
        private BattleCommand? _selectedCommand;

        /// <summary>
        /// 操作イベントを購読しているか
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// 行動と演出を処理しているか
        /// </summary>
        private bool _isExecuting = false;

        /// <summary>
        /// このPresenterが破棄されたか
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// 最後の演出まで完了した戦闘状態を通知
        /// </summary>
        public event UnityAction<BattleState> BattleFinished;

        /// <summary>
        /// プレイヤーからの操作を受け付けられるか
        /// </summary>
        // TODO LESSON02-01: 初期化・破棄・実行中・入力受付・行動する番を判定する。
        // 第2回・1コマ目: 現在状態があり、バトルが続き、ActionSideが_inputSideと一致すること。
        private bool CanAcceptPlayerInput => false;

        /// <summary>
        /// 表示・状態・進行管理・プレイヤーの操作側を受け取る
        /// </summary>
        public BattlePresenter (IBattleView view, BattleModel model, BattleFlowController flow, BattleSide inputSide)
        {
            _view = view;
            _model = model;
            _flow = flow;
            _inputSide = inputSide;
        }

        /// <summary>
        /// 開始済みの戦闘へ接続し、初期状態を表示
        /// </summary>
        public bool TryInitialize()
        {
            if (_isDisposed || _view == null || _model?.CurrentState == null || _flow == null || !_flow.CanAcceptInput)
            {
                return false;
            }

            if (_inputSide != BattleSide.First && _inputSide != BattleSide.Second)
            {
                return false;
            }

            if (_isInitialized)
            {
                return true;
            }

            _view.CommandSelected += HandleCommandSelected;
            _view.ConfirmRequested += HandleConfirmRequested;
            _view.CancelRequested += HandleCancelRequested;
            _isInitialized = true;

            _view.Render(_model.CurrentState);
            _view.SetSelectedCommand(_selectedCommand);
            RefreshInput();
            return true;
        }

        /// <summary>
        /// プレイヤーの番で選択されたコマンドを保持する
        /// </summary>
        private void HandleCommandSelected(BattleCommand command)
        {
            // TODO LESSON02-02: 操作できるかどうかと技の実行条件を確認し、選択を保持してViewの表示を更新する。
            // 第2回・1コマ目: Model.CanExecute(_inputSide, command)で使える技かを調べる。
            // 使用可能なら_selectedCommandへ覚え、SetSelectedCommandとRefreshInputへ反映する。
            // 選んだだけでは戦闘を実行しない。
        }

        /// <summary>
        /// 選択中のコマンドを、現在のターンの指示として送る
        /// </summary>
        private void HandleConfirmRequested()
        {
            // TODO LESSON02-03: 選択済みの技を再確認し、現在のターン番号で指示を作る。
            // 第2回・1コマ目: 受付可能・選択あり・使用可能を確認し、最新番号でBattleActionRequestを作る。
            // TryExecuteAsyncへ合流させ、方式別の攻撃処理を作らない。
            // パッド・キー・クリックともTryExecuteAsyncへ送る。
        }

        /// <summary>
        /// コマンド選択を取り消す
        /// </summary>
        private void HandleCancelRequested()
        {
            // TODO LESSON02-04: 操作可能な場合に選択を解除し、Viewの表示を更新する。
            // 第2回・1コマ目: _selectedCommandをnullにし、表示とボタンを更新する。HPや番号は変えない。
        }

        /// <summary>
        /// 行動を実行し、演出と表示更新まで完了する。
        /// </summary>
        public async UniTask<bool> TryExecuteAsync(BattleActionRequest request)
        {
            if (_isDisposed || !_isInitialized || _isExecuting || !_flow.CanAcceptInput)
            {
                return false;
            }

            _isExecuting = true;
            _view.SetInputEnabled(false);

            // 実行できない指示では演出を開始しない
            if (!_flow.TryExecute(request, out BattleResult result))
            {
                _isExecuting = false;
                RefreshInput();
                return false;
            }

            _selectedCommand = null;
            _view.SetSelectedCommand(null);

            // プレイヤーの陣営を基準に、確定した結果の表示文を作成する
            string message = BattleResultFormatter.Format(result, _inputSide);

            // 表示と演出が完了するまで、次の行動を受け付けない
            bool canceled = await _view.PlayResultAsync(result, message, _lifeTimeCts.Token).SuppressCancellationThrow();

            _isExecuting = false;

            if (_isDisposed)
            {
                // 実行中の演出が終了してから通知元を解放
                _lifeTimeCts?.Dispose();
                return false;
            }

            // 演出が中断されても、確定済みの状態は維持
            _view.Render(_model.CurrentState);

            if (!_flow.TryCompleteEffects(result))
            {
                RefreshInput();
                return false;
            }

            RefreshInput();

            if (_flow.ActionState == BattleActionState.Finished)
            {
                BattleFinished?.Invoke(_model.CurrentState);
            }

            // falseでも、成立済みの行動は巻き戻さない
            return !canceled;
        }

        /// <summary>
        /// 現在の実行条件と行動する番に合わせて、選択内容と操作できるかどうかを更新する
        /// </summary>
        private void RefreshInput()
        {
            if (_isDisposed || !_isInitialized)
            {
                return;
            }

            // TODO LESSON02-05: 各技が使えるか確かめ、選択内容とボタンの操作を更新する。
            // 第2回・1コマ目: 各技のModel.CanExecuteをSetCommandEnabledへ反映する。
            // 使用不可になった選択を解除し、選択表示と全体のSetInputEnabledを更新する。
            // 第2回・3コマ目: 方式変更時もPresenterの選択とViewの表示の両方を解除する。
            // 配布時は未実装の操作が実行されないように全体を無効化する。
            _view.SetInputEnabled(false);
            // TODO LESSON04-02: R3で操作できるかどうかの変化を購読し、再表示時の重複購読を防ぐ。
            // 第4回・2〜3コマ目: HP・エネルギー・操作可否の変化を購読し、Viewの表示へ反映する。
            // 購読は初期化時に1回だけ開始する。RefreshInputが呼ばれるたびに追加しない。
            // 第4回・4コマ目: HPが少ない場合の警告表示を購読から追加する。
        }

        /// <summary>
        /// 操作の購読を解除し、実行中の演出に中断を指示
        /// </summary>
        public void Dispose()
        {
            // TODO LESSON04-03: 追加したR3の購読も、この所有者の終了時に解放する。
            // 第4回・3コマ目: 購読をまとめて保持し、Dispose時に解除する。再戦で前の購読を残さない。
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_isInitialized)
            {
                _view.CommandSelected -= HandleCommandSelected;
                _view.ConfirmRequested -= HandleConfirmRequested;
                _view.CancelRequested -= HandleCancelRequested;
                _isInitialized = false;
            }

            _lifeTimeCts?.Cancel();

            if (!_isExecuting)
            {
                _lifeTimeCts?.Dispose();
            }

            BattleFinished = null;
        }
    }
}
