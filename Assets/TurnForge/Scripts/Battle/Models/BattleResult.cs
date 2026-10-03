using TF.Battle.Commands;

namespace TF.Battle.Models
{
    /// <summary>
    /// 実行できる行動によって確定した戦闘結果
    /// </summary>
    public sealed class BattleResult
    {
        /// <summary>
        /// 実行した行動の指示
        /// </summary>
        public BattleActionRequest Request { get; init; }
        
        /// <summary>
        /// 行動前の戦闘状態
        /// </summary>
        public BattleState PreviousState { get; init; }
        
        /// <summary>
        /// 行動後の戦闘状態
        /// </summary>
        public BattleState NextState { get; init; }

        /// <summary>
        /// 実行した行動と、その前後の状態を設定
        /// </summary>
        public BattleResult(BattleActionRequest request, BattleState previousState, BattleState nextState)
        {
            Request = request;
            PreviousState = previousState;
            NextState = nextState;
        }
    }
}