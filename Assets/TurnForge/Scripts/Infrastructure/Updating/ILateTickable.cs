namespace TF.Infrastructure.Updating
{
    public interface ILateTickable
    {
        /// <summary>
        /// カメラ追従などの後処理を行う
        /// </summary>
        public void LateTick(UpdateContext context);
    }
}