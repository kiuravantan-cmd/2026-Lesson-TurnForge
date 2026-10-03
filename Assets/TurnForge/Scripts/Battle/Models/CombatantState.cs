namespace TF.Battle.Models
{
    /// <summary>
    /// 対戦者1人分の戦闘状態
    /// </summary>
    public sealed class CombatantState
    {
        /// <summary>
        /// 対戦者の識別子
        /// </summary>
        public BattleSide Side { get; init; }
        
        /// <summary>
        /// 現在の体力
        /// </summary>
        public int Hp { get; init; }
        
        /// <summary>
        /// 最大体力
        /// </summary>
        public int MaxHp { get; init; }
        
        /// <summary>
        /// 現在のエネルギー
        /// </summary>
        public int Energy { get; init; }
        
        /// <summary>
        /// 最大エネルギー
        /// </summary>
        public int MaxEnergy { get; init; }
        
        /// <summary>
        /// 防御状態が有効か
        /// </summary>
        public bool IsGuarding { get; init; }

        /// <summary>
        /// 体力が0以下になっているか
        /// </summary>
        public bool IsDefeated => Hp <= 0;

        /// <summary>
        /// 対戦者の識別子と戦闘状態を設定
        /// </summary>
        public CombatantState(BattleSide side, int hp, int maxHp, int energy, int maxEnergy, bool isGuarding)
        {
            Side = side;
            Hp = hp;
            MaxHp = maxHp;
            Energy = energy;
            MaxEnergy = maxEnergy;
            IsGuarding = isGuarding;
        }
    }
}