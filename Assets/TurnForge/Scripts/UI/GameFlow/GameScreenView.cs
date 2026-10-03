using UnityEngine;

namespace TF.UI.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class GameScreenView : MonoBehaviour, IGameScreenView
    {
        /// <summary>
        /// ローディング画面のルート
        /// </summary>
        [SerializeField] private GameObject _loadingScreen;
        
        /// <summary>
        /// タイトル画面のルート
        /// </summary>
        [SerializeField] private GameObject _titleScreen;
        
        /// <summary>
        /// 戦闘画面のルート
        /// </summary>
        [SerializeField] private GameObject _battleScreen;
        
        /// <summary>
        /// リザルト画面のルート
        /// </summary>
        [SerializeField] private GameObject _resultScreen;
        
        /// <summary>
        /// エラー画面のルート
        /// </summary>
        [SerializeField] private GameObject _errorScreen;
        
        /// <summary>
        /// ローディングの案内文
        /// </summary>
        [SerializeField] private TMPro.TextMeshProUGUI _loadingMessage;
        
        /// <summary>
        /// エラーメッセージ
        /// </summary>
        [SerializeField] private TMPro.TextMeshProUGUI _errorMessage;

        /// <summary>
        /// 全ての画面を非表示
        /// </summary>
        public void HideAll()
        {
            ShowOnly(null);
        }

        /// <summary>
        /// ローディング画面表示
        /// </summary>
        /// <param name="message">案内文</param>
        public void ShowLoading(string message)
        {
            if (_loadingMessage != null)
            {
                var text = message ?? string.Empty;
                _loadingMessage.SetText(text);
            }
            
            ShowOnly(_loadingScreen);
        }

        /// <summary>
        /// タイトル画面表示
        /// </summary>
        public void ShowTitle()
        {
            ShowOnly(_titleScreen);
        }

        /// <summary>
        /// 戦闘画面表示
        /// </summary>
        public void ShowBattle()
        {
            ShowOnly(_battleScreen);
        }
        
        /// <summary>
        /// 結果画面表示
        /// </summary>
        public void ShowResult()
        {
            ShowOnly(_resultScreen);
        }

        /// <summary>
        /// エラー画面を表示
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public void ShowError(string message)
        {
            if (_errorMessage != null)
            {
                var text = message ?? string.Empty;
                _errorMessage.SetText(text);
            }
            
            ShowOnly(_errorScreen);
        }

        /// <summary>
        /// 指定した画面だけを表示する。nullなら全て非表示
        /// </summary>
        private void ShowOnly(GameObject target)
        {
            // 先に対象外の画面を閉じる
            HideIfDifferent(_loadingScreen, target);
            HideIfDifferent(_titleScreen, target);
            HideIfDifferent(_battleScreen, target);
            HideIfDifferent(_resultScreen, target);
            HideIfDifferent(_errorScreen, target);

            // 表示中の画面を再指定しても、無効化と再有効化を行わない
            if (target != null && !target.activeSelf)
            {
                target.SetActive(true);
            }
        }

        /// <summary>
        /// 表示対象ではない画面を非表示にする
        /// </summary>
        private static void HideIfDifferent(GameObject screen, GameObject target)
        {
            if (screen != null && screen != target && screen.activeSelf)
            {
                screen.SetActive(false);
            }
        }
    }
}