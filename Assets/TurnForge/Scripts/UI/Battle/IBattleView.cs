using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TF.Battle.Commands;
using TF.Battle.Models;
using UnityEngine.Events;

namespace TF.UI.Battle
{
    /// <summary>
    /// 戦闘画面の操作通知・表示・演出を定義
    ///
    /// 【操作と表示の流れ】
    /// パッド・キーボード・クリック
    ///    ↓
    /// CommandSelected → Presenterが選択を保持
    ///    ↓
    /// ConfirmRequested → FlowControllerで実行
    ///    ↓
    /// PlayResultAsync → 演出完了を待つ
    ///    ↓
    /// Render → 確定状態を表示
    ///    ↓ 
    /// TryCompleteEffects → 次の受付または終了
    /// </summary>
    public interface IBattleView
    {
        /// <summary>
        /// コマンドが選択された時に通知
        /// </summary>
        event UnityAction<BattleCommand> CommandSelected;

        /// <summary>
        /// 選択したコマンドの実行が指示されたときに通知
        /// </summary>
        event UnityAction ConfirmRequested;
        
        /// <summary>
        /// コマンド選択の取り消しが指示されたときに通知
        /// </summary>
        event UnityAction CancelRequested;

        /// <summary>
        /// HP・エネルギー・行動する番などを指定された状態で表示
        /// </summary>
        void Render(BattleState state);
        
        /// <summary>
        /// 戦闘操作全体の受付を切り替える
        /// </summary>
        void SetInputEnabled(bool enabled);

        /// <summary>
        /// 選択中のコマンドを表示
        /// nullの場合は選択を解除
        /// </summary>
        void SetSelectedCommand(BattleCommand? command);

        /// <summary>
        /// 確定済みの行動結果と表示文を反映し、演出完了まで待機する
        /// </summary>
        /// <param name="result">確定した行動結果</param>
        /// <param name="message">Presenterが作成した表示文</param>
        /// <param name="token">演出の中断通知</param>
        UniTask PlayResultAsync(BattleResult result, string message, CancellationToken token);

        /// <summary>
        /// 指定した技が使えるかどうかを画面に表示する
        /// </summary>
        /// <param name="command">対象のコマンド</param>
        /// <param name="enabled">実行条件を満たしているか</param>
        void SetCommandEnabled(BattleCommand command, bool enabled);
    }
}