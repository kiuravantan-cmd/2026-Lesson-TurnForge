namespace TF.Battle.Commands
{
    /// <summary>
    /// 戦闘で選択できる行動
    /// </summary>
    public enum BattleCommand
    {
        /// <summary>
        /// 通常攻撃を行う
        /// </summary>
        Attack,
        
        /// <summary>
        /// 次の自分のターンまで被ダメージを軽減する
        /// </summary>
        Guard,
        
        /// <summary>
        /// エネルギーを増やす
        /// </summary>
        Charge,
        
        /// <summary>
        /// エネルギーを消費して必殺技を使う
        /// </summary>
        Special,
    }
}