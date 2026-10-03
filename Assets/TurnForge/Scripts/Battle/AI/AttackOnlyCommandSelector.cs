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
            // TODO LESSON01-05: 継続中かつ指定した側の番なら通常攻撃を選ぶ。
            // 第1回・2コマ目: 状態あり・継続中・有効な陣営・自分の番ならAttackを返す。
            // TODO LESSON01-07: 第1回・4コマ目でactor側のHPを読み、半分以下なら回復を選ぶ。
            // HPが半分より多ければAttack。HP計算や番の交代はRulesに任せる。
            // TODO LESSON05-01: このインターフェースを使う別のCPU判断を追加し、DIで差し替える。
            // 第5回・1/4コマ目: IBattleCommandSelectorを実装した別クラスを追加する。
            // CPUは技だけを選び、Rulesを変更せず登録した実装の差し替えで判断を変える。
            return false;
        }
    }
}
