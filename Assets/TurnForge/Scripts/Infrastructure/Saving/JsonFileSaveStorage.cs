namespace TF.Infrastructure.Saving
{
    /// <summary>
    /// 第3回で実装する、ゲーム内容に依存しないJSON保存処理
    /// </summary>
    public sealed class JsonFileSaveStorage : ISaveStorage
    {
        /// <summary>
        /// 保存ファイルを配置するフォルダー
        /// </summary>
        private readonly string _directoryPath;

        /// <summary>
        /// 保存先フォルダーを受け取る
        /// </summary>
        public JsonFileSaveStorage(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        /// <summary>
        /// 指定したキーでデータを保存する。配布時は書き込まない。
        /// </summary>
        public bool TrySave<T>(string key, T data) where T : class
        {
            // TODO LESSON03-01: キーと入力を確認し、JSONに変換して保存する。
            // 第3回・1〜2コマ目: キーを英数字・ハイフン・アンダースコアに限定して保存先を作る。
            // JSONを一時ファイルへ書き、書き込み成功後に保存先へ反映する。
            // ファイルI/Oの境界でtry/catchし、失敗はfalseで返して既存データを保護する。
            // 一時ファイルからの反映、I/O失敗時の扱いと後始末を実装する。
            return false;
        }

        /// <summary>
        /// 保存済みデータを読み込む。配布時は読み込まない。
        /// </summary>
        public bool TryLoad<T>(string key, out T data, out SaveLoadStatus status) where T : class
        {
            data = null;
            status = SaveLoadStatus.Failed;
            // TODO LESSON03-02: JSONを読み込み、成功・未保存・失敗を区別する。
            // 第3回・2コマ目: ファイルなしはNotFound、読み込み・変換の失敗はFailedに分ける。
            // 読み込み成功時だけdataを返す。破損を初回扱いにして上書きしない。
            // 未実装をNotFoundとして返さない。既存データの上書きを防ぐ。
            return false;
        }
    }
}
