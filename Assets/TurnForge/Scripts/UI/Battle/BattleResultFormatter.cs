using System.Text;
using TF.Battle.Commands;
using TF.Battle.Models;

namespace TF.UI.Battle
{
    /// <summary>
    /// 確定した戦闘結果から、画面に表示する文章を作成する
    /// </summary>
    public static class BattleResultFormatter
    {
        /// <summary>
        /// 行動内容と、実際のHP・エネルギー変化を文章へ変換
        /// </summary>
        /// <param name="result">行動前後の状態を含む結果</param>
        /// <param name="playerSide">プレイヤーが操作する陣営</param>
        public static string Format(BattleResult result, BattleSide playerSide)
        {
            if (result?.Request == null || result.PreviousState == null || result.NextState == null)
            {
                return string.Empty;
            }

            if (playerSide != BattleSide.First && playerSide != BattleSide.Second)
            {
                return string.Empty;
            }

            // 行動した陣営
            BattleSide actorSide = result.Request.Actor;

            if (actorSide != BattleSide.First && actorSide != BattleSide.Second)
            {
                return string.Empty;
            }

            // 行動前後の相手
            CombatantState previousActor = actorSide == BattleSide.First
                ? result.PreviousState.FirstCombatant
                : result.PreviousState.SecondCombatant;

            CombatantState nextActor = actorSide == BattleSide.First
                ? result.NextState.FirstCombatant
                : result.NextState.SecondCombatant;

            // 行動前後の相手
            CombatantState previousTarget = actorSide == BattleSide.First
                ? result.PreviousState.SecondCombatant
                : result.PreviousState.FirstCombatant;

            CombatantState nextTarget = actorSide == BattleSide.First
                ? result.NextState.SecondCombatant
                : result.NextState.FirstCombatant;

            if (previousActor == null || nextActor == null || previousTarget == null || nextTarget == null)
            {
                return string.Empty;
            }

            // プレイヤー視点での表示名
            string actorName = actorSide == playerSide ? "あなた" : "相手";
            string targetName = actorSide == playerSide ? "相手" : "あなた";

            // 作る表示文
            var message = new StringBuilder();

            switch (result.Request.Command)
            {
                case BattleCommand.Attack:
                    message.Append($"{actorName}の攻撃！");
                    break;

                case BattleCommand.Guard:
                    message.Append($"{actorName}は防御した。");
                    break;

                case BattleCommand.Charge:
                    message.Append($"{actorName}はチャージした。");
                    break;

                case BattleCommand.Special:
                    message.Append($"{actorName}の必殺技！");
                    break;

                default:
                    return string.Empty;
            }

            // 防御による軽減やHPの下限を反映した、実際の減少量
            int lostHp = previousTarget.Hp - nextTarget.Hp;

            switch (result.Request.Command)
            {
                case BattleCommand.Attack:
                case BattleCommand.Special:
                    message.Append($"\n{targetName}のHPが{lostHp}減少。");
                    break;

                default:
                    break;
            }

            // 消費と回復を適用した後の、実際のエネルギー増減
            int energyChange = nextActor.Energy - previousActor.Energy;
            if (energyChange > 0)
            {
                message.Append($"\n{actorName}のエネルギーが{energyChange}増加。");
            }
            else if (energyChange < 0)
            {
                message.Append($"\n{actorName}のエネルギーが{-energyChange}減少。");
            }

            return message.ToString();
        }
    }
}
