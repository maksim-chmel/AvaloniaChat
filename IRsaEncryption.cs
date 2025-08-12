namespace AvaloniaChat;

public interface IRsaEncryption
{
    string GetPublicKey();
    void LoadPublicKey(string base64PublicKey);
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}