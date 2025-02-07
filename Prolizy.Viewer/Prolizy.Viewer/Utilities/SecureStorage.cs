using System;
using System.Text;

namespace Prolizy.Viewer.Utilities;

public class SecureStorage
{
    private const int XorKey = 16334;
    
    public static string EncryptPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte) (bytes[i] ^ XorKey);
        return Convert.ToBase64String(bytes);
    }
    
    public static string DecryptPassword(string encryptedPassword)
    {
        var bytes = Convert.FromBase64String(encryptedPassword);
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte) (bytes[i] ^ XorKey);
        return Encoding.UTF8.GetString(bytes);
    }
}