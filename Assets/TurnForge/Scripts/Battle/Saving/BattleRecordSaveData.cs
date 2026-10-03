using System;
using UnityEngine;

namespace TF.Battle.Saving
{
    /// <summary>
    /// JSONへ保存する戦績データ
    /// </summary>
    [Serializable]
    // TODO LESSON03-12: 第3回・3コマ目で別の戦闘再開用DTOをこのフォルダーへ追加する。
    // 現在状態・キャラクターID・操作方式・保存形式の版番号を保存し、戦績とは区別する。
    public sealed class BattleRecordSaveData
    {
        /// <summary>
        /// 現在対応している保存形式のバージョン
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// 保存時のデータ形式のバージョン
        /// </summary>
        [SerializeField] private int _version;

        /// <summary>
        /// 勝利した回数
        /// </summary>
        [SerializeField] private int _winCount;

        /// <summary>
        /// 敗北した回数
        /// </summary>
        [SerializeField] private int _lossCount;

        /// <summary>
        /// 引き分けになった回数
        /// </summary>
        [SerializeField] private int _drawCount;

        /// <summary>
        /// 保存形式のバージョン
        /// </summary>
        public int Version => _version;

        /// <summary>
        /// 勝利した回数
        /// </summary>
        public int WinCount => _winCount;

        /// <summary>
        /// 敗北した回数
        /// </summary>
        public int LossCount => _lossCount;

        /// <summary>
        /// 引き分けになった回数
        /// </summary>
        public int DrawCount => _drawCount;

        /// <summary>
        /// 現在の保存形式で戦績データを作成する
        /// </summary>
        /// <param name="winCount">勝利数</param>
        /// <param name="lossCount">敗北数</param>
        /// <param name="drawCount">引き分け数</param>
        public BattleRecordSaveData(int winCount, int lossCount, int drawCount)
        {
            _version = CurrentVersion;
            _winCount = winCount;
            _lossCount = lossCount;
            _drawCount = drawCount;
        }
    }
}
