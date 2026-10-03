using System;
using System.Collections.Generic;
using UnityEngine;

namespace TF.MasterData
{
    [Serializable]
    public class BattleCommandDataRecord : IMasterData
    {
        /// <summary>
        /// マスタレコードの識別子
        /// </summary>
        [field: SerializeField]
        public ulong Id { get; private set; }

        /// <summary>
        /// 対応する戦闘コマンド
        /// </summary>
        [field: SerializeField]
        public int Command { get; private set; }

        /// <summary>
        /// 防御による軽減前のダメージ
        /// </summary>
        [field: SerializeField]
        public int Damage { get; private set; }

        /// <summary>
        /// 行動時に消費するエネルギー
        /// </summary>
        [field: SerializeField]
        public int EnergyCost { get; private set; }

        /// <summary>
        /// 行動時に回復するエネルギー
        /// </summary>
        [field: SerializeField]
        public int EnergyGain { get; private set; }

        /// <summary>
        /// 防御中の被ダメージを割る値。1で軽減なし、2で半減
        /// </summary>
        [field: SerializeField]
        public int GuardDamageDivisor { get; private set; } = 1;
    }
    
    /// <summary>
    /// 戦闘コマンドのマスタレコード一覧を保持
    /// </summary>
    [CreateAssetMenu(fileName = "BattleCommandData", menuName = "Scriptable Objects/BattleCommandData")]
    public class BattleCommandData : ScriptableObject, IMasterDataContainer<BattleCommandDataRecord>
    {
        /// <summary>
        /// CSVから取り込んだマスタレコード一覧
        /// </summary>
        [field: SerializeField]
        public List<BattleCommandDataRecord> Records { get; private set; }
    }
}