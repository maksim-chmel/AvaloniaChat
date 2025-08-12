using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaChat;

public class ChatService : IChatService
{
    public event Action<string>? OnMessageReceived;
    public event Action<string>? OnStatusChanged;

    public async Task StartReceiverLoop(NetworkStream stream, AesEncryption aes, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                byte[] lengthBuffer = new byte[4];
                int read = 0;
                while (read < 4)
                {
                    int bytesRead = await stream.ReadAsync(lengthBuffer, read, 4 - read, token);
                    if (bytesRead == 0)
                    {
                        OnStatusChanged?.Invoke("⚠️ Собеседник отключился");
                        return;
                    }
                    read += bytesRead;
                }

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0)
                {
                    OnStatusChanged?.Invoke("❌ Некорректная длина сообщения");
                    continue;
                }

                byte[] messageBuffer = new byte[messageLength];
                read = 0;
                while (read < messageLength)
                {
                    int bytesRead = await stream.ReadAsync(messageBuffer, read, messageLength - read, token);
                    if (bytesRead == 0)
                    {
                        OnStatusChanged?.Invoke("⚠️ Собеседник отключился");
                        return;
                    }
                    read += bytesRead;
                }

                string encrypted = Encoding.UTF8.GetString(messageBuffer);
                string decrypted;
                try
                {
                    decrypted = aes.Decrypt(encrypted);
                }
                catch
                {
                    OnStatusChanged?.Invoke("❌ Ошибка расшифровки сообщения");
                    continue;
                }

                OnStatusChanged?.Invoke($"Получено сообщение: {decrypted}");

                if (decrypted == "__exit__")
                {
                    OnStatusChanged?.Invoke("👋 Собеседник вышел из чата");
                    break;
                }

                OnMessageReceived?.Invoke(decrypted);
            }
        }
        catch (OperationCanceledException)
        {
            OnStatusChanged?.Invoke("⏹ Приём сообщений остановлен.");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"❌ Ошибка приёма сообщений: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(NetworkStream stream, AesEncryption aes, string message)
    {
        string encrypted = aes.Encrypt(message);
        OnStatusChanged?.Invoke($"Отправлено сообщение: {message}");

        byte[] encryptedData = Encoding.UTF8.GetBytes(encrypted);

        byte[] lengthPrefix = BitConverter.GetBytes(encryptedData.Length);
        await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);

        await stream.WriteAsync(encryptedData, 0, encryptedData.Length);
    }
}