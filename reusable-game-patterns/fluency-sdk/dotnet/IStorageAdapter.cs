using System.Threading.Tasks;

namespace FluencySDK
{
    public interface IStorageAdapter
    {
        Task<T> GetItemAsync<T>(string key);
        Task SetItemAsync<T>(string key, T value);
        Task RemoveItemAsync(string key);
    }
} 