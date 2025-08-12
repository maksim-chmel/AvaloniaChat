using System.Threading.Tasks;

namespace AvaloniaChat;

public interface IChatHost
{
    Task StartHostAsync(int port);
    Task SendMessageAsync(string message);
    Task StopHostAsync();
}