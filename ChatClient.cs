using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat;

public class ChatClient
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private AesEncryption? _aes;

    private CancellationTokenSource? _ctsReceiver;

    private readonly ChatService chatService = new();
    private readonly SecureChannel secureChannel = new();

    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnMessageReceived;

    public NetworkStream? Stream => _stream;
    public AesEncryption? Aes => _aes;

    public async Task<bool> ConnectToHostAsync(string ipString, int port, int timeoutSeconds)
    {
        if (!IPAddress.TryParse(ipString, out IPAddress? ip))
        {
            OnStatusChanged?.Invoke("❌ Неверный IP адрес");
            return false;
        }

        OnStatusChanged?.Invoke($"🔌 Подключение к {ip}:{port}...");

        var client = await ConnectWithRetryAsync(ip, port, timeoutSeconds);

        if (client == null)
        {
            OnStatusChanged?.Invoke("❌ Таймаут подключения");
            return false;
        }

        _client = client;
        _stream = _client.GetStream();
        _aes = await secureChannel.InitializeAsClientAsync(_stream);

        OnStatusChanged?.Invoke("💬 Подключено! Можно общаться.");

        chatService.OnMessageReceived += msg => OnMessageReceived?.Invoke(msg);
        chatService.OnStatusChanged += status => OnStatusChanged?.Invoke(status);

        _ctsReceiver = new CancellationTokenSource();
        _ = chatService.StartReceiverLoop(_stream, _aes, _ctsReceiver.Token);

        return true;
    }

    private async Task<TcpClient?> ConnectWithRetryAsync(IPAddress serverIp, int port, int timeoutSeconds)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                var client = new TcpClient();
                var connectTask = client.ConnectAsync(serverIp, port);

                var remainingTime = TimeSpan.FromSeconds(timeoutSeconds) - stopwatch.Elapsed;
                if (remainingTime <= TimeSpan.Zero)
                {
                    client.Dispose();
                    break;
                }

                var timeoutTask = Task.Delay(remainingTime);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    if (client.Connected)
                    {
                        return client;
                    }
                    client.Dispose();
                }
                else
                {
                    client.Dispose();
                    break;
                }
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        return null;
    }

    public async Task SendMessageAsync(string message)
    {
        if (_stream == null || _aes == null)
        {
            OnStatusChanged?.Invoke("❌ Нет подключения");
            return;
        }

        try
        {
            await chatService.SendMessageAsync(_stream, _aes, message);
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Ошибка отправки: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            _ctsReceiver?.Cancel();
            _stream?.Close();
            _client?.Close();
        }
        catch { }

        _stream = null;
        _client = null;
        _aes = null;
        _ctsReceiver = null;

        OnStatusChanged?.Invoke("🔌 Отключено от сервера.");
    }
}