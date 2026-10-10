using System;
using System.Collections.Generic;
using TF.Battle.Commands;
using TF.Battle.Models;
using TF.MasterData;
using UnityEngine;

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
            if (!CanExecute(currentState, request, out var battleCommand))
            {
                return false;
            }

            CombatantState actor = GetCombatant(currentState, request.Actor);
            CombatantState target = GetCombatant(
                currentState, GetOpponentSide(request.Actor));

            CombatantState nextActor = actor;
            CombatantState nextTarget = target;

            switch (request.Command)
            {
                case BattleCommand.Attack:
                    nextTarget = ApplyDamage(target, battleCommand.Damage, 1);
                    break;
                case BattleCommand.Heal:
                    nextActor = ApplyRecovery(actor, battleCommand.Recovery);
                    break;
                default:
                    return false;
            }

            bool isFinished = nextTarget.IsDefeated;

            int nextTurnNumber = currentState.TurnNumber;
            BattleSide nextActionSide = currentState.ActionSide;
            if (!isFinished)
            {
                if (nextTurnNumber >= int.MaxValue)
                {
                    return false;
                }

                nextActionSide = target.Side;
                nextTurnNumber++;
            }

            CombatantState nextFirst =
                request.Actor == BattleSide.First ? actor : nextTarget;
            CombatantState nextSecond =
                request.Actor == BattleSide.Second ? actor : nextTarget;

            BattleState nextState = new BattleState(
                nextFirst, nextSecond, nextActionSide, nextTurnNumber, isFinished);

            result = new BattleResult(request, currentState, nextState);
            return true;

            // TODO LESSON06-01: 防御・チャージ・必殺技、コストとエネルギー上限を追加する。
            // 第6回・1コマ目: マスタのEnergyCostを引き、EnergyGainを加え、MaxEnergy以下に収める。
            // 防御は自分のIsGuardingを立て、次の自分の番開始時に解除する。
            // 通常攻撃・必殺技は相手へダメージを与え、チャージはエネルギーを増やす。
            // 加算は longなどで中間計算し、上限を適用してからintへ戻す。
            // 防御は次の自分の番開始時に解除する。計算値の桁あふれにも対応する。
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
            if (state == null || request == null)
            {
                return false;
            }

            if (!IsConfigured)
            {
                return false;
            }

            if (state.IsFinished || state.TurnNumber < 1)
            {
                return false;
            }

            if (!IsValidCombatant(state.FirstCombatant, BattleSide.First) ||
                !IsValidCombatant(state.SecondCombatant, BattleSide.Second))
            {
                return false;
            }

            if (state.ActionSide != BattleSide.First &&
                state.ActionSide != BattleSide.Second)
            {
                return false;
            }

            if (request.Command != BattleCommand.Attack ||
                request.Command != BattleCommand.Heal)
            {
                return false;
            }

            if (state.ActionSide != request.Actor ||
                state.TurnNumber != request.TurnNumber)
            {
                return false;
            }

            return _commandData.TryGetValue(request.Command, out commandData);

            // TODO LESSON06-02: 技のコスト不足・チャージ上限を確認する。
            // 第6回・1コマ目: マスタのコスト以上のエネルギーがあるかを調べる。
            // エネルギー最大時のチャージを拒否し、ターン開始時の防御解除と条件をそろえる。
            // TODO LESSON08-01: オンラインでもホスト側でこの判定を通す。
            // 第8回・1コマ目: ホストの最新状態で陣営・番・番号・コストを確認する。
            // 送信者と陣営の対応は通信の受付側で確認する。Actorの申告だけでは信用しない。
            // 現在のターン番号と指示の番号を照合し、古い指示・二重指示を受け付けない。
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
            int nextHP = Mathf.Max(0, target.Hp - damage);
            return CopyCombatant(target, nextHP, target.Energy, target.IsGuarding);

            // TODO LESSON06-03: 防御中は guardDamageDivisor で整数除算してから適用する。
            // 第6回・1コマ目: 防御中だけ damage を除数で割り、端数を切り捨ててからHPへ適用する。
        }

        /// <summary>
        /// 最大HPを超えないように回復し、新しい状態を作る。
        /// </summary>
        private static CombatantState ApplyRecovery(CombatantState actor, int amount)
        {
            // 加算途中で、intの上限を超えないよう、longで計算する。
            long calculatedHp = (long)actor.Hp + amount;
            int nextHp = (int)Math.Min((long)actor.MaxHp, calculatedHp);
            return CopyCombatant(actor, nextHp, actor.Energy, actor.IsGuarding);
        }

        /// <summary>
        /// キャラクターを区別する値と最大値を引き継ぎ、指定された値で状態を作成
        /// </summary>
        private static CombatantState CopyCombatant(CombatantState source, int hp, int energy, bool isGuarding)
        {
            return new CombatantState(source.Side, hp, source.MaxHp, energy, source.MaxEnergy, isGuarding);
        }
    }
}
