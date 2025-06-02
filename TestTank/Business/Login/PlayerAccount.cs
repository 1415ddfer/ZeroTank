using System.Text;
using log4net;
using TestTank.Server.common;
using TestTank.Server.proto;
using ILog = log4net.ILog;

namespace TestTank.Business.account;

public static class PlayerAccount
{
    static readonly ILog Log = LogManager.GetLogger(typeof(PlayerAccount));
    
    public static int TryLogin(PacketIn packet, out byte[] clientKey)
    {
        try
        {
            var data = packet.Deserialize<ProtoC0>();
            byte[] bytes;
            {
                bytes = RsaCrypt.RsaDecrypt1(data.LoginData);
            }
            clientKey = bytes[7..15];
            var utf8 = new UTF8Encoding();
            var loginSrc = utf8.GetString(bytes[15..]);
            var arr = loginSrc.Split(',', 2);
            var roleId = Account.TcpLogin(arr[0], arr[1]);
            if (roleId != 0) return roleId;
            Log.Info($"账号{arr[0]}登录失败!");
            return -1;
        }
        catch (Exception e)
        {
            Log.Error($"登录时出现异常{e.Message}--{e.StackTrace}");
            clientKey = [];
            return -1;
        }
    }

    public static bool TryLoginHttp(int roleId, string src)
    {
        byte[] ba;
        try
        {
            ba = RsaCrypt.RsaDecrypt2(src);
        }
        catch (Exception _)
        {
            Log.Error("密文解密失败!");
            return false;
        }
        try
        {
            var utf8 = new UTF8Encoding();
            var arr = utf8.GetString(ba[7..]).Split(",");
            if (arr.Length != 4) return false;
            var acc = arr[0];
            var webKey = arr[1];
            var clientKey = arr[2];
            // var nick = arr[3];
            return Account.ClientLogin(acc, webKey, clientKey, roleId);
        }
        catch (Exception e)
        {
            Log.Error($"密文序列化失败!{e.Message}--{e.StackTrace}");
            return false;
        }

    }
    

}