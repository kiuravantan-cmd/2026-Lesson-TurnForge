using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TF.UI.Title
{
    /// <summary>
    /// タイトルの開始操作と、ボタンの入力受付を管理
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleView : MonoBehaviour, ITitleView
    {
        /// <summary>
        /// 新しい戦闘を開始するボタン
        /// </summary>
        [SerializeField] private Button _startButton;

        /// <summary>
        /// 再開ボタン
        /// </summary>
        [SerializeField] private Button _resumeButton;

        /// <summary>
        /// Presenterから指定された入力受付状態
        /// </summary>
        private bool _isInputEnabled = false;

        /// <summary>
        /// 再開できる保存データがあるか
        /// </summary>
        private bool _isResumeAvailable;

        /// <summary>
        /// 新しい戦闘の開始が指示されたときに通知
        /// </summary>
        public event UnityAction StartRequested;

        public event UnityAction ResumeRequested;

        /// <summary>
        /// 画面が有効化されるときに、ボタンのクリックイベントを登録
        /// </summary>
        private void OnEnable()
        {
            if (_startButton == null || _resumeButton == null)
            {
                Debug.LogError("開始または再開ボタンが設定されていません", this);
                return;
            }

            _startButton.onClick.AddListener(HandleStartClicked);
            _resumeButton.onClick.AddListener(HandleResumeClicked);
            RefreshButtons();
        }

        /// <summary>
        /// 画面が無効化されるときに、ボタンのクリックイベントを解除
        /// </summary>
        private void OnDisable ()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (_resumeButton != null) {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }
        }

        /// <summary>
        /// Presenterから指定された入力受付状態を更新
        /// </summary>
        public void SetInputEnabled(bool isEnabled)
        {
            _isInputEnabled = isEnabled;
            RefreshButtons();
        }

        public void SetResumeAvailable(bool available)
        {
            _isResumeAvailable = available;
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            if (_startButton != null) {
                _startButton.interactable = _isInputEnabled;
            }

            if (_resumeButton != null) {
                _resumeButton.interactable = _isInputEnabled && _isResumeAvailable;
            }
        }

        /// <summary>
        /// 開始操作を受け付けるか設定
        /// </summary>
        private void HandleStartClicked()
        {
            if (!isActiveAndEnabled || !_isInputEnabled || _startButton == null || !_startButton.IsInteractable())
            {
                return;
            }

            StartRequested?.Invoke();
        }

        private void HandleResumeClicked()
        {
            if (!isActiveAndEnabled || !_isInputEnabled || !_isResumeAvailable ||
                _resumeButton == null || !_resumeButton.IsInteractable())
            {
                return;
            }

            ResumeRequested?.Invoke();
        }
    }
}
