using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TF.UI.Common
{
    /// <summary>
    /// 標準ボタンの機能に、親画面への取消通知を追加
    /// </summary>
    [AddComponentMenu("UI/TurnForge/UI Button")]
    [DisallowMultipleComponent]
    public sealed class UiButton : Button, ICancelHandler
    {
        /// <summary>
        /// 取消入力を、親階層で最初に見つかった受付先へ渡す
        /// </summary>
        /// <param name="eventData">取消入力のイベント情報</param>
        public void OnCancel (BaseEventData eventData)
        {
            if (!IsActive()
                || !IsInteractable()
                || eventData == null
                || eventData.used)
            {
                return;
            }

            // 自分自身への再通知を避け、親から探索する
            Transform parent = transform.parent;

            if (parent == null)
            {
                return;
            }

            ExecuteEvents.ExecuteHierarchy(
                parent.gameObject,
                eventData,
                ExecuteEvents.cancelHandler);
        }

#if UNITY_EDITOR
        /// <summary>
        /// コンポーネント追加・リセット時に共通の初期設定を適用
        /// </summary>
        protected override void Reset ()
        {
            base.Reset();

            // 色によってボタンの状態を表示する
            transition = Transition.ColorTint;

            // 選択中のボタンを水色で区別する
            UnityEngine.UI.ColorBlock buttonColors = colors;
            buttonColors.selectedColor = new Color32(120, 200, 255, 255);
            colors = buttonColors;

            // 位置関係から移動先を自動探索する
            UnityEngine.UI.Navigation buttonNavigation = navigation;
            buttonNavigation.mode = UnityEngine.UI.Navigation.Mode.Automatic;
            navigation = buttonNavigation;
        }
#endif
    }
}
