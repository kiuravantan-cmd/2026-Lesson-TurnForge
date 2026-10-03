using System;
using System.Collections.Generic;
using TF.Battle.Commands;
using TF.Battle.Models;
using TF.MasterData;

namespace TF.Battle.Rules
{
    /// <summary>
    /// 行動を確認し、次の戦闘状態を計算
    /// </summary>
    public sealed class BattleRules
    {
        /// <summary>
        /// コマンドごとのマスタデータ
        /// </summary>
        private readonly Dictionary<BattleCommand, BattleCommandDataRecord> _commandData =
            new  Dictionary<BattleCommand, BattleCommandDataRecord>();

        /// <summary>
        /// 戦闘に必要なマスタがそろっているか
        /// </summary>
        public bool IsConfigured { get; private set; }

        /// <summary>
        /// 読み込み済みのコマンドマスタを受け取る
        /// </summary>
        public BattleRules(IEnumerable<BattleCommandDataRecord> records)
        {
            if (records == null)
            {
                return;
            }

            // 各レコードをコマンド別に登録する
            foreach (var record in records)
            {
                if (record == null
                    || !Enum.IsDefined(typeof(BattleCommand), record.Command)
                    || record.Damage < 0
                    || record.EnergyCost < 0
                    || record.EnergyGain < 0
                    || record.GuardDamageDivisor < 1)
                {
                    return;
                }
                
                BattleCommand command = (BattleCommand)record.Command;
                if (_commandData.ContainsKey(command) || (command == BattleCommand.Charge && record.EnergyGain == 0))
                {
                    return;
                }
                
                _commandData.Add(command, record);
            }

            // 必要なコマンドの登録漏れを確認する
            foreach (BattleCommand command in Enum.GetValues(typeof(BattleCommand)))
            {
                if (!_commandData.ContainsKey(command))
                {
                    return;
                }
            }
            
            IsConfigured = true;
        }

        /// <summary>
        /// 状態を変更せず、行動の指示が実行条件を満たすか
        /// </summary>
        /// <param name="state">判定対象の戦闘状態</param>
        /// <param name="request">確認する行動の指示</param>
        public bool CanExecute(BattleState state, BattleActionRequest request)
        {
            return CanExecute(state, request, out _);
        }
        
        /// <summary>
        /// 行動を処理
        /// 無効な場合はfalseを返し、結果を作成しない
        /// </summary>
        public bool TryExecute(BattleState currentState, BattleActionRequest request, out BattleResult result)
        {
            result = null;
            // TODO LESSON01-03: CanExecuteで指示を確認し、行動する側と相手を取得する。
            // 第1回・2コマ目: resultをnullにし、CanExecuteで状態・指示・技のマスタを確認する。
            // 通常攻撃は相手のHPを減らし、HP0なら終了。続く場合だけ番を交代し、番号を1増やす。
            // First/Secondの配置を保ち、変更前と変更後をBattleResultへまとめる。
            // TODO LESSON01-06B: 第1回・3コマ目で回復の分岐を追加し、自分の回復後の状態を組み込む。
            // 回復でもターン交代と結果作成は共通にし、変更前の状態は書き換えない。
            // 通常攻撃のマスタ値とApplyDamageを使って、攻撃後の状態を作る。
            // HP0なら終了。バトルが続くときだけ行動する番を交代し、ターン番号を1進める。
            // First/Secondの順でBattleStateを作り、前後の状態をBattleResultへ渡す。
            // TODO LESSON06-01: 防御・チャージ・必殺技、コストとエネルギー上限を追加する。
            // 第6回・1コマ目: マスタのEnergyCostを引き、EnergyGainを加え、MaxEnergy以下に収める。
            // 防御は自分のIsGuardingを立て、次の自分の番開始時に解除する。
            // 通常攻撃・必殺技は相手へダメージを与え、チャージはエネルギーを増やす。
            // 加算はlongなどで中間計算し、上限を適用してからintへ戻す。
            // 防御は次の自分の番開始時に解除する。計算値の桁あふれにも対応する。
            return false;
        }

        /// <summary>
        /// 状態と指示を確認し、行動を実行できるか判定
        /// </summary>
        private bool CanExecute(
            BattleState state,
            BattleActionRequest request,
            out BattleCommandDataRecord commandData)
        {
            commandData = null;
            // TODO LESSON01-02: 未設定・終了済み・HPなどの値がルールに合わないキャラクター・行動する側が違う場合を受け付けない。
            // 第1回・1コマ目: null、終了、キャラクターのHPなど、陣営、ターン番号を順に確認する。
            // 指示のActorとActionSide、指示のTurnNumberと現在番号が一致すること。
            // 通常攻撃のマスタを取得してoutへ渡し、無効な指示では状態を変えずfalseを返す。
            // TODO LESSON01-06C: 第1回・3コマ目で回復も受け付けるよう、技の判定を広げる。
            // _commandDataから指示されたコマンドのマスタを取得する。
            // 前半はAttackだけ許可し、第3コマの課題で回復を追加する。防御・チャージ・必殺技は第6回。
            // TODO LESSON06-02: 技のコスト不足・チャージ上限を確認する。
            // 第6回・1コマ目: マスタのコスト以上のエネルギーがあるかを調べる。
            // エネルギー最大時のチャージを拒否し、ターン開始時の防御解除と条件をそろえる。
            // TODO LESSON08-01: オンラインでもホスト側でこの判定を通す。
            // 第8回・1コマ目: ホストの最新状態で陣営・番・番号・コストを確認する。
            // 送信者と陣営の対応は通信の受付側で確認する。Actorの申告だけでは信用しない。
            // 現在のターン番号と指示の番号を照合し、古い指示・二重指示を受け付けない。
            return false;
        }

        /// <summary>
        /// 戦闘に使えるキャラクターの状態か判定
        /// </summary>
        private static bool IsValidCombatant(CombatantState combatant, BattleSide expectedSide)
        {
            return combatant != null
                   && combatant.Side == expectedSide
                   && combatant.MaxHp > 0
                   && combatant.Hp > 0
                   && combatant.Hp <= combatant.MaxHp
                   && combatant.MaxEnergy >= 0
                   && combatant.Energy >= 0
                   && combatant.Energy <= combatant.MaxEnergy;
        }

        /// <summary>
        /// 識別子に対応するキャラクターを取得
        /// </summary>
        private static CombatantState GetCombatant(BattleState state, BattleSide side)
        {
            return side == BattleSide.First ? state.FirstCombatant : state.SecondCombatant;
        }

        /// <summary>
        /// 相手側を取得
        /// </summary>
        private static BattleSide GetOpponentSide(BattleSide side)
        {
            return side == BattleSide.First ? BattleSide.Second : BattleSide.First;
        }

        /// <summary>
        /// 防御による軽減を適用し、ダメージを受けた後の状態を作成
        /// </summary>
        private static CombatantState ApplyDamage(CombatantState target, int damage, int guardDamageDivisor)
        {
            // TODO LESSON01-01B: マスタのdamageをHPから引き、下限を0にする。
            // 第1回・1コマ目: Math.MaxでHPを0以上にし、CopyCombatantへ計算後のHPを渡す。
            // エネルギー・防御状態・Side・最大値は引き継ぐ。
            // CopyCombatantでHP以外を引き継いだ新しい状態を返す。
            // TODO LESSON06-03: 防御中はguardDamageDivisorで整数除算してから適用する。
            // 第6回・1コマ目: 防御中だけdamageを除数で割り、端数を切り捨ててからHPへ適用する。
            return target;
        }

        /// <summary>
        /// キャラクターを区別する値と最大値を引き継ぎ、指定された値で状態を作成
        /// </summary>
        private static CombatantState CopyCombatant(CombatantState source, int hp, int energy, bool isGuarding)
        {
            // TODO LESSON01-01A: sourceのSide・MaxHp・MaxEnergyを引き継ぐ。
            // 第1回・1コマ目: 引数のhp・energy・isGuardingで新しいCombatantStateを作る。
            // TODO LESSON01-06A: 第1回・3コマ目で回復用メソッドをこのクラスへ追加する。
            // 自分の状態とマスタの回復量を受け取り、MaxHpを超えないHPでCopyCombatantする。
            // 講師準備: 回復コマンド・回復量のマスタ・授業用入力口を先に用意する。
            // hp・energy・isGuardingは引数の値を使い、新しいCombatantStateを返す。
            return source;
        }
    }
}
