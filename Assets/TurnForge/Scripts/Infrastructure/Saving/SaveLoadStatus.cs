namespace TF.Infrastructure.Saving
{
    /// <summary>保存データの読み込み結果。</summary>
    public enum SaveLoadStatus
    {
        /// <summary>
        /// 読み込みに失敗
        /// </summary>
        Failed,

        /// <summary>
        /// 保存データが存在しない
        /// </summary>
        NotFound,

        /// <summary>
        /// 読み込みに成功した
        /// </summary>
        Success
    }
}