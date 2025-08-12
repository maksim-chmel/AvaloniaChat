using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat;

public class ChatHost
{
    private readonly ChatService _chatService = new();
    private readonly SecureChannel _secureChannel = new();
    private TcpListener? _listener;

    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnMessageReceived;
    public event Action? OnClientConnected;

    private NetworkStream? _stream;
    private AesEncryption? _aes;
    private CancellationTokenSource? _ctsReceiver;

    public NetworkStream? Stream => _stream;
    public AesEncryption? Aes => _aes;

    public async Task StartHostAsync(int port)
    {
        if (_listener != null)
        {
            OnStatusChanged?.Invoke("❌ Хост уже запущен.");
            return;
        }

        _listener = new TcpListener(IPAddress.Any, port);

        
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        _listener.Start();
        OnStatusChanged?.Invoke("Хостинг запущен. Ожидаем подключения...");

        try
        {
            TcpClient? client = await WaitForClientWithTimeoutAsync(_listener, CancellationToken.None);

            if (client != null)
            {
                OnClientConnected?.Invoke();
                OnStatusChanged?.Invoke("✅ Клиент подключен!");

                _stream = client.GetStream();
                _aes = await _secureChannel.InitializeAsHostAsync(_stream);
                _ctsReceiver = new CancellationTokenSource();

                OnStatusChanged?.Invoke("💬 Чат запущен!");

                _chatService.OnMessageReceived += msg => OnMessageReceived?.Invoke(msg);
                _chatService.OnStatusChanged += status => OnStatusChanged?.Invoke(status);
                
                _ = _chatService.StartReceiverLoop(_stream, _aes, _ctsReceiver.Token);
            }
            else
            {
                OnStatusChanged?.Invoke("❌ Таймаут ожидания клиента.");
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Ошибка: {ex.Message}");
        }
        
    }

    private async Task<TcpClient?> WaitForClientWithTimeoutAsync(TcpListener listener, CancellationToken token)
    {
        var acceptTask = listener.AcceptTcpClientAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(Program.TimeoutSeconds), token);

        var completedTask = await Task.WhenAny(acceptTask, timeoutTask);

        if (completedTask == acceptTask)
        {
            token.ThrowIfCancellationRequested();
            return acceptTask.Result;
        }

        return null;
    }

    public async Task SendMessageAsync(string message)
    {
        if (_stream == null || _aes == null)
        {
            OnStatusChanged?.Invoke("❌ Нет подключения для отправки сообщения.");
            return;
        }

        try
        {
            await _chatService.SendMessageAsync(_stream, _aes, message);
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Ошибка отправки: {ex.Message}");
        }
    }

    public Task StopHostAsync()
    {
        try
        {
            _ctsReceiver?.Cancel();
            _stream?.Close();
            _stream = null;
            _aes = null;
            _ctsReceiver = null;

            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }
        }
        catch
        {
            // ignored
        }

        OnStatusChanged?.Invoke("🔌 Хост остановлен.");
        return Task.CompletedTask;
    }
}