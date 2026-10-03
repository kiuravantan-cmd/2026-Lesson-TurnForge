using UnityEngine.Events;

namespace TF.UI.Result
{
    /// <summary>
    /// 戦闘結果の表示と、結果画面の操作通知を定義
    /// </summary>
    public interface IResultView
    {
        /// <summary>
        /// 再戦が指示されたときに通知
        /// </summary>
        event UnityAction RetryRequested;

        /// <summary>
        /// タイトルへ戻る操作が指示されたときに通知
        /// </summary>
        event UnityAction ReturnToTitleRequested;

        /// <summary>
        /// 勝敗のメッセージと決着したターン番号を表示
        /// </summary>
        /// <param name="resultMessage">勝利・敗北などの表示文</param>
        /// <param name="turnNumber">決着したターン番号</param>
        void Render(string resultMessage, int turnNumber);

        /// <summary>
        /// 再戦とタイトルへ戻る操作の受付を切り替える
        /// </summary>
        /// <param name="enabled">操作を受け付けるか</param>
        void SetInputEnabled(bool enabled);
    }
}
