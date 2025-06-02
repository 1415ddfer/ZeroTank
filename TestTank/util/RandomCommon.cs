using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TestTank.util;


public static class RandomCommon
{
    
    static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
    
    /// <summary>
    /// 随机字符串
    /// </summary>
    /// <param name="chars"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string GetRandomStr(int length, string? chars = null)
    {
        chars ??= "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghizklmnopqrstuvwxyz0123456789";
        var stringBuilder = new StringBuilder(length);

        
        var byteBuffer = new byte[sizeof(uint)];

        for (int i = 0; i < length; i++)
        {
            Rng.GetBytes(byteBuffer);
            uint num = BitConverter.ToUInt32(byteBuffer, 0);
            stringBuilder.Append(chars[(int)(num % (uint)chars.Length)]);
        }
    
        return stringBuilder.ToString();
    }
    
    public static T Draw<T>(Dictionary<T, double> itemsWithProbabilities) where T : notnull
    {
        // 计算总权重
        double totalWeight = 0;
        foreach (var item in itemsWithProbabilities)
            totalWeight += item.Value;
        

        // 生成一个0到总权重之间的随机数
        var randomValue = GetRandomDouble() * totalWeight;

        // 根据随机数选择物品
        foreach (var item in itemsWithProbabilities)
        {
            if (randomValue < item.Value)
                return item.Key;
            randomValue -= item.Value;
        }
        throw new InvalidOperationException("The probabilities are not set up correctly.");
    }

    private static double GetRandomDouble()
    {
        byte[] buffer = new byte[8];
        Rng.GetBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0) / (1.0 + UInt64.MaxValue);
    }
}

public static class UtfCommon
{
    public static bool IsNumeric(string input)
    {
        return Regex.IsMatch(input, @"^\d+$");
    }

    public static bool IsAlphanumeric(string input)
    {
        return Regex.IsMatch(input, @"^[a-zA-Z0-9]+$");
    }

    public static bool IsMD5(string input)
    {
        return Regex.IsMatch(input, @"^[a-fA-F0-9]{32}$");
    }
}