namespace TF.Infrastructure.Updating
{
    public static class UpdateOrder
    {
        /// <summary>
        /// 入力指示の処理
        /// </summary>
        public const int Input = 100;

        /// <summary>
        /// ゲーム状態の更新
        /// </summary>
        public const int Simulation = 200;
        
        /// <summary>
        /// 演出の更新
        /// </summary>
        public const int Effects = 300;

        /// <summary>
        /// 表示の更新
        /// </summary>
        public const int Presentation = 400;

        /// <summary>
        /// カメラの更新
        /// </summary>
        public const int Camera = 500;
    }
}