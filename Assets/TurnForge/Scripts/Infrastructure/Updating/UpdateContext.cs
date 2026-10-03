namespace TF.Infrastructure.Updating
{
    /// <summary>
    /// 更新処理に渡す時間情報
    /// </summary>
    public sealed class UpdateContext
    {
        /// <summary>
        /// timeScaleの影響を受ける経過時間
        /// </summary>
        public float DeltaTime { get; init; }
        
        /// <summary>
        /// timeScaleの影響を受けない経過時間
        /// </summary>
        public float UnscaledDeltaTime { get; init; }

        public UpdateContext(float deltaTime, float unscaledDeltaTime)
        {
            DeltaTime = deltaTime;
            UnscaledDeltaTime = unscaledDeltaTime;
        }
    }
}