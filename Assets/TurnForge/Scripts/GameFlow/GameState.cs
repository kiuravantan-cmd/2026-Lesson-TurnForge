namespace TF.GameFlow
{
    /// <summary>
    /// ゲーム全体の進行段階
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// 初期化を開始する前の状態
        /// </summary>
        Inactive,
        
        /// <summary>
        /// マスタなど、起動に必要なデータを読み込んでいる状態
        /// </summary>
        Loading,
        
        /// <summary>
        /// タイトルでゲーム開始を待っている状態
        /// </summary>
        Title,
        
        /// <summary>
        /// 戦闘と、その演出を進めている状態
        /// </summary>
        Battle,
        
        /// <summary>
        /// 戦闘結果を表示している状態
        /// </summary>
        Result,
        
        /// <summary>
        /// 戦闘用アセットの読み込みや、通信の開始準備を待っている状態
        /// </summary>
        BattleLoading,
        
        /// <summary>
        /// 初期化などに失敗し、通常の進行を停止している状態
        /// </summary>
        Error = 99,
    }
}