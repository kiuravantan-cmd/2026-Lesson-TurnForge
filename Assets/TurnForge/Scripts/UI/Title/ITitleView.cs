using UnityEngine.Events;

namespace TF.UI.Title
{
    /// <summary>
    /// タイトル画面の操作通知と、入力受付の切り替えを定義
    /// </summary>
    public interface ITitleView
    {
        /// <summary>
        /// 新しい戦闘の開始が指示されたときに通知
        /// </summary>
        event UnityAction StartRequested;
        
        /// <summary>
        /// 開始操作を受け付けるか設定
        /// </summary>
        void SetInputEnabled(bool enabled);
    }
}