using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat;

public interface IChatService
{
    Task StartReceiverLoop(NetworkStream stream, AesEncryption aes, CancellationToken token);
    Task SendMessageAsync(NetworkStream stream, AesEncryption aes, string message);
    event Action<string>? OnMessageReceived;
    event Action<string>? OnStatusChanged;
}