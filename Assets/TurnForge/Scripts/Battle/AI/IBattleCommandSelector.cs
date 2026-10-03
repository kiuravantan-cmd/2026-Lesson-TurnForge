using TF.Battle.Commands;
using TF.Battle.Models;

namespace TF.Battle.AI
{
    /// <summary>
    /// 戦闘状態に応じたコマンドの選択方法を定義
    /// </summary>
    public interface IBattleCommandSelector
    {
        /// <summary>
        /// 指定したキャラクターのコマンドを選択する。選択できない場合はfalseを返す。
        /// </summary>
        bool TrySelectCommand(BattleState state, BattleSide actor, out BattleCommand command);
    }
}