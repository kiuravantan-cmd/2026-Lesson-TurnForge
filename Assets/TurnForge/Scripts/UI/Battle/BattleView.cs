using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TF.Battle.Commands;
using TF.Battle.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TF.UI.Battle
{
    /// <summary>
    /// 戦闘状態の表示と、コマンド操作の通知を担当
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleView : MonoBehaviour, IBattleView, ICancelHandler
    {
        /// <summary>
        /// 1人目のHP・エネルギー表示
        /// </summary>
        [SerializeField] private TextMeshProUGUI _firstStatus;

        /// <summary>
        /// 2人目のHP・エネルギー表示
        /// </summary>
        [SerializeField] private TextMeshProUGUI _secondStatus;

        /// <summary>
        /// 今どちらの番かを表示
        /// </summary>
        [SerializeField] private TextMeshProUGUI _turnText;

        /// <summary>
        /// 選択中のコマンド表示
        /// </summary>
        [SerializeField] private TextMeshProUGUI _selectionText;

        /// <summary>
        /// 行動結果表示
        /// </summary>
        [SerializeField] private TextMeshProUGUI _resultText;

        /// <summary>
        /// 攻撃コマンドボタン
        /// </summary>
        [SerializeField] private Button _attackButton;

        /// <summary>
        /// 回復コマンドボタン
        /// </summary>
        [SerializeField] private Button _healButton;

        /// <summary>
        /// 防御コマンドボタン
        /// </summary>
        [SerializeField] private Button _guardButton;
        
        /// <summary>
        /// チャージボタン
        /// </summary>
        [SerializeField] private Button _chargeButton;

        /// <summary>
        /// 必殺技ボタン
        /// </summary>
        [SerializeField] private Button _specialButton;

        /// <summary>
        /// 選択したコマンドを実行するボタン
        /// </summary>
        [SerializeField] private Button _confirmButton;

        /// <summary>
        /// 選択中のコマンドを取り消すボタン
        /// </summary>
        [SerializeField] private Button _cancelButton;

        /// <summary>
        /// Presenterから指定された入力受付状態
        /// </summary>
        private bool _isInputEnabled = false;

        /// <summary>
        /// 選択中のコマンド。未選択の場合はnull
        /// </summary>
        private BattleCommand? _selectedCommand;

        /// <summary>
        /// Presenterから使用可能と通知されたコマンド
        /// </summary>
        private readonly HashSet<BattleCommand> _availableCommands = new HashSet<BattleCommand>();

        /// <summary>
        /// コマンドが選択されたときに通知
        /// </summary>
        public event UnityAction<BattleCommand> CommandSelected;

        /// <summary>
        /// 決定操作を通知
        /// </summary>
        public event UnityAction ConfirmRequested;

        /// <summary>
        /// 選択中のコマンドの取り消し操作を通知
        /// </summary>
        public event UnityAction CancelRequested;

        /// <summary>
        /// ボタン操作の購読を開始
        /// </summary>
        private void OnEnable ()
        {
            SetButtonListeners(true);
            RefreshButtons();
        }

        /// <summary>
        /// ボタン操作の購読を解除
        /// </summary>
        private void OnDisable()
        {
            SetButtonListeners(false);
        }

        /// <summary>
        /// HP・エネルギー・行動する番を表示
        /// </summary>
        public void Render(BattleState state)
        {
            if (state == null)
            {
                return;
            }

            SetText(_firstStatus, FormatCombatant(state.FirstCombatant));
            SetText(_secondStatus, FormatCombatant(state.SecondCombatant));

            var turnText = state.IsFinished ? "戦闘終了" : $"ターン {state.TurnNumber}：{state.ActionSide}";
            SetText(_turnText, turnText);

            // 新しい戦闘の初回表示では、前の戦闘の結果文を消す
            if (state.TurnNumber == 1 && !state.IsFinished)
            {
                SetText(_resultText, string.Empty);
            }
        }

        /// <summary>
        /// 戦闘操作全体の受付を切り替える
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            RefreshButtons();
        }

        /// <summary>
        /// 指定した技が使えるかどうかを覚えておき、ボタンを更新する
        /// </summary>
        /// <param name="command">対象のコマンド</param>
        /// <param name="enabled">実行条件を満たしているか</param>
        public void SetCommandEnabled(BattleCommand command, bool enabled)
        {
            if (enabled)
            {
                _availableCommands.Add(command);
            }
            else
            {
                _availableCommands.Remove(command);
            }

            RefreshButtons();
        }

        /// <summary>
        /// Presenterが覚えている選択内容を画面に表示する
        /// </summary>
        public void SetSelectedCommand(BattleCommand? command)
        {
            _selectedCommand = command;

            var text = command.HasValue ? $"選択：{GetCommandName(command.Value)}" : "コマンドを選択してください";
            SetText(_selectionText, text);

            RefreshButtons();
        }

        public async UniTask PlayResultAsync(BattleResult result, string message, CancellationToken token)
        {
            if (!token.IsCancellationRequested && result?.NextState != null)
            {
                // 第1回の確認に必要な即時表示は講師基盤として提供する。
                Render(result.NextState);
                SetText(_resultText, message ?? string.Empty);
            }

            // TODO LESSON06-04: 演出の待機とキャンセルを追加する。
            // 第6回・2コマ目: 必要なDOTween・UniTaskで演出完了を待ち、終了時はtokenで中断する。
            // 演出の前に結果は確定済み。中断でHP・エネルギー・番を巻き戻さない。
            // 結果は確定済みのため、演出が中断されても戦闘状態を巻き戻さない。
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);
        }

        /// <summary>
        /// 入力受付と選択状態をボタンへ反映
        /// </summary>
        private void RefreshButtons()
        {
            SetInteractable(_attackButton, _isInputEnabled && _availableCommands.Contains(BattleCommand.Attack));
            SetInteractable(_guardButton, _isInputEnabled && _availableCommands.Contains(BattleCommand.Guard));
            SetInteractable(_chargeButton, _isInputEnabled && _availableCommands.Contains(BattleCommand.Charge));
            SetInteractable(_specialButton, _isInputEnabled && _availableCommands.Contains(BattleCommand.Special));
            SetInteractable(_healButton, _isInputEnabled && _availableCommands.Contains(BattleCommand.Heal));

            // 選択したコマンドが使用可能な場合だけ決定できる
            bool canConfirm = _isInputEnabled && _selectedCommand.HasValue && _availableCommands.Contains(_selectedCommand.Value);

            // 使用不可になったコマンドでも選択は取り消せる
            bool canCancel = _isInputEnabled && _selectedCommand.HasValue;

            SetInteractable(_confirmButton, canConfirm);
            SetInteractable(_cancelButton, canCancel);
        }

        /// <summary>
        /// 各ボタンの購読を追加または解除
        /// </summary>
        private void SetButtonListeners(bool subscribe)
        {
            SetListener(_attackButton, HandleAttack, subscribe);
            SetListener(_guardButton, HandleGuard, subscribe);
            SetListener(_chargeButton, HandleCharge, subscribe);
            SetListener(_specialButton, HandleSpecial, subscribe);
            SetListener(_confirmButton, HandleConfirm, subscribe);
            SetListener(_cancelButton, HandleCancel, subscribe);
            SetListener(_healButton, HandleHeal, subscribe);
        }

        /// <summary>
        /// 攻撃の選択を通知
        /// </summary>
        private void HandleAttack() => NotifySelection(BattleCommand.Attack);

        /// <summary>
        /// 回復の選択を通知
        /// </summary>
        private void HandleHeal() => NotifySelection(BattleCommand.Heal);

        /// <summary>
        /// 防御の選択を通知
        /// </summary>
        private void HandleGuard () => NotifySelection(BattleCommand.Guard);

        /// <summary>
        /// チャージの選択を通知
        /// </summary>
        private void HandleCharge () => NotifySelection(BattleCommand.Charge);

        /// <summary>
        /// 必殺技の選択を通知
        /// </summary>
        private void HandleSpecial () => NotifySelection(BattleCommand.Special);

        /// <summary>
        /// 操作可能な場合だけコマンド選択を通知
        /// </summary>
        private void NotifySelection(BattleCommand command)
        {
            if (!isActiveAndEnabled || !_isInputEnabled || !_availableCommands.Contains(command))
            {
                return;
            }

            CommandSelected?.Invoke(command);
        }

        /// <summary>
        /// 選択済みの場合だけ決定を通知
        /// </summary>
        private void HandleConfirm()
        {
            if (!isActiveAndEnabled || !_isInputEnabled
                || !_selectedCommand.HasValue
                || !_availableCommands.Contains(_selectedCommand.Value))
            {
                return;
            }

            ConfirmRequested?.Invoke();
        }

        /// <summary>
        /// 選択済みの場合だけ取消を通知
        /// </summary>
        private void HandleCancel()
        {
            if (!isActiveAndEnabled || !_isInputEnabled || !_selectedCommand.HasValue)
            {
                return;
            }

            CancelRequested?.Invoke();
        }

        /// <summary>
        /// キー・パッドの取消入力を、取消ボタンと共通の処理へ接続
        /// </summary>
        /// <param name="eventData">ボタンから渡された取消イベント</param>
        public void OnCancel(BaseEventData eventData)
        {
            if (eventData == null || eventData.used
                || !isActiveAndEnabled || !_isInputEnabled || !_selectedCommand.HasValue)
            {
                return;
            }

            eventData.Use();
            HandleCancel();
        }

        /// <summary>
        /// キャラクターの状態を表示用文字列へ変換
        /// </summary>
        private static string FormatCombatant (CombatantState state)
        {
            if (state == null)
            {
                return string.Empty;
            }

            return $"HP：{state.Hp} / {state.MaxHp}\n"
                + $"エネルギー：{state.Energy} / {state.MaxEnergy}\n"
                + (state.IsGuarding ? "防御中" : string.Empty);
        }

        /// <summary>
        /// コマンドの表示名を取得
        /// </summary>
        private static string GetCommandName (BattleCommand command)
        {
            return command switch
            {
                BattleCommand.Attack => "攻撃",
                BattleCommand.Guard => "防御",
                BattleCommand.Charge => "チャージ",
                BattleCommand.Special => "必殺技",
                BattleCommand.Heal => "回復",
                _ => "不明"
            };
        }

        /// <summary>
        /// テキストが設定されている場合に表示を更新
        /// </summary>
        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.SetText(text);
            }
        }

        /// <summary>
        /// ボタンが設定されている場合に操作できるかどうかを更新
        /// </summary>
        private static void SetInteractable (Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        /// <summary>
        /// 指定したボタンのクリック購読を切り替える
        /// </summary>
        private static void SetListener(Button button, UnityAction listener, bool subscribe)
        {
            if (button == null)
            {
                return;
            }

            if (subscribe)
            {

                button.onClick.AddListener(listener);
            }
            else
            {
                button.onClick.RemoveListener(listener);
            }
        }
    }
}
