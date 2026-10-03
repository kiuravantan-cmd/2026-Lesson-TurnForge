using System.Threading;
using Cysharp.Threading.Tasks;

namespace TF.Battle.Preparation
{
    /// <summary>
    /// 戦闘開始に必要な準備と、確保したリソースの解放を定義
    /// </summary>
    public interface IBattlePreparation
    {
        /// <summary>
        /// 戦闘の準備
        /// </summary>
        /// <returns>成功時はtrue、失敗・中断時はfalse</returns>
        UniTask<bool> PrepareAsync(CancellationToken cts);
        
        /// <summary>
        /// この準備処理が所有するリソースを解放
        /// </summary>
        void Release();
    }
}