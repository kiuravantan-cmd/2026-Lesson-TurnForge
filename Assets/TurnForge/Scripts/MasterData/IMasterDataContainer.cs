using System.Collections.Generic;
namespace TF.MasterData
{
    public interface IMasterDataContainer<T> where T : IMasterData
    {
        List<T> Records { get; }
    }
}
