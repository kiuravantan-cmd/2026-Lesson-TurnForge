namespace TF.Battle.Models
{
    /// <summary>
    /// 戦闘の入力受付と行動処理の進行段階
    ///
    /// 【状態の流れ】　
    /// Inactive
    ///     ↓ 戦闘開始
    /// WaitingForInput
    ///     ↓ 行動の指示を受信
    /// Resolving
    ///     ├─ 実行できない指示 → WaitingForInput
    ///     └─ 有効な指示 → PlayingEffects
    ///                         ├─ 戦闘継続 → WaitingForInput
    ///                         └─ 戦闘終了 → Finished 
    /// </summary>
    public enum BattleActionState
    {
        /// <summary>
        /// 初期化前など、行動を受け付けない状態
        /// </summary>
        Inactive,
        
        /// <summary>
        /// 今の番のキャラクターから行動の指示を受け付ける状態
        /// </summary>
        WaitingForInput,
        
        /// <summary>
        /// 行動を確認し、戦闘結果を計算している状態
        /// </summary>
        Resolving,
        
        /// <summary>
        /// 確定した結果の演出完了を待っている状態
        /// </summary>
        PlayingEffects,
        
        /// <summary>
        /// 最後の演出まで完了し、戦闘を終了した状態
        /// </summary>
        Finished,
    }
}