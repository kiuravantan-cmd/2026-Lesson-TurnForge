using TF.Battle.Commands;
using TF.Battle.Models;
using TF.Battle.Rules;
using UnityEngine.Events;

namespace TF.Battle
{
    /// <summary>
    /// 現在のHPや行動する番を覚えておき、行動後のデータに更新する
    /// </summary>
    public sealed class BattleModel
    {
        /// <summary>
        /// 行動の確認と状態の計算を担当するルール
        /// </summary>
        private readonly BattleRules _rules;
        
        /// <summary>
        /// 現在の戦闘状態
        /// </summary>
        private BattleState  _currentState;
        
        /// <summary>
        /// 現在の戦闘状態（プロパティ）
        /// </summary>
        public BattleState CurrentState => _currentState;

        /// <summary>
        /// 実行できる行動の結果を適用した後に通知
        /// </summary>
        public event UnityAction<BattleResult> ActionResolved;
        
        /// <summary>
        /// 戦闘ルールと初期状態を受け取る
        /// </summary>
        public BattleModel(BattleRules rules, BattleState initialState)
        {
            _rules = rules;
            _currentState = initialState;
        }

        /// <summary>
        /// 現在の戦闘状態で、指定したコマンドの実行条件を満たすか
        /// </summary>
        /// <param name="actor">行動する陣営</param>
        /// <param name="command">確認するコマンド</param>
        public bool CanExecute(BattleSide actor, BattleCommand command)
        {
            if (_rules == null || _currentState == null)
            {
                return false;
            }

            // 現在のターン番号を使って確認用の指示を作成
            var request = new BattleActionRequest(actor, _currentState.TurnNumber, command);

            return _rules.CanExecute(_currentState, request);
        }

        public bool TryExecute(BattleActionRequest request, out BattleResult result)
        {
            result = null;
            // TODO LESSON01-04: ルールへ処理を委ね、成功時だけ現在状態を置き換える。
            // 第1回・2コマ目: Rules.TryExecuteが成功した場合だけNextStateを_currentStateへ採用する。
            // その後ActionResolvedを通知する。失敗時は状態を置き換えず、通知もしない。
            // 現在状態を更新した後にActionResolvedで結果を通知する。
            // 実行できない指示では現在状態を変更しない。
            if (request == null || _rules == null || _currentState == null)
            {
                return false;
            }

            if (!_rules.TryExecute(_currentState, request, out result))
            {
                return false;
            }

            _currentState = result.NextState;
            ActionResolved?.Invoke(result);
            return true;

            // TODO LESSON04-01: R3による状態通知へ移行し、購読の所有者と寿命を決める。
            // 第4回・1〜2コマ目: 現在状態の通知元をR3で用意し、成功時にNextStateを通知する。
            // 変更前の状態を保持する設計は維持し、購読側から状態を直接変更させない。
            // 通知元を戦闘単位で所有し、終了時に解放する。導入前のイベントとの二重通知を避ける。
            return false;
        }
    }
}
