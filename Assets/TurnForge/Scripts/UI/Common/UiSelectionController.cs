using System.Linq;
using TF.Infrastructure.Updating;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TF.UI.Common
{
    /// <summary>
    /// 画面内の操作可能なUIへ選択を補い、入力復帰を支援
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSelectionController : MonoBehaviour, IUpdateTickable
    {
        /// <summary>
        /// 選択を補う際の優先順で並べたUI
        /// </summary>
        [SerializeField] private Selectable[] _selectable = new Selectable[0];

        /// <summary>
        /// 現在の選択が使えない場合に、操作可能なUIへ選択を移す
        /// </summary>
        public void Tick(UpdateContext context)
        {
            // 更新登録が残っていても、非表示の画面は処理しない
            if (!isActiveAndEnabled)
            {
                return;
            }

            // シーンで入力を処理しているEventSystem
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null || eventSystem.alreadySelecting)
            {
                return;
            }

            // 現在フォーカスされているオブジェクト
            GameObject selectedObject = eventSystem.currentSelectedGameObject;

            // この画面の有効な選択はそのままにする
            if (TryGetRegisterdSelectable(selectedObject, out var selected) && CanSelect(selected))
            {
                return;
            }

            // 登録順で最初に有効なUIを探す
            Selectable fallback = FindFirstSelectable();

            if (fallback != null)
            {
                eventSystem.SetSelectedGameObject(fallback.gameObject);
                return;
            }

            // この画面の操作不可な選択(CanSelectがfalseだったもの)だけを解除
            if (selected != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void OnDisable ()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null || eventSystem.alreadySelecting)
            {
                return;
            }

            if (TryGetRegisterdSelectable(eventSystem.currentSelectedGameObject, out _))
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// 指定されたオブジェクトが登録済みのUIか
        /// </summary>
        private bool TryGetRegisterdSelectable(GameObject target, out Selectable selectable)
        {
            selectable = null;

            if (target == null || _selectable == null)
            {
                return false;
            }

            foreach (Selectable candidate in _selectable)
            {
                if (candidate != null && candidate.gameObject == target)
                {
                    selectable = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 登録順で最初に操作可能なUIを取得
        /// </summary>
        private Selectable FindFirstSelectable()
        {
            if (_selectable == null)
            {
                return null;
            }

            foreach (Selectable candidate in _selectable)
            {
                if (CanSelect(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// UIが有効かつ操作可能か
        /// </summary>
        private static bool CanSelect(Selectable target)
        {
            return target != null && target.isActiveAndEnabled && target.IsInteractable();
        }
    }
}
