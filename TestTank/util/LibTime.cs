namespace TestTank.util;

public static class LibTime
{
    //  获取时间戳, 单位毫秒
    public static long UnixTimeNowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}