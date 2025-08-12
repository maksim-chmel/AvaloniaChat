namespace AvaloniaChat;

public interface IAesEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}