using TF.Battle.Models;

namespace TF.Battle.Commands
{
    /// <summary>
    /// 対戦者が実行を指示した行動
    /// </summary>
    // TODO LESSON07-03: 第7回・2コマ目で、この指示を通信で運ぶDTOと変換処理を別途追加する。
    // Actor・TurnNumber・Commandを運び、ホスト側の受付で戦闘指示へ変換する。
    // TODO LESSON08-04: 第8回・2コマ目で通信指示を識別する番号と重複排除を追加する。
    // 送信者は通信APIから取得し、クライアントが指示に書いたActorと照合する。
    public sealed class BattleActionRequest
    {
        /// <summary>
        /// 行動を指示する対戦者
        /// </summary>
        public BattleSide Actor { get; init; }
        
        /// <summary>
        /// 行動を選択した時点のターン番号
        /// </summary>
        public int TurnNumber { get; init; }
        
        /// <summary>
        /// 実行を指示する行動
        /// </summary>
        public BattleCommand Command { get; init; }

        /// <summary>
        /// 対戦者・ターン番号・行動を指定して指示を作成
        /// </summary>
        public BattleActionRequest(BattleSide actor, int turnNumber, BattleCommand command)
        {
            Actor = actor;
            TurnNumber = turnNumber;
            Command = command;
        }
    }

}