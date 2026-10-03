namespace TF.UI.GameFlow
{
    /// <summary>
    /// ゲーム全体の画面表示を切り替える
    /// </summary>
    public interface IGameScreenView
    {
        /// <summary>
        /// 全ての画面を非表示
        /// </summary>
        void HideAll();

        /// <summary>
        /// ローディング画面表示
        /// </summary>
        /// <param name="message">案内文</param>
        void ShowLoading(string message);

        /// <summary>
        /// タイトル画面表示
        /// </summary>
        void ShowTitle();
        
        /// <summary>
        /// 戦闘画面表示
        /// </summary>
        void ShowBattle();

        /// <summary>
        /// 結果画面表示
        /// </summary>
        void ShowResult();

        /// <summary>
        /// エラー画面表示
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        void ShowError(string message);
    }
}