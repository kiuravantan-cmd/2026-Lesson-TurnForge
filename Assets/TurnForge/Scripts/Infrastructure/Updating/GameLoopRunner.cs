using System;
using UnityEngine;

namespace TF.Infrastructure.Updating
{
    /// <summary>
    /// Unityの更新イベントをスケジューラへ転送するクラス
    /// </summary>
    [DisallowMultipleComponent]
    public class GameLoopRunner : MonoBehaviour
    {
        /// <summary>
        /// 更新対象を管理するスケジューラ
        /// </summary>
        private UpdateScheduler _scheduler;

        /// <summary>
        /// スケジューラーを設定し、成功したかを返す
        /// </summary>
        public bool TryInitialize(UpdateScheduler scheduler)
        {
            if (scheduler == null)
            {
                return false;
            }

            if (_scheduler != null)
            {
                Debug.LogWarning("GameLoopRunnerは初期化済みです");
                return false;
            }
            
            _scheduler = scheduler;
            return true;
        }

        /// <summary>
        /// 更新の転送を停止
        /// スケジューラーは破棄しない
        /// </summary>
        public void Shutdown()
        {
            _scheduler = null;
        }

        private void Update()
        {
            if (_scheduler == null)
            {
                return;
            }
            
            // 通常更新を転送
            var context = new UpdateContext(Time.deltaTime, Time.unscaledDeltaTime);
            _scheduler.RunUpdate(context);
        }

        private void FixedUpdate()
        {
            if (_scheduler == null)
            {
                return;
            }

            // 固定間隔の更新を転送
            var context = new UpdateContext(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
            _scheduler.RunFixedUpdate(context);
        }

        private void LateUpdate()
        {
            if (_scheduler == null)
            {
                return;
            }
            
            // 通常更新後の更新を転送
            var context = new UpdateContext(Time.deltaTime, Time.unscaledDeltaTime);
            _scheduler.RunLateUpdate(context);
        }

        private void OnDestroy()
        {
            // 破棄時にスケジューラーへの参照を解放
            Shutdown();
        }
    }
}