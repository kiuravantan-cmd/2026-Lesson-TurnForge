using System;
using TF.Battle.Models;
using UnityEngine;

namespace TF.Battle.Saving
{
    /// <summary>
    /// 途中のバトルをJSONへ保存するデータ
    /// </summary>
    [Serializable]
    public sealed class BattleSessionSaveData
    {
        /// <summary>
        /// 現在対応する保存形式
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// 保存形式の番号
        /// </summary>
        [SerializeField] private int _version;

        /// <summary>
        /// 1人目のマスタID
        /// </summary>
        [SerializeField] private ulong _firstCombatantId;

        /// <summary>
        /// 2人目のマスタID
        /// </summary>
        [SerializeField] private ulong _secondCombatantId;

        /// <summary>
        /// 1人目の現在HP
        /// </summary>
        [SerializeField] private int _firstHp;

        /// <summary>
        /// 2人目の現在HP
        /// </summary>
        [SerializeField] private int _secondHp;

        /// <summary>
        /// 1人目のエネルギー
        /// </summary>
        [SerializeField] private int _firstEnergy;

        /// <summary>
        /// 2人目のエネルギー
        /// </summary>
        [SerializeField] private int _secondEnergy;

        /// <summary>
        /// 1人目の防御状態
        /// </summary>
        [SerializeField] private bool _firstIsGuarding;

        /// <summary>
        /// 2人目の防御状態
        /// </summary>
        [SerializeField] private bool _secondIsGuarding;

        /// <summary>
        /// 現在の番
        /// </summary>
        [SerializeField] private BattleSide _actionSide;

        /// <summary>
        /// ターン番号
        /// </summary>
        [SerializeField] private int _turnNumber;

        /// <summary>
        /// このバトルが終了したか
        /// </summary>
        [SerializeField] private bool _isFinished;

        /// <summary>
        /// 保存形式の番号
        /// </summary>
        public int Version => _version;

        /// <summary>
        /// 1人目のマスタID
        /// </summary>
        public ulong FirstCombatantId => _firstCombatantId;

        /// <summary>
        /// 2人目のマスタID
        /// </summary>
        public ulong SecondCombatantId => _secondCombatantId;

        /// <summary>
        /// 1人目の現在HP
        /// </summary>
        public int FirstHp => _firstHp;

        /// <summary>
        /// 2人目の現在HP
        /// </summary>
        public int SecondHp => _secondHp;

        /// <summary>
        /// 1人目のエネルギー
        /// </summary>
        public int FirstEnergy => _firstEnergy;

        /// <summary>
        /// 2人目のエネルギー
        /// </summary>
        public int SecondEnergy => _secondEnergy;

        /// <summary>
        /// 1人目の防御状態
        /// </summary>
        public bool FirstIsGuarding => _firstIsGuarding;

        /// <summary>
        /// 2人目の防御状態
        /// </summary>
        public bool SecondIsGuarding => _secondIsGuarding;

        /// <summary>
        /// 現在の番
        /// </summary>
        public BattleSide ActionSide => _actionSide;

        /// <summary>
        /// ターン番号
        /// </summary>
        public int TurnNumber => _turnNumber;

        /// <summary>
        /// このバトルが終了したか
        /// </summary>
        public bool IsFinished => _isFinished;

        /// <summary>
        /// 確定した状態とマスタIDを保存用に写す
        /// </summary>
        public BattleSessionSaveData(BattleState state, ulong firstCombatantId, ulong secondCombatantId)
        {
            _version = CurrentVersion;
            _firstCombatantId = firstCombatantId;
            _secondCombatantId = secondCombatantId;
            _firstHp = state.FirstCombatant.Hp;
            _secondHp = state.SecondCombatant.Hp;
            _firstEnergy = state.FirstCombatant.Energy;
            _secondEnergy = state.SecondCombatant.Energy;
            _firstIsGuarding = state.FirstCombatant.IsGuarding;
            _secondIsGuarding = state.SecondCombatant.IsGuarding;
            _actionSide = state.ActionSide;
            _turnNumber = state.TurnNumber;
            _isFinished = state.IsFinished;
        }
    }
}
