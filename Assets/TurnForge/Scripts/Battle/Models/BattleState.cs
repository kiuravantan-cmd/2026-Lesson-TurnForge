namespace TF.Battle.Models
{
    /// <summary>
    /// 戦闘全体の状態
    /// </summary>
    public sealed class BattleState
    {
        /// <summary>
        /// 1人目の対戦者の状態
        /// </summary>
        public CombatantState FirstCombatant { get; init; }
        
        /// <summary>
        /// 2人目の対戦者の状態
        /// </summary>
        public CombatantState SecondCombatant { get; init; }
        
        /// <summary>
        /// 現在、行動する権利を持つ対戦者
        /// </summary>
        public BattleSide ActionSide { get; init; }
        
        /// <summary>
        /// 現在のターン番号
        /// 1から開始する
        /// </summary>
        public int TurnNumber { get; init; }
        
        /// <summary>
        /// 戦闘が終了しているか
        /// </summary>
        public bool IsFinished { get; init; }

        /// <summary>
        /// 双方の状態と戦闘の進行状況を設定
        /// </summary>
        public BattleState(
            CombatantState firstCombatant,
            CombatantState secondCombatant,
            BattleSide actionSide,
            int turnNumber,
            bool isFinished)
        {
            FirstCombatant = firstCombatant;
            SecondCombatant = secondCombatant;
            ActionSide = actionSide;
            TurnNumber = turnNumber;
            IsFinished = isFinished;
        }
    }
}