using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TF.Battle;
using TF.Battle.AI;
using TF.Battle.Factories;
using TF.Battle.Flow;
using TF.Battle.Models;
using TF.Battle.Preparation;
using TF.Battle.Rules;
using TF.Battle.Saving;
using TF.GameFlow;
using TF.Infrastructure.Saving;
using TF.Infrastructure.Updating;
using TF.MasterData;
using TF.UI.Battle;
using TF.UI.Common;
using TF.UI.GameFlow;
using TF.UI.Result;
using TF.UI.Title;

namespace TF.Composition
{
    /// <summary>
    /// ゲームの依存関係を組み立て、終了時に解放する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameLoopRunner))]
    public class GameCompositionRoot : MonoBehaviour
    {
        /// <summary>
        /// 更新登録を解除するためのハンドル
        /// </summary>
        private readonly List<IDisposable> _registrations = new List<IDisposable>();
        
        /// <summary>
        ///  Unityの更新イベントの受け取り口
        /// </summary>
        [SerializeField] private GameLoopRunner _runner;

        /// <summary>
        /// ゲーム全体の画面切り替え
        /// </summary>
        [SerializeField] private GameScreenView _gameScreenView;

        /// <summary>
        /// タイトル画面の操作受付
        /// </summary>
        [SerializeField] private TitleView _titleView;

        /// <summary>
        /// 戦闘画面の操作受付と表示
        /// </summary>
        [SerializeField] private BattleView _battleView;

        /// <summary>
        /// 結果画面の操作受付と表示
        /// </summary>
        [SerializeField] private ResultView _resultView;

        /// <summary>
        /// 各画面のUI選択を管理するコンポーネント
        /// </summary>
        [SerializeField]
        private UiSelectionController[] _uiSelectionControllers =
             new UiSelectionController[0];

        /// <summary>
        /// 1人目に使用するキャラクターマスタのID
        /// </summary>
        [SerializeField] private ulong _firstCombatantId = 1;
        
        /// <summary>
        /// 2人目に使用するキャラクターマスタのID
        /// </summary>
        [SerializeField] private ulong _secondCombatantId = 1;

        /// <summary>
        /// 更新対象と実行順を管理
        /// </summary>
        private UpdateScheduler _scheduler;

        /// <summary>
        /// このRootがRunnerを初期化したか
        /// </summary>
        private bool _ownsRunner;
        
        /// <summary>
        /// 現在のHPや行動する番を覚えておくモデル
        /// </summary>
        private BattleModel _battleModel;

        /// <summary>
        /// 入力受付と演出待ちを管理する進行処理
        /// </summary>
        private BattleFlowController _battleFlow;

        /// <summary>
        /// このRootの解放処理が行われたか
        /// </summary>
        private bool _isReleased = false;

        /// <summary>
        /// ーム全体の進行管理
        /// </summary>
        private GameFlowController _gameFlow;
        
        /// <summary>
        /// 戦闘開始前の準備管理
        /// </summary>
        private BattleLoadingController _battleLoading;
        
        /// <summary>
        /// 戦闘開始処理が進行中か
        /// </summary>
        private bool _isStartingBattle = false;

        /// <summary>
        /// ゲーム状態を画面に表示するPresenter
        /// </summary>
        private GameScreenPresenter _gameScreenPresenter;

        /// <summary>
        /// タイトル画面の操作受付と、戦闘開始処理を接続するPresenter
        /// </summary>
        private TitlePresenter _titlePresenter;

        /// <summary>
        /// 戦闘結果の表示と再戦・タイトルへの遷移を接続
        /// </summary>
        private ResultPresenter _resultPresenter;

        /// <summary>
        /// 戦闘画面の操作受付・状態表示・結果演出を接続するPresenter
        /// </summary>
        private BattlePresenter _battlePresenter;

        /// <summary>
        /// CPUの行動選択と実行指示を管理
        /// </summary>
        private BattleCpuController _battleCpu;

        /// <summary>
        /// 戦闘終了時にCPUの更新登録を解除するハンドル
        /// </summary>
        private IDisposable _cpuRegistration;

        /// <summary>
        /// 起動中の戦績と保存状態を管理
        /// </summary>
        private BattleRecordService _battleRecordService;

        /// <summary>
        /// 現在のゲーム全体の状態
        /// </summary>
        public GameState CurrentState => _gameFlow?.CurrentState ?? GameState.Inactive;

        private void Awake()
        {
            // 更新基盤とゲームの依存関係を初期化
            if (_runner == null)
            {
                _runner = GetComponent<GameLoopRunner>();
            }
            
            _scheduler = new UpdateScheduler();

            if (_runner != null && _runner.TryInitialize(_scheduler))
            {
                _ownsRunner = true;
            }
            else
            {
                Debug.LogError("更新基盤を初期化できませんでした。", this);
                
                // // 初期化途中で作ったリソースを解放
                Release();
                enabled = false;
            }
        }

        /// <summary>
        /// 更新基盤の初期化後に、戦闘の組み立てを開始する
        /// </summary>
        private void Start()
        {
            if (_isReleased || !_ownsRunner)
            {
                return;
            }

            _gameFlow = new GameFlowController();

            if (_gameScreenView == null)
            {
                Debug.LogError("GameScreenViewが設定されていません。", this);
                Release();
                enabled = false;
                return;
            }

            _gameScreenPresenter = new GameScreenPresenter(_gameFlow, _gameScreenView);

            if (!_gameScreenPresenter.TryInitialize())
            {
                Debug.LogError("GameScreenPresenterを初期化できませんでした。", this);
                Release();
                enabled = false;
                return;
            }

            if (_titleView == null)
            {
                Debug.LogError("TitleViewが設定されていません。", this);
                Release();
                enabled = false;
                return;
            }

            _titlePresenter = new TitlePresenter(_titleView, _gameFlow, TryStartBattleAsync);

            if (!_titlePresenter.TryInitialize())
            {
                Debug.LogError("TitlePresenterを初期化できませんでした。", this);
                Release();
                enabled = false;
                return;
            }

            if (_battleView == null)
            {
                Debug.LogError("BattleViewが設定されていません。", this);
                Release();
                enabled = false;
                return;
            }

            // オフライン用の準備処理を接続
            // TODO LESSON07-01: オンライン用の準備処理を追加し、同一PCの2プロセスを接続する。
            // 第7回・1コマ目: 同一PCの別認証・別保存先の2プロセスをRelayで接続する。
            // IBattlePreparationのオンライン用実装を追加し、接続完了までローディングで待つ。
            // TODO LESSON07-02: 第7回・2コマ目で指示送信・ホストでの計算・確定結果の通知を接続する。
            // クライアントはHPを独自に確定しない。ホストの結果をModelへ反映し、同じ結果を表示する。
            // オンラインではCPUの自動行動を止め、各接続先と操作する陣営を対応させる。
            // TODO LESSON08-02: 切断通知と終了処理を接続する。
            // 第8回・2コマ目: 切断時は入力を止め、案内を出し、通信の購読とリソースを解放する。
            // タイトルへ戻る経路を用意し、途中参加・再接続・ホスト移行は今回の範囲に含めない。
            // TODO LESSON08-03: 通信受付側で送信者と陣営を照合し、重複・古い指示を拒否する。
            // 結果にも順序を識別する番号を持たせ、同じ結果の再適用と古い結果への巻き戻りを防ぐ。
            var preparation = new OfflineBattlePreparation();
            
            _battleLoading = new BattleLoadingController(_gameFlow, preparation);

            if (_resultView == null)
            {
                Debug.LogError("ResultViewが設定されていません。", this);
                Release();
                enabled = false;
                return;
            }

            // 戦闘画面と同じFirstをプレイヤーの陣営として扱う
            _resultPresenter = new ResultPresenter(
                _resultView,
                _gameFlow,
                TryStartBattleAsync,
                BattleSide.First);

            if (!_resultPresenter.TryInitialize())
            {
                Debug.LogError("ResultPresenterを初期化できませんでした。", this);
                Release();
                enabled = false;
                return;
            }

            if (!TryRegisterUISelections())
            {
                Release();
                enabled = false;
                return;
            }

            InitializeGameAsync().Forget();
        }

        /// <summary>
        /// 各画面の選択管理を表示更新の順序で登録
        /// </summary>
        private bool TryRegisterUISelections()
        {
            if (_uiSelectionControllers == null || _uiSelectionControllers.Length == 0)
            {
                Debug.LogError("UI選択管理が設定されていません。", this);
                return false;
            }

            foreach (UiSelectionController controller in _uiSelectionControllers)
            {
                if (controller == null)
                {
                    Debug.LogError("UI選択管理に未設定の要素があります。", this);
                    return false;
                }

                // 戦闘処理や演出の更新後に、操作可能なUIを確認する
                if (!_scheduler.TryRegisterUpdate(controller, out var registration, UpdateOrder.Input))
                {
                    Debug.LogError("UI選択管理の更新登録に失敗しました。", this);
                    return false;
                }

                // ゲーム終了時にまとめて登録を解除
                _registrations.Add(registration);
            }

            return true;
        }

        private async UniTaskVoid InitializeGameAsync()
        {
            if (!_gameFlow.TryBeginStartup())
            {
                return;
            }

            // TODO LESSON03-07: セーブ機能完成後、読み込みと失敗時の扱いを接続する。
            // 第3回・2コマ目: 読み込み完了後に新規開始・再開・失敗時の案内を切り替える。
            // 未保存と破損を区別し、破損データは保護したうえで新規開始を選べるようにする。
            // 配布時は保存を呼び出さず、タイトルへ進める。

            // マスタ読み込みと戦闘作成の結果
            bool succeeded = await LoadMasterDataAsync();
            
            // 待機中に破棄された場合は、そのまま終了する
            if (_isReleased)
            {
                return;
            }

            if (!succeeded)
            {
                Debug.LogError("マスタを初期化できませんでした。", this);
                _gameFlow.TryFailStartup();
                return;
            }
            
            _gameFlow.TryCompleteStartup();
        }
        
        /// <summary>
        /// 使用するマスタを登録し、読み込み完了まで待機
        /// </summary>
        private async UniTask<bool> LoadMasterDataAsync()
        {
            MasterDataAccessor accessor = MasterDataAccessor.Instance;
            if (accessor == null)
            {
                Debug.LogError("MasterDataAccessorが配置されていません。", this);
                return false;
            }
            
            // 初回だけ、このゲームで使用するマスタを登録
            if (!accessor.IsInitialized)
            {
                bool combatantsRegistered =
                    accessor.Register<BattleCombatantData, BattleCombatantDataRecord>("BattleCombatantData");
                
                bool commandRegistered = 
                    accessor.Register<BattleCommandData, BattleCommandDataRecord>("BattleCommandData");

                if (!combatantsRegistered || !commandRegistered)
                {
                    Debug.LogError("マスタを登録できませんでした。登録・初期化の重複を確認してください。",this);
                    return false;
                }
            }
            
            // 初期化済みの場合も、Accessorが保持する結果を受け取る
            bool initialized = await accessor.InitializeAsync();
            if (_isReleased || accessor == null || !initialized)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 読み込み済みマスタから、1戦分のオブジェクトを作る。
        /// </summary>
        // TODO LESSON02-10: 第2回・2〜3コマ目で操作方式の設定と入力切り替え用クラスを追加・接続する。
        // 方式を覚え、選択中の技を解除し、パッド・キーでは使用可能なボタンへフォーカスを置く。
        // TODO LESSON02-11: 第2回・4コマ目で未接続パッドの案内と設定へ戻る操作を接続する。
        // 戦闘入力を止めても、方式を選び直すキーボード・クリックの入口は残す。
        private bool TryComposeBattle()
        {
            // TODO LESSON05-02: この手動の組み立てをVContainerへ移行する。
            // 第5回・2〜3コマ目: LifetimeScopeでModel・Rules・Presenter・CPUなどの依存を登録する。
            // 戦闘ごとにスコープを作成・破棄し、再戦時に状態・購読・CPUを持ち越さない。
            // IBattleCommandSelectorの登録を変え、自作CPUへ差し替える。
            // 戦闘単位の寿命と破棄順を保ち、CPUの実装を登録で差し替える。
            MasterDataAccessor accessor = MasterDataAccessor.Instance;
            if (accessor == null || !accessor.IsInitialized)
            {
                return false;
            }
            
            if (!accessor.TryGetById(_firstCombatantId, out BattleCombatantDataRecord firstCombatant))
            {
                Debug.LogError($"参加者マスタがありません：ID {_firstCombatantId}", this);
                return false;
            }

            if (!accessor.TryGetById(_secondCombatantId, out BattleCombatantDataRecord secondCombatant))
            {
                Debug.LogError($"参加者マスタがありません：ID {_secondCombatantId}", this);
                return false;
            }
            
            // マスタから初期化状態を作るFactory
            var factory = new BattleStateFactory();
            if (!factory.TryCreateInitialState(firstCombatant, secondCombatant, out var initialState))
            {
                Debug.LogError("参加者マスタの初期能力が不正です。", this);
                return false;
            }

            // コマンドマスタを使用する戦闘ルール
            var rules = new BattleRules(accessor.GetAll<BattleCommandDataRecord>());

            if (!rules.IsConfigured)
            {
                Debug.LogError("コマンドマスタの構成が不正です。", this);
                return false;
            }
            
            _battleModel = new BattleModel(rules, initialState);
            _battleFlow = new BattleFlowController(_battleModel);

            // Presenterの初期化の前に、行動受付状態へ進める
            if (!_battleFlow.TryStartBattle())
            {
                Debug.LogError("戦闘の開始に失敗しました。", this);
                return false;
            }

            // オフラインではFirstをプレイヤーの操作側にする
            _battlePresenter = new BattlePresenter(_battleView, _battleModel, _battleFlow, BattleSide.First);
            _battlePresenter.BattleFinished += HandleBattleFinised;

            if (!_battlePresenter.TryInitialize())
            {
                Debug.LogError("BattlePresenterの初期化に失敗しました。", this);
                return false;
            }

            // CPUは通常攻撃を選択
            IBattleCommandSelector selector = new AttackOnlyCommandSelector();

            // プレイヤーと共通の行動実行・演出処理へ接続する
            _battleCpu = new BattleCpuController(
                _battleModel,
                _battleFlow,
                selector,
                BattleSide.Second,
                _battlePresenter.TryExecuteAsync);

            _battleCpu.Failed += HandleCpuFailed;

            // CPUの更新をゲームロジックの実行順で登録
            if (!_scheduler.TryRegisterUpdate(_battleCpu, out _cpuRegistration, UpdateOrder.Simulation))
            {
                Debug.LogError("CPUの更新登録に失敗しました。", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// タイトルまたは結果画面から、準備を経由して戦闘を開始
        /// </summary>
        public async UniTask<bool> TryStartBattleAsync()
        {
            if (_isReleased || _isStartingBattle || _gameFlow == null || _battleLoading == null)
            {
                return false;
            }

            switch (CurrentState)
            {
                case GameState.Title:
                case GameState.Result:
                    break;

                case GameState.Inactive:
                case GameState.Loading:
                case GameState.Battle:
                case GameState.BattleLoading:
                case GameState.Error:
                default:
                    return false;
            }
            
            _isStartingBattle = true;
            
            // 前の戦闘の利用側を片付けてから、アセットを解放
            ReleaseBattle();

            if (!_battleLoading.ReleasePreparatedResources())
            {
                _isStartingBattle = false;
                return false;
            }

            // ローディングへ移り、戦闘に必要な準備を待つ
            bool isPrepared = await _battleLoading.TryPrepareAsync();

            if (_isReleased || !isPrepared)
            {
                _isStartingBattle = false;
                return false;
            }

            if (!TryComposeBattle())
            {
                ReleaseBattle();
                _battleLoading.CancelPreparation();
                _isStartingBattle = false;
                return false;
            }

            if (!_battleLoading.TryEnterBattle())
            {
                ReleaseBattle();
                _battleLoading.CancelPreparation();
                _isStartingBattle = false;
                return false;
            }

            // 戦闘画面への遷移後に、CPUの行動を許可する
            _battleCpu.SetEnabled(true);

            _isStartingBattle = false;
            return true;
        }
        
        /// <summary>
        /// ローディング中の戦闘準備に中断を指示
        /// </summary>
        public void CancelBattlePreparation()
        {
            if (_isReleased)
            {
                return;
            }

            _battleLoading?.CancelPreparation();
        }

        /// <summary>
        /// 1戦分の演出・購読・オブジェクトを片付ける
        /// </summary>
        private void ReleaseBattle()
        {
            // CPUの更新登録を先に解除
            _cpuRegistration?.Dispose();
            _cpuRegistration = null;

            // CPUの購読と参照を解放
            if (_battleCpu != null)
            {
                _battleCpu.Failed -= HandleCpuFailed;
                _battleCpu.Dispose();
                _battleCpu = null;
            }

            // 新しい画面操作を止める
            if (_battleView != null)
            {
                _battleView.SetInputEnabled(false);
            }

            // モデルの参照を外す前に、演出の中断と購読解除を行う
            if (_battlePresenter != null)
            {
                _battlePresenter.BattleFinished -= HandleBattleFinised;
                _battlePresenter.Dispose();
                _battlePresenter = null;
            }

            _battleFlow = null;
            _battleModel = null;
        }

        /// <summary>
        /// 最後の演出が完了したら、結果画面へ進む
        /// </summary>
        private void HandleBattleFinised(BattleState state)
        {
            if (_isReleased || state == null || !state.IsFinished || _gameFlow.CurrentState != GameState.Battle)
            {
                return;
            }

            // 結果画面ではCPUの行動を停止する
            if (_battleCpu != null)
            {
                _battleCpu.SetEnabled(false);
            }

            // 結果を設定してから画面を切り替える
            if (_resultPresenter == null || !_resultPresenter.TrySetResult(state))
            {
                Debug.LogError("戦闘結果を設定できませんでした。", this);
                return;
            }

            if (!_gameFlow.TryShowResult())
            {
                Debug.LogError("結果画面へ遷移できませんでした。", this);
            }

            // TODO LESSON03-09: 戦績の加算と保存を接続する。重複加算を防ぐ。
            // 第3回・2コマ目: 終了した状態を一度だけ戦績へ加算し、変更があれば保存する。
        }

        /// <summary>
        /// CPUが停止した原因を出力する
        /// </summary>
        private void HandleCpuFailed(string message)
        {
            if (_isReleased)
            {
                return;
            }

            Debug.LogError($"CPUの行動処理が停止しました：{message}", this);
        }

        /// <summary>
        /// 所有する更新基盤と登録を解放
        /// </summary>
        private void Release()
        {
            if (_isReleased)
            {
                return;
            }
            
            // 非同期処理が完了しても、戦闘を作り直さないようにする
            _isReleased = true;
            
            // 自分が初期化したRunnerだけを停止する
            if (_ownsRunner && _runner != null)
            {
                _runner.Shutdown();
                _ownsRunner = false;
            }

            for (int i = _registrations.Count - 1; i >= 0; i--)
            {
                _registrations[i].Dispose();
            }
            
            _registrations.Clear();
            
            // 利用側を先に終了させてから、準備処理を破棄する。
            ReleaseBattle();

            _battleLoading?.Dispose();
            _battleLoading = null;

            // 今後、画面Presenterの購読解除もここへ追加する。
            _gameScreenPresenter?.Dispose();
            _gameScreenPresenter = null;
            _titlePresenter?.Dispose();
            _titlePresenter = null;
            _resultPresenter?.Dispose();
            _resultPresenter = null;

            _scheduler?.Dispose();
            _scheduler = null;
        }

        /// <summary>
        /// 保存先を組み立て、戦績を初期化
        /// </summary>
        private bool TryInitializeBattleRecord ()
        {
            // TODO LESSON03-07: 保存先とServiceを組み立て、起動時の読み込みを接続する。
            // TODO LESSON03-11: 第3回・3コマ目で戦闘再開用DTOと復元処理を追加する。
            // 両者のHP・エネルギー・防御、番・番号、キャラクターID、操作方式を保存する。
            // マスタから固定値を読み直し、保存データの値とIDを確認してから戦闘状態を復元する。
            // 戦績DTOは勝敗数の保存用。戦闘途中の再開DTOとは分ける。
            // 戦闘途中の状態・キャラクターID・操作設定の保存データも第3回で追加する。
            return false;
        }

        /// <summary>
        /// 未保存の戦績がある場合に保存を試みる
        /// </summary>
        private void SaveBattleRecordIfNeeded ()
        {
            // TODO LESSON03-08: 変更がある場合だけ保存し、失敗時は後で再試行する。
            // 第3回・3コマ目: 戦闘途中の保存は次のターン開始時など、結果が確定した区切りへ接続する。
            // オンラインの途中状態をローカル保存から単独で復元する処理は追加しない。
            // 戦闘途中の保存はターン開始時に接続し、演出途中では保存しない。
        }

        /// <summary>
        /// 第2回のUI接続前に、第1回の通常攻撃をゲーム本体で確認する。
        /// </summary>
        [ContextMenu("授業確認/第1回・プレイヤーの通常攻撃")]
        private void ExecuteLessonAttack()
        {
            if (!Application.isPlaying || _isReleased
                || CurrentState != GameState.Battle
                || _battleModel?.CurrentState == null || _battlePresenter == null)
            {
                Debug.Log("Playモードでタイトルから戦闘画面へ進んでください。", this);
                return;
            }

            // CPUの番や戦闘終了後の指示は、本体のルールで受け付けない。
            var request = new TF.Battle.Commands.BattleActionRequest(
                BattleSide.First, _battleModel.CurrentState.TurnNumber,
                TF.Battle.Commands.BattleCommand.Attack);
            _battlePresenter.TryExecuteAsync(request).Forget();
        }

        /// <summary>
        /// 破棄時に所有するリソースを解放
        /// </summary>
        private void OnDestroy()
        {
            // TODO LESSON03-10: 終了時の未保存データの再保存を接続する。
            // 第3回・2コマ目: 終了前に未保存の変更を保存する。終了時だけの保存に頼らない。
            Release();
        }
    }   
}
