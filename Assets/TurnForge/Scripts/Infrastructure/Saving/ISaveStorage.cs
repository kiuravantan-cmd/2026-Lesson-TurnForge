namespace TF.Infrastructure.Saving
{
    /// <summary>
    /// 保存先に依存しない、データの保存と読み込みを定義
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>
        /// 指定したキーでデータを保存する
        /// </summary>
        /// <typeparam name="T">保存形式に対応したデータ型</typeparam>
        /// <param name="key">保存データを識別するキー</param>
        /// <param name="data">保存するデータ</param>
        /// <returns>保存に成功した場合はtrue</returns>
        bool TrySave<T> (string key, T data) where T : class;

        /// <summary>
        /// 指定したキーのデータを読み込む
        /// </summary>
        /// <typeparam name="T">読み込むデータ型</typeparam>
        /// <param name="key">保存データを識別するキー</param>
        /// <param name="data">読み込んだデータ。失敗時はnull</param>
        /// <param name="status">読み込み結果</param>
        /// <returns>読み込みに成功した場合はtrue</returns>
        bool TryLoad<T> (string key, out T data, out SaveLoadStatus status) where T : class;
    }
}
