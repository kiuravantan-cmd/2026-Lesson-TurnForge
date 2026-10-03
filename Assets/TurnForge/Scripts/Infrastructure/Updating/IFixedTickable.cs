namespace TF.Infrastructure.Updating
{
    /// <summary>
    /// 固定間隔の更新を受け取る対象。
    /// </summary>
    public interface IFixedTickable
    {
        /// <summary>
        /// 固定時間分の処理を行う。
        /// </summary>
        public void FixedTick(UpdateContext context);
    }
}