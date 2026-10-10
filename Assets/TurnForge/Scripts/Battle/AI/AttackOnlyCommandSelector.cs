using TF.Battle.Commands;
using TF.Battle.Models;

namespace TF.Battle.AI
{
    /// <summary>
    /// 自分の番で通常攻撃を選択
    /// </summary>
    public sealed class AttackOnlyCommandSelector : IBattleCommandSelector
    {
        /// <summary>
        /// 戦闘が継続中で、自分の番なら攻撃を選択
        /// </summary>
        public bool TrySelectCommand(BattleState state, BattleSide actor, out BattleCommand command)
        {
            command = default;
            if (state == null || state.IsFinished
                || (actor != BattleSide.First && actor != BattleSide.Second)
                || state.ActionSide != actor) {
                return false;
            }

            // 行動する側の状態。CPUをSecondに固定せず、actorから選ぶ。
            CombatantState self = actor == BattleSide.First ? state.FirstCombatant : state.SecondCombatant;
            if (self == null || self.MaxHp <= 0 || self.Hp <= 0 || self.Hp > self.MaxHp)
            {
                return false;
            }

            command = self.Hp <= self.MaxHp / 2
                ? BattleCommand.Heal : BattleCommand.Attack;
            return true;

            // TODO LESSON05-01: このインターフェースを使う別のCPU判断を追加し、DIで差し替える。
            // 第5回・1/4コマ目: IBattleCommandSelectorを実装した別クラスを追加する。
            // CPUは技だけを選び、Rulesを変更せず登録した実装の差し替えで判断を変える。
            return false;
        }
    }
}
