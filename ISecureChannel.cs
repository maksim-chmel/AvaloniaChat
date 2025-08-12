using System.Net.Sockets;
using System.Threading.Tasks;

namespace AvaloniaChat;

public interface ISecureChannel
{
    Task<AesEncryption> InitializeAsClientAsync(NetworkStream stream);
    Task<AesEncryption> InitializeAsHostAsync(NetworkStream stream);
}