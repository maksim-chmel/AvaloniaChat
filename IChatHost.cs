using System;
using System.Threading.Tasks;

namespace AvaloniaChat;

public interface IChatHost
{
    Task StartHostAsync(int port);
    Task SendMessageAsync(string message);
    Task StopHostAsync();
    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnMessageReceived;
    public event Action? OnClientConnected;
}