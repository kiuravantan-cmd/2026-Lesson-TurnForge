using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TF.UI.Result
{
    /// <summary>
    /// 戦闘結果の表示と、結果画面の操作通知を担当
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultView : MonoBehaviour, IResultView
    {
        /// <summary>
        /// 勝敗を表示するテキスト
        /// </summary>
        [SerializeField] private TextMeshProUGUI _resultText;

        /// <summary>
        /// 決着したターン番号を表示するテキスト
        /// </summary>
        [SerializeField] private TextMeshProUGUI _turnText;

        /// <summary>
        /// 再戦を指示するボタン
        /// </summary>
        [SerializeField] private UnityEngine.UI.Button _retryButton;

        /// <summary>
        /// タイトルへ戻るボタン
        /// </summary>
        [SerializeField] private UnityEngine.UI.Button _returnToTitleButton;

        /// <summary>
        /// Presenterから指定された入力受付状態
        /// </summary>
        private bool _isInputEnabled;

        /// <summary>
        /// 再戦が指示されたときに通知
        /// </summary>
        public event UnityAction RetryRequested;

        /// <summary>
        /// タイトルへ戻る操作が指示されたときに通知
        /// </summary>
        public event UnityAction ReturnToTitleRequested;

        /// <summary>
        /// ボタンの操作通知を登録し、入力受付状態を更新
        /// </summary>
        private void OnEnable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(HandleRetryClicked);
            }
            else
            {
                Debug.LogError("再戦ボタンが設定されていません。", this);
            }

            if (_returnToTitleButton != null)
            {
                _returnToTitleButton.onClick.AddListener(HandleReturnToTitleClicked);
            }
            else
            {
                Debug.LogError("タイトルへ戻るボタンが設定されていません。", this);
            }

            SetInputEnabled(true);
        }

        /// <summary>
        /// ボタンの操作通知を解除
        /// </summary>
        private void OnDisable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(HandleRetryClicked);
            }

            if (_returnToTitleButton != null)
            {
                _returnToTitleButton.onClick.RemoveListener(HandleReturnToTitleClicked);
            }
        }

        /// <summary>
        /// 勝敗のメッセージと決着したターン番号を表示
        /// </summary>
        /// <param name="resultMessage">勝利・敗北などの表示文</param>
        /// <param name="turnNumber">決着したターン番号</param>
        public void Render(string resultMessage, int turnNumber)
        {
            if (_resultText != null)
            {
                _resultText.SetText(resultMessage ?? string.Empty);
            }

            if (_turnText != null)
            {
                _turnText.SetText($"決着ターン：{turnNumber}");
            }
        }

        /// <summary>
        /// 再戦とタイトルへ戻る操作の受付を切り替える
        /// </summary>
        /// <param name="enabled">操作を受け付けるか</param>
        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;

            if (_retryButton != null)
            {
                _retryButton.interactable = enabled;
            }

            if (_returnToTitleButton != null)
            {
                _returnToTitleButton.interactable = enabled;
            }
        }

        /// <summary>
        /// 操作可能な場合に再戦指示を通知
        /// </summary>
        private void HandleRetryClicked()
        {
            if (!isActiveAndEnabled || !_isInputEnabled ||
                _retryButton == null || !_retryButton.IsInteractable())
            {
                return;
            }

            RetryRequested?.Invoke();
        }

        /// <summary>
        /// 操作可能な場合にタイトルへ戻る指示を通知
        /// </summary>
        private void HandleReturnToTitleClicked()
        {
            if (!isActiveAndEnabled || !_isInputEnabled ||
                _returnToTitleButton == null || !_returnToTitleButton.IsInteractable())
            {
                return;
            }

            ReturnToTitleRequested?.Invoke();
        }
    }
}
