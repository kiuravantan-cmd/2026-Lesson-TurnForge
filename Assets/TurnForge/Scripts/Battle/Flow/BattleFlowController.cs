using TF.Battle.Commands;
using TF.Battle.Models;
using UnityEngine.UIElements;

namespace TF.Battle.Flow
{
    /// <summary>
    /// 行動の受付と演出待ちを含む戦闘の進行を管理
    ///
    /// 【Presenterからの呼び出しの流れ】
    /// TryStartBattle()
    ///     ↓
    /// プレイヤーまたはCPUが行動を選択
    ///     ↓
    /// TryExecute(request, out result)
    ///     ├─ false → 行動できなかった場合
    ///     └─ true  → resultを使って演出を開始
    ///                    ↓
    ///                演出が完了
    /// </summary>
    public sealed class BattleFlowController
    {
        /// <summary>
        /// 戦闘状態の更新を担当するモデル
        /// </summary>
        private readonly BattleModel _model;
        
        /// <summary>
        /// 現在の進行段階
        /// </summary>
        private BattleActionState _actionState;

        /// <summary>
        /// 演出完了を待っている行動結果
        /// </summary>
        private BattleResult _pendingResult;
        
        /// <summary>
        /// 現在の進行段階（プロパティ）
        /// </summary>
        public BattleActionState ActionState => _actionState;
        
        /// <summary>
        /// 行動の指示を受け付けられるか
        /// </summary>
        public bool CanAcceptInput => _actionState == BattleActionState.WaitingForInput;

        /// <summary>
        /// 使用するモデルを受け取り、開始前の状態にする
        /// </summary>
        public BattleFlowController(BattleModel model)
        {
            _model = model;
            _actionState = BattleActionState.Inactive;
        }

        /// <summary>
        /// 初期状態に応じて、入力受付または終了状態へ進む
        /// </summary>
        public bool TryStartBattle()
        {
            if (_actionState != BattleActionState.Inactive)
            {
                return false;
            }

            if (_model?.CurrentState == null)
            {
                return false;
            }

            _actionState = _model.CurrentState.IsFinished
                ? BattleActionState.Finished
                : BattleActionState.WaitingForInput;
            
            return true;
        }

        /// <summary>
        /// 受付中の行動を処理し、成功した場合は演出待ちにする。
        /// </summary>
        public bool TryExecute(BattleActionRequest request, out BattleResult result)
        {
            result = null;

            if (!CanAcceptInput)
            {
                return false;
            }
            
            // モデルの通知中にも次の指示が入らないように先に閉じる
            _actionState = BattleActionState.Resolving;

            if (!_model.TryExecute(request, out result))
            {
                _actionState = BattleActionState.WaitingForInput;
                return false;
            }

            _pendingResult = result;
            _actionState = BattleActionState.PlayingEffects;
            return true;
        }

        /// <summary>
        /// 対応する結果の演出が完了したら、次の受付または終了へ進む。
        /// </summary>
        public bool TryCompleteEffects(BattleResult result)
        {
            if (_actionState != BattleActionState.PlayingEffects)
            {
                return false;
            }

            // 過去の演出や別の結果からの完了通知を受付ない
            if (result == null || !ReferenceEquals(result, _pendingResult))
            {
                return false;
            }

            _pendingResult = null;

            _actionState = result.NextState.IsFinished 
                ? BattleActionState.Finished 
                : BattleActionState.WaitingForInput;
            return true;
        }
    }
}