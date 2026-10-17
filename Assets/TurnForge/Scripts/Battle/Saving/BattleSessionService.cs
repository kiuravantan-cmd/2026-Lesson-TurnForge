using TF.Battle.Models;
using TF.Infrastructure.Saving;

namespace TF.Battle.Saving
{
    /// <summary>
    /// 自分のターン開始時の状態を保存し、再開する
    /// </summary>
    public sealed class BattleSessionService
    {
        /// <summary>
        /// 途中のバトルを識別するキー
        /// </summary>
        private const string SaveKey = "battle_session";

        /// <summary>
        /// ファイルの読み書きを頼む相手
        /// </summary>
        private readonly ISaveStorage _storage;

        /// <summary>
        /// 現在保持する保存データ
        /// </summary>
        public BattleSessionSaveData CurrentData { get; private set; }

        /// <summary>
        /// 起動時の読み込みが終わったか
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// まだファイルへ保存できていない変更があるか
        /// </summary>
        public bool HasUnsavedChanges { get; private set; }

        /// <summary>
        /// 再開できる途中のデータを持つか
        /// </summary>
        public bool HasResumeData => CurrentData != null && !CurrentData.IsFinished;

        /// <summary>
        /// 保存処理を受け取る
        /// </summary>
        public BattleSessionService(ISaveStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// 初回と失敗を分け、保存データを読み込む
        /// </summary>
        public bool TryLoad()
        {
            if (_storage == null) {
                return false;
            }

            if (IsInitialized) {
                return true;
            }

            if (!_storage.TryLoad<BattleSessionSaveData>(SaveKey, out var data, out var status)) {
                if (status == SaveLoadStatus.Failed) {
                    return false;
                }

                if (status == SaveLoadStatus.NotFound) {
                    IsInitialized = true;
                    return true;
                }
            }

            if (!IsValidData(data)) {
                return false;
            }

            CurrentData = data;
            IsInitialized = true;
            return true;
        }

        /// <summary>
        /// マスタ由来の初期状態へ、保存した可変値を戻す
        /// </summary>
        public bool TryRestore(BattleState initialState, ulong firstId, ulong secondId, out BattleState state)
        {
            state = null;
            if (!IsInitialized || 
                initialState?.FirstCombatant == null || initialState.SecondCombatant == null) {
                return false;
            }

            if (!HasResumeData || !IsValidData(CurrentData)) {
                return false;
            }

            if (CurrentData.FirstCombatantId != firstId || CurrentData.SecondCombatantId != secondId) {
                return false;
            }

            CombatantState first = initialState.FirstCombatant;
            CombatantState second = initialState.SecondCombatant;

            if (CurrentData.FirstHp > first.MaxHp || CurrentData.SecondHp > second.MaxHp ||
                CurrentData.FirstEnergy > first.MaxEnergy || CurrentData.SecondEnergy > second.MaxEnergy)
            {
                return false;
            }

            state = new BattleState(
                new CombatantState(BattleSide.First, CurrentData.FirstHp, first.MaxHp,
                CurrentData.FirstEnergy, first.MaxEnergy, CurrentData.FirstIsGuarding),
                new CombatantState(BattleSide.Second, CurrentData.SecondHp, second.MaxHp,
                CurrentData.SecondEnergy, second.MaxEnergy, CurrentData.SecondIsGuarding),
                CurrentData.ActionSide, CurrentData.TurnNumber, CurrentData.IsFinished);

            return true;
        }

        /// <summary>
        /// 入力受付中の自分の番、または戦闘終了を保存用に写す
        /// </summary>
        public bool TryCapture(BattleState state, ulong firstId, ulong secondId)
        {
            if (!IsInitialized || state?.FirstCombatant == null || state.SecondCombatant == null) {
                return false;
            }

            var data = new BattleSessionSaveData(state, firstId, secondId);
            if (!IsValidData(data)) {
                return false;
            }

            CurrentData = data;
            HasUnsavedChanges = true;
            return true;
        }

        /// <summary>
        /// 成功した場合だけ、未保存の印を下ろす
        /// </summary>
        public bool TrySave()
        {
            if (!IsInitialized || _storage == null) {
                return false;
            }

            if (!HasUnsavedChanges) {
                return false;
            }

            if (!IsValidData(CurrentData)) {
                return false;
            }

            if (!_storage.TrySave(SaveKey, CurrentData)) {
                return false;
            }

            HasUnsavedChanges = false;
            return true;
        }

        /// <summary>
        /// 対応形式と保存可能な区切りの値か調べる
        /// </summary>
        private static bool IsValidData(BattleSessionSaveData data)
        {
            if (data == null || data.Version != BattleSessionSaveData.CurrentVersion
                || data.FirstCombatantId == 0 || data.SecondCombatantId == 0
                || data.FirstHp < 0 || data.SecondHp < 0
                || data.FirstEnergy < 0 || data.SecondEnergy < 0
                || data.TurnNumber < 1 || (!data.IsFinished && data.TurnNumber == int.MaxValue)
                || (data.ActionSide != BattleSide.First && data.ActionSide != BattleSide.Second))
            {
                return false;
            }

            // 終了データは再開しない印。途中データは自分の番だけ。
            return data.IsFinished
                ? data.FirstHp == 0 || data.SecondHp == 0
                : data.ActionSide == BattleSide.First && data.FirstHp > 0 && data.SecondHp > 0;
        }
    }
}
