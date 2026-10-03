using System;
using System.Collections.Generic;
using UnityEngine;

namespace TF.MasterData
{
    /// <summary>
    /// 戦闘キャラクターの初期能力を定義するマスタレコード
    /// </summary>
    [Serializable]
    public class BattleCombatantDataRecord : IMasterData
    {
        /// <summary>
        /// マスタレコードの識別子。
        /// </summary>
        [field: SerializeField]
        public ulong Id { get; private set; }
        
        /// <summary>
        /// 最大体力
        /// 戦闘開始時の体力としても使用
        /// </summary>
        [field: SerializeField]
        public int MaxHp { get; private set; }
        
        /// <summary>
        /// 最大エネルギー
        /// </summary>
        [field: SerializeField]
        public int MaxEnergy { get; private set; }
        
        /// <summary>
        /// 戦闘開始時のエネルギー
        /// </summary>
        [field: SerializeField]
        public int InitialEnergy { get; private set; }
    }

    /// <summary>
    /// 戦闘キャラクターのマスタレコード一覧を保持
    /// </summary>
    [CreateAssetMenu(fileName = "BattleCombatantData", menuName = "Scriptable Objects/BattleCombatantData")]
    public class BattleCombatantData : ScriptableObject, IMasterDataContainer<BattleCombatantDataRecord>
    {
        [field: SerializeField]
        public List<BattleCombatantDataRecord> Records { get; private set; }
    }
}