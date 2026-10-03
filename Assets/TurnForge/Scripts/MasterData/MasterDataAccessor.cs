using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TF.MasterData
{
    public sealed class MasterDataAccessor : MonoBehaviour
    {
        /// <summary>
        /// 外部からアクセスするためのインスタンス
        /// </summary>
        public static MasterDataAccessor Instance { get; private set; }

        /// <summary>
        /// あらゆる型の辞書を「レコードの型（Type）」をキーにして一括で保持する
        /// </summary>
        private readonly Dictionary<Type, object> _masterDataDictionaries = new Dictionary<Type, object>();
        
        /// <summary>
        /// 解放対象のAddressablesロードハンドル
        /// </summary>
        private readonly List<AsyncOperationHandle> _loadHandles = new List<AsyncOperationHandle>();
        
        /// <summary>
        /// レコードの型ごとに登録された読み込み処理
        /// </summary>
        private readonly Dictionary<Type, Func<UniTask<bool>>> _loaders = new Dictionary<Type, Func<UniTask<bool>>>();

        /// <summary>
        /// 初期化を開始したことがあるか
        /// </summary>
        private bool _isInitializationStarted = false;
        
        /// <summary>
        /// 初期化処理が進行中か
        /// </summary>
        private bool _isInitializing = false;

        /// <summary>
        /// このコンポーネントが破棄されたか
        /// </summary>
        private bool _isDestroyed = false;

        /// <summary>
        /// 全マスタが正常に読み込まれ、利用可能になったか
        /// </summary>
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async UniTask<bool> InitializeAsync()
        {
            if (_isDestroyed || Instance != this)
            {
                return false;
            }

            if (_isInitializationStarted)
            {
                await UniTask.WaitUntil(() => _isDestroyed || !_isInitializing);
                return !_isDestroyed && IsInitialized;
            }

            if (_loaders.Count == 0)
            {
                Debug.LogError("読み込むマスタが登録されていません。", this);
                return false;
            }

            _isInitializationStarted = true;
            _isInitializing = true;

            // 登録された読み込み処理を順番に実行する
            foreach (var loader in _loaders.Values)
            {
                // 1種類のマスタの読み込み結果
                bool succeeded = await loader();

                if (_isDestroyed)
                {
                    return false;
                }

                if (!succeeded)
                {
                    return FailInitialization();
                }
            }

            IsInitialized = true;
            _isInitializing = false;

            Debug.Log("全てのマスターデータの読み込みが完了しました。");
            return true;
        }
        
        /// <summary>
        /// 初期化前に、読み込むマスタの型とラベルを登録
        /// </summary>
        public bool Register<TAsset, TRecord>(string label)
            where TAsset : ScriptableObject, IMasterDataContainer<TRecord>
            where TRecord : class, IMasterData
        {
            if (_isDestroyed || _isInitializationStarted || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            // 同じレコード型の二重登録を防ぐ
            Type recordType = typeof(TRecord);

            if (_loaders.ContainsKey(recordType))
            {
                return false;
            }

            // この時点では読み込まず、実行する処理だけ登録する
            _loaders.Add(recordType, () => LoadRegisteredAsync<TAsset, TRecord>(label));

            return true;
        }

        /// <summary>
        /// 登録されたマスタを読み込み、公開前の辞書へ保存
        /// </summary>
        private async UniTask<bool> LoadRegisteredAsync<TAsset, TRecord>(string label)
            where TAsset : ScriptableObject, IMasterDataContainer<TRecord>
            where TRecord : class, IMasterData
        {
            // 既存の汎用ロード処理で取得する。
            Dictionary<ulong, TRecord> records = await LoadAsync<TAsset, TRecord>(label);

            if (_isDestroyed || records == null)
            {
                return false;
            }

            // 全件成功まではIsInitializedがfalseなので外部取得できない
            _masterDataDictionaries[typeof(TRecord)] = records;
            return true;
        }

        /// <summary>
        /// ジェネリクスを用いた汎用ロード処理
        /// TAssetはSO、TRecordはレコードデータであることをインターフェースで保証する
        /// </summary>
        private async UniTask<Dictionary<ulong, TRecord>>LoadAsync<TAsset, TRecord>(string label)
            where TAsset : ScriptableObject, IMasterDataContainer<TRecord>
            where TRecord : class, IMasterData
        {
            // 読み込み結果と解放に使用するハンドル
            AsyncOperationHandle<IList<TAsset>> handle = Addressables.LoadAssetsAsync<TAsset>(label, null, false);
            
            _loadHandles.Add(handle);
            
            // 破棄された場合は、解放済みハンドルにアクセスせず待機を終える
            await UniTask.WaitUntil(() => _isDestroyed || handle.IsDone);

            if (_isDestroyed)
            {
                return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"マスタデータの読み込みに失敗しました：{label}", this);
                return null;
            }
            
            // 読み込まれたコンテナ一覧
            IList<TAsset> assets = handle.Result;
            if (assets == null || assets.Count == 0)
            {
                Debug.LogError($"アセットがありません：{label}", this);
                return null;
            }
            
            // この型のレコードを保持する辞書
            var records = new Dictionary<ulong, TRecord>();
            
            // 読み込み対象の各コンテナ
            foreach (var asset in assets)
            {
                if (asset == null || asset.Records == null)
                {
                    Debug.LogError($"コンテナまたはRecordsが未設定です：{label}", this);
                    return null;
                }
                
                // コンテナ内の各レコード
                foreach (var record in asset.Records)
                {
                    if (record == null)
                    {
                        Debug.LogError($"nullのレコードがあります：{label}", this);
                        return null;
                    }

                    if (records.ContainsKey(record.Id))
                    {
                        Debug.LogError($"IDが重複しています：{label} / {record.Id}", this);
                        return null;
                    }

                    records.Add(record.Id, record);
                }
            }

            if (records.Count == 0)
            {
                Debug.LogError($"レコードがありません：{label}", this);
                return null;
            }

            return records;
        }

        /// <summary>
        /// 初期化済みの型別辞書を取得
        /// </summary>
        private bool TryGetDictionary<TRecord>(out Dictionary<ulong, TRecord> records) where TRecord : class, IMasterData
        {
            records = null;
            
            if (!IsInitialized || _isDestroyed)
            {
                return false;
            }

            // 指定した型で登録された辞書
            if (!_masterDataDictionaries.TryGetValue(typeof(TRecord), out var dictionary))
            {
                return false;
            }
            
            records = dictionary as Dictionary<ulong, TRecord>;
            return records != null;
        }

        /// <summary>
        /// 初期化失敗時に、途中まで読み込んだデータを解放
        /// </summary>
        private bool FailInitialization()
        {
            ReleaseLoadedData();
            _isInitializing = false;
            return false;
        }
            
        /// <summary>
        /// 初期化済みの型別辞書を取得
        /// </summary>
        public bool TryGetById<TRecord>(ulong id, out TRecord record) where TRecord : class, IMasterData
        {
            record = null;
            return TryGetDictionary<TRecord>(out var records) 
                && records.TryGetValue(id, out record);
        }

        /// <summary>
        /// 型とIDを指定して、該当するマスターデータを1つ取得
        /// 使い方： accessor.GetById<EnemyDataRecord>(101);
        /// </summary>
        public TRecord GetById<TRecord>(ulong id) where TRecord : class, IMasterData
        {
            return TryGetById(id,  out TRecord record) ? record : null;
        }

        /// <summary>
        /// 型を指定して、その型のすべてのマスターデータを取得する
        /// </summary>
        public IReadOnlyCollection<TRecord> GetAll<TRecord>()  where TRecord : class, IMasterData
        {
            if (TryGetDictionary<TRecord>(out var records))
            {
                return records.Values;
            }
            
            return Array.Empty<TRecord>();
        }

        public TRecord GetRandom<TRecord>()  where TRecord : class, IMasterData
        {
            if (!TryGetDictionary<TRecord>(out var records) || records.Count == 0)
            {
                return null;
            }
            
            int index = UnityEngine.Random.Range(0, records.Count);
            return records.Values.ElementAt(index);
        }

        public IEnumerable<TRecord> Where<TRecord>(Func<TRecord, bool> predicate) where TRecord : class, IMasterData
        {
            return predicate != null
                ? GetAll<TRecord>().Where(predicate)
                : Array.Empty<TRecord>();
        }

        public TRecord First<TRecord>(Func<TRecord, bool> predicate = null) where TRecord : class, IMasterData
        {
            return predicate != null
                ? GetAll<TRecord>().FirstOrDefault(predicate)
                : GetAll<TRecord>().FirstOrDefault();
        }

        public bool Any<TRecord>(ulong id) where TRecord : class, IMasterData
        {
            return TryGetById<TRecord>(id, out _);
        }

        public bool Any<TRecord>(Func<TRecord, bool> predicate) where TRecord : class, IMasterData
        {
            return predicate != null && GetAll<TRecord>().Any(predicate);
        }

        /// <summary>
        /// 型を指定して、その型のすべてのマスターデータの数を取得する
        /// </summary>
        public int Count<TRecord>() where TRecord : class, IMasterData
        {
            return GetAll<TRecord>().Count();
        }

        public int Count<TRecord>(Func<TRecord, bool>predicate) where TRecord : class, IMasterData
        {
            return predicate == null ? 0 : GetAll<TRecord>().Count(predicate);
        }

        /// <summary>
        /// データの公開を終了し、全ロードハンドルを解放
        /// </summary>
        private void ReleaseLoadedData()
        {
            IsInitialized = false;
            _masterDataDictionaries.Clear();

            // ロードと逆順にハンドルを解放する
            for (int index = _loadHandles.Count - 1; index >= 0; index--)
            {
                AsyncOperationHandle handle = _loadHandles[index];
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            
            _loadHandles.Clear();
        }

        /// <summary>
        /// 破棄を通知し、ロード資源と共有インスタンスを解放
        /// </summary>
        private void OnDestroy()
        {
            _isDestroyed = true;
            _isInitializing = false;
            
            ReleaseLoadedData();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
