using System.Net;
using System.Security.Cryptography;

namespace TestTank.Server.common;

public static class RsaCrypt
{
    private static RSACryptoServiceProvider _key;

    static RsaCrypt()
    {
        _key = new RSACryptoServiceProvider(new CspParameters
        {
            Flags = CspProviderFlags.UseMachineKeyStore
        });
        _key.FromXmlString(Config.RsaKey);
    }
    
    public static void SetKey(string? privateKey)
    {
        if (privateKey == null) return;
        _key = new RSACryptoServiceProvider(new CspParameters
        {
            Flags = CspProviderFlags.UseMachineKeyStore
        });
        _key.FromXmlString(privateKey);
    }
    
    public static byte[] RsaEncrypt1(byte[] bytes)
    {
        //if (_key == null) SetKey(Config.RsaKey);
        return _key.Encrypt(bytes, fOAEP: false);
    }
    
    public static byte[] RsaDecrypt1(byte[] src)
    {
        return _key.Decrypt(src, fOAEP: false);
    }
    
    public static string RsaEncrypt2(byte[] bytes)
    {
        //if (_key == null) SetKey(Config.RsaKey);
        return WebUtility.UrlEncode(Convert.ToBase64String(_key.Encrypt(bytes, fOAEP: false)));
    }
    
    public static byte[] RsaDecrypt2(string src)
    {
        byte[] rgb = Convert.FromBase64String(src);
        return _key.Decrypt(rgb, fOAEP: false);
    }
}