using TF.Battle.Models;
using TF.MasterData;

namespace TF.Battle.Factories
{
    /// <summary>
    /// 新しい戦闘の初期状態を作成
    /// </summary>
    public sealed class BattleStateFactory
    {
        /// <summary>
        /// 双方のマスタから、Firstが先攻の初期状態を作成
        /// </summary>
        public bool TryCreateInitialState(
            BattleCombatantDataRecord firstData,
            BattleCombatantDataRecord secondData,
            out BattleState state)
        {
            state = null;

            if (!CanCreateCombatant(firstData) || !CanCreateCombatant(secondData))
            {
                return false;
            }

            CombatantState firstCombatant = CreateCombatant(firstData, BattleSide.First);
            CombatantState secondCombatant = CreateCombatant(secondData, BattleSide.Second);

            state = new BattleState(
                firstCombatant: firstCombatant,
                secondCombatant: secondCombatant,
                actionSide: BattleSide.First,
                turnNumber: 1,
                isFinished: false);

            return true;
        }

        /// <summary>
        /// 戦闘開始に使用できる能力値か確認
        /// </summary>
        private static bool CanCreateCombatant(BattleCombatantDataRecord data)
        {
            return data is { MaxHp: > 0 }
                   && data.MaxEnergy >= 0
                   && data.InitialEnergy >= 0
                   && data.InitialEnergy <= data.MaxEnergy;
        }

        /// <summary>
        /// マスタの値をコピーし、キャラクターの初期状態を作成
        /// </summary>
        private static CombatantState CreateCombatant(BattleCombatantDataRecord data, BattleSide side)
        {
            return new CombatantState(
                side: side,
                hp: data.MaxHp,
                maxHp: data.MaxHp,
                energy: data.InitialEnergy,
                maxEnergy: data.MaxEnergy,
                isGuarding: false);
        }
    }
}