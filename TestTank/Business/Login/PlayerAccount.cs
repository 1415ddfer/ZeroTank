using System.Text;
using Microsoft.Extensions.Logging;
using TestTank.Server.common;
using TestTank.Server.proto;

namespace TestTank.Business.Login;

public class PlayerAccount(IAccountService accountService, ILogger<PlayerAccount> logger)
{
    public int TryLogin(PacketIn packet, out byte[] clientKey)
    {
        clientKey = [];

        try
        {
            var data = packet.Deserialize<ProtoC0>();
            var bytes = RsaCrypt.RsaDecrypt1(data.LoginData);
            clientKey = bytes[7..15];

            var utf8 = new UTF8Encoding();
            var loginSrc = utf8.GetString(bytes[15..]);
            var arr = loginSrc.Split(',', 2);

            if (arr.Length != 2)
            {
                logger.LogWarning("登录数据格式错误");
                return -1;
            }

            var roleId = accountService.TcpLogin(arr[0], arr[1]);
            if (roleId != 0)
            {
                return roleId;
            }

            logger.LogWarning("账号 {Username} 登录失败!", arr[0]);
            return -1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "登录时出现异常");
            return -1;
        }
    }

    public bool TryLoginHttp(int roleId, string src)
    {
        try
        {
            byte[] ba = RsaCrypt.RsaDecrypt2(src);
            var utf8 = new UTF8Encoding();
            var arr = utf8.GetString(ba[7..]).Split(",");

            if (arr.Length != 4)
            {
                logger.LogWarning("HTTP登录数据格式错误");
                return false;
            }

            var acc = arr[0];
            var webKey = arr[1];
            var clientKey = arr[2];

            return accountService.ClientLogin(acc, webKey, clientKey, roleId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP登录时发生错误");
            return false;
        }
    }
}