using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AvaloniaChat;

public interface IChatClient
{
    Task<bool> ConnectToHostAsync(string ipString, int port, int timeoutSeconds);
    Task<TcpClient?> ConnectWithRetryAsync(IPAddress serverIp, int port, int timeoutSeconds);
    Task SendMessageAsync(string message);
    void Disconnect();
}