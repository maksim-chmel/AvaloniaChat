using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat;

public class ChatHost(IChatService chatService, ISecureChannel secureChannel) : IChatHost
{
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
            OnStatusChanged?.Invoke("❌ Host is already running.");
            return;
        }

        try
        {
            _listener = new TcpListener(IPAddress.Any, port);

            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _listener.Start();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            OnStatusChanged?.Invoke($"❌ Port {port} is already in use.");
            _listener = null;
            return;
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Failed to start host: {ex.Message}");
            _listener = null;
            return;
        }
       
        OnStatusChanged?.Invoke("Hosting started. Waiting for a client...");
        try
        {
            TcpClient? client = await WaitForClientWithTimeoutAsync(_listener, CancellationToken.None);

            if (client != null)
            {
                OnClientConnected?.Invoke();
                OnStatusChanged?.Invoke("✅ Client connected!");

                _stream = client.GetStream();
                _aes = await secureChannel.InitializeAsHostAsync(_stream);
                _ctsReceiver = new CancellationTokenSource();

                OnStatusChanged?.Invoke("💬 Chat started!");

                chatService.OnMessageReceived += msg => OnMessageReceived?.Invoke(msg);
                chatService.OnStatusChanged += status => OnStatusChanged?.Invoke(status);
                
                _ = chatService.StartReceiverLoop(_stream, _aes, _ctsReceiver.Token);
            }
            else
            {
                OnStatusChanged?.Invoke("❌ Client connection timed out.");
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Error: {ex.Message}");
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
            OnStatusChanged?.Invoke("❌ No connection to send the message.");
            return;
        }

        try
        {
            await chatService.SendMessageAsync(_stream, _aes, message);
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Send error: {ex.Message}");
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

        OnStatusChanged?.Invoke("🔌 Host stopped.");
        return Task.CompletedTask;
    }
}