using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TF.Infrastructure.Updating
{
    public sealed class UpdateScheduler : IDisposable
    {
        /// <summary>
        /// 1種類の更新対象を実行順付きで管理
        /// </summary>
        private sealed class OrderedTickList<T> : IDisposable
            where T : class
        {
            /// <summary>
            /// 更新対象の登録情報と解除処理を保持
            /// </summary>
            private sealed class Entry : IDisposable
            {
                /// <summary>
                /// この登録を監視するリスト
                /// </summary>
                private OrderedTickList<T> _owner;
                
                /// <summary>
                /// 更新対象
                /// 解除後はnullになる
                /// </summary>
                public T Target { get; private set; }
                
                /// <summary>
                /// 数値が小さいほど先に実行する
                /// </summary>
                public int Order { get; }
                
                /// <summary>
                /// 同じ実行順で使用する登録番号
                /// </summary>
                public long Sequence { get; }
                
                /// <summary>
                /// この登録が有効化
                /// </summary>
                public bool IsActive => Target != null;

                /// <summary>
                /// 更新対象と実行順を登録する
                /// </summary>
                public Entry(OrderedTickList<T> owner, T target, int order, long sequence)
                {
                    _owner = owner;
                    Target = target;
                    Order = order;
                    Sequence = sequence;
                }
                
                /// <summary>
                /// 所有者へ登録解除を指示する
                /// </summary>
                public void Dispose()
                {
                    _owner?.Unregister(this);
                }

                /// <summary>
                /// 登録を無効化し、参照を解放する
                /// </summary>
                public void Invalidate()
                {
                    Target = null;
                    _owner = null;
                }
            }
            
            /// <summary>
            /// 登録された対象の一覧
            /// </summary>
            private readonly List<Entry> _entries = new List<Entry>();

            /// <summary>
            /// 実行順の比較処理
            /// </summary>
            private static readonly Comparison<Entry> s_comparison = CompareEntries;
            
            /// <summary>
            /// 解除済みの登録を判定する処理
            /// </summary>
            private static readonly Predicate<Entry> s_inactivePredicate = IsInactive;
            
            /// <summary>
            /// 次の登録を割り当てる通し番号
            /// </summary>
            private long _nextSequence;

            /// <summary>
            /// 並べ替えが必要か
            /// </summary>
            private bool _needsSort;

            /// <summary>
            /// 解除済みの登録を削除する必要があるか
            /// </summary>
            private bool _needsCompaction;

            /// <summary>
            /// このリストを実行中か
            /// </summary>
            private bool _isExecuting;
            
            /// <summary>
            /// このリストが破棄済みか
            /// </summary>
            private bool _isDisposed;
            
            /// <summary>
            /// 対象を登録し、成功時に解除用ハンドルを返す
            /// </summary>
            public bool TryRegister(T target, int order, out IDisposable registration)
            {
                registration = null;
                
                if (_isDisposed)
                {
                    Debug.LogError("更新リストは破棄済みです。");
                    return false;
                }

                if (target == null)
                {
                    Debug.LogError("更新対象が設定されていません。");
                    return false;
                }

                foreach (var entry in _entries)
                {
                    if (entry.IsActive && ReferenceEquals(entry.Target, target))
                    {
                        Debug.LogWarning("同じ対象が同じ更新種別に登録されています。");
                        return false;
                    }
                }
                
                // 実行順と登録順を保持する新しい登録
                var newEntry = new Entry(this, target, order, _nextSequence++);
                
                _entries.Add(newEntry);
                _needsSort = true;

                registration = newEntry;
                return true;
            }

            /// <summary>
            /// 開始時点で登録されている有効な対象を更新
            /// </summary>
            public void Run(UpdateContext context, UnityAction<T, UpdateContext> invoke)
            {
                if (_isDisposed)
                {
                    return;
                }
                
                // 登録内容が変わった場合だけ並べ替える
                if (_needsSort)
                {
                    _entries.Sort(s_comparison);
                    _needsSort = false;
                }
                
                // 今回の実行対象数。更新中の追加は含めない
                int countAtStart = _entries.Count;

                _isExecuting = true;

                try
                {
                    for (int i = 0; i < countAtStart; i++)
                    {
                        if (_isDisposed)
                        {
                            break;
                        }
                    
                        Entry entry = _entries[i];
                        if (!entry.IsActive)
                        {
                            continue;
                        }
                    
                        // 呼び出し中の登録解除に備えて対象を保持
                        T target = entry.Target;
                    
                        invoke(target, context);
                    }
                }
                finally
                {
                    // 更新先でエラーが起きても後始末を行う
                    _isExecuting = false;
                    CompactIfNeeded();
                }
            }

            /// <summary>
            /// 全ての登録を無効化
            /// </summary>
            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }
                
                _isDisposed = true;

                foreach (var entry in _entries)
                {
                    entry.Invalidate();
                }

                _needsCompaction = true;
                
                // 更新中のリスト削除は実行終了まで保留
                if (!_isExecuting)
                {
                    CompactIfNeeded();
                }
            }

            /// <summary>
            /// 指定した登録を解除
            /// </summary>
            private void Unregister(Entry entry)
            {
                if (!entry.IsActive)
                {
                    return;
                }
                
                // 更新中でも、以降の呼び出しは即座に停止する
                entry.Invalidate();
                _needsCompaction = true;

                if (!_isExecuting)
                {
                    CompactIfNeeded();
                }
            }

            /// <summary>
            /// 解除済みの登録をリストから取り除く
            /// </summary>
            private void CompactIfNeeded()
            {
                if (!_needsCompaction)
                {
                    return;
                }

                _entries.RemoveAll(s_inactivePredicate);
                _needsCompaction = false;
            }

            /// <summary>
            /// 実行順を比較し、同じ場合は登録順で比較する
            /// </summary>
            private static int CompareEntries(Entry left, Entry right)
            {
                // 指定された実行順の比較結果
                int orderComparison = left.Order.CompareTo(right.Order);

                if (orderComparison != 0)
                {
                    return orderComparison;
                }
                
                return left.Sequence.CompareTo(right.Sequence);
            }

            /// <summary>
            /// 登録が解除済みかを返す
            /// </summary>
            private static bool IsInactive(Entry entry)
            {
                return !entry.IsActive;
            }
        }

        /// <summary>
        /// 通常更新の登録先
        /// </summary>
        private readonly OrderedTickList<IUpdateTickable> _updates = new OrderedTickList<IUpdateTickable>();
        
        /// <summary>
        /// 固定更新の登録先
        /// </summary>
        private readonly OrderedTickList<IFixedTickable> _fixedUpdates = new OrderedTickList<IFixedTickable>();
        
        /// <summary>
        /// 後処理更新の登録先
        /// </summary>
        private readonly OrderedTickList<ILateTickable> _lateUpdates = new OrderedTickList<ILateTickable>();

        /// <summary>
        /// 通常更新の呼び出しを共有
        /// </summary>
        private static readonly UnityAction<IUpdateTickable, UpdateContext> s_updateInvoker = InvokeUpdate;
        
        /// <summary>
        /// 固定更新の呼び出しを共有
        /// </summary>
        private static readonly UnityAction<IFixedTickable, UpdateContext> s_fixedUpdateInvoker = InvokeFixedUpdate;
        
        /// <summary>
        /// 後処理更新の呼び出しを共有
        /// </summary>
        private static readonly UnityAction<ILateTickable, UpdateContext> s_lateUpdateInvoker =  InvokeLateUpdate;
        
        /// <summary>
        /// 更新を実行中か
        /// </summary>
        private bool _isExecuting;
        
        /// <summary>
        /// 破棄済みか
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 通常更新へ登録し、解除用のハンドルを返す
        /// </summary>
        public bool TryRegisterUpdate(IUpdateTickable target, out IDisposable registration, int order = UpdateOrder.Simulation)
        {
            registration = null;
            
            if (_isDisposed)
            {
                Debug.LogError("既に破棄されています。");
                return false;
            }
            
            return _updates.TryRegister(target, order, out registration);
        }

        /// <summary>
        /// 固定更新へ登録し、解除用のハンドルを返す
        /// </summary>
        public bool TryRegisterFixedUpdate(IFixedTickable target, out IDisposable registration, int order = UpdateOrder.Simulation)
        {
            registration = null;
            
            if (_isDisposed)
            {
                Debug.LogError("既に破棄されています。");
                return false;
            }
            
            return _fixedUpdates.TryRegister(target, order, out registration);
        }

        /// <summary>
        /// 後処理更新へ登録し、解除用のハンドルを返す
        /// </summary>
        public bool TryRegisterLateUpdate(ILateTickable target, out IDisposable registration, int order = UpdateOrder.Simulation)
        {
            registration = null;
            
            if (_isDisposed)
            {
                Debug.LogError("既に破棄されています。");
                return false;
            }
            
            return _lateUpdates.TryRegister(target, order, out registration);
        }

        /// <summary>
        /// 通常更新を実行
        /// </summary>
        public void RunUpdate(UpdateContext context)
        {
            Execute(_updates, context, s_updateInvoker);
        }

        /// <summary>
        /// 固定更新を実行
        /// </summary>
        public void RunFixedUpdate(UpdateContext context)
        {
            Execute(_fixedUpdates, context, s_fixedUpdateInvoker);
        }
        
        /// <summary>
        /// 後処理更新を実行
        /// </summary>
        public void RunLateUpdate(UpdateContext context)
        {
            Execute(_lateUpdates, context, s_lateUpdateInvoker);
        }

        /// <summary>
        /// すべての登録を解除し、使用を終了
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            
            _isDisposed = true;
            
            _updates.Dispose();
            _fixedUpdates.Dispose();
            _lateUpdates.Dispose();
        }

        /// <summary>
        /// 指定した更新リストを実行
        /// </summary>
        private void Execute<T>(OrderedTickList<T> targets, UpdateContext context, UnityAction<T, UpdateContext> invoke)
            where T : class
        {
            if (_isDisposed)
            {
                Debug.LogError("UpdateSchedulerは破棄済みです。");
                return;
            }

            if (_isExecuting)
            {
                Debug.LogError("更新処理の実行中に、別の更新処理を開始できません。");
                return;
            }
            
            _isExecuting = true;

            try
            {
                targets.Run(context, invoke);
            }
            finally
            {
                // 更新先でエラーが起きても実行状態を戻す
                _isExecuting = false;
            }
        }

        /// <summary>
        /// 対象の通常更新を呼び出す
        /// </summary>
        private static void InvokeUpdate(IUpdateTickable target, UpdateContext context)
        {
            target.Tick(context);
        }

        /// <summary>
        /// 対象の固定更新を呼び出す
        /// </summary>
        private static void InvokeFixedUpdate(IFixedTickable target, UpdateContext context)
        {
            target.FixedTick(context);
        }

        /// <summary>
        /// 対象の後処理更新を呼び出す
        /// </summary>
        private static void InvokeLateUpdate(ILateTickable target, UpdateContext context)
        {
            target.LateTick(context);
        }
    }

    
}