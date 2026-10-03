namespace TF.Infrastructure.Updating
{
    /// <summary>
    /// 通常更新を受け取るインターフェース
    /// </summary>
    public interface IUpdateTickable
    {
        /// <summary>
        /// 1フレーム分の処理を行う。
        /// </summary>
        public void Tick(UpdateContext context);
    }
}

