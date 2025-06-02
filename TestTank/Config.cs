

namespace TestTank;

public static class Config
{
    public const string DbName = "TestTank";
    public const string ConnectionString = "mongodb://localhost:27017";
    public const string RsaKey =
        "<RSAKeyValue><Modulus>zRSdzFcnZjOCxDMkWUbuRgiOZIQlk7frZMhElQ0a7VqZI9VgU3+lwo0ghZLU3Gg63kOY2UyJ5vFpQdwJUQydsF337ZAUJz4rwGRt/MNL70wm71nGfmdPv4ING+DyJ3ZxFawwE1zSMjMOqQtY4IV8his/HlgXuUfIHVDK87nMNLc=</Modulus><Exponent>AQAB</Exponent><P>7lzjJCmL0/unituEcjoJsZhUDYajgiiIwWwuh0NlCZElmfa5M6l8H+Ahd9yo7ruT6Hrwr4DAdrIKP6LDmFhBdw==</P><Q>3EFKHt4FcDiZXRBLqYZaNSmM1KDrrU97N3CtEDCYS4GimmFOGJhmuK3yGfp/nYLcL2BTKyOZLSQO+/nAjRp2wQ==</Q><DP>SFdkkGsThuiPdrMcxVYb7wxeJiTApxYKOznL/T1VAsxMbyfUGXvMshfh0HDlzF6diycUuQ8IWn26YonRdwECDQ==</DP><DQ>xR9x1NpkB6HAMHhLHzftODMtpYc4Jm5CGsYvPZQgWUN2YbDAkmajWJnlWbbFzBS4N3aAONWtW6cv+ff2itKqgQ==</DQ><InverseQ>oyJzP0Sn+NgdNRRc7/cUKkbbbYaNxkDLDvKLDYMKV6+gcDce85t/FGfaTwkuYQNFqkrRBtDYjtfGsPRTGS6Mow==</InverseQ><D>wM33JNtzUSRwdmDWdZC4BuOYa2vJoD0zc0bNI4x0ml2oyAWdUCMcBfKEds/6i1T6s2e91d2dcJ/aI27o22gO/sfNg3tsr7uYMiUuhSjniqBDB/zyUVig29E4qdfuY1GHxTE8zurroY8mgGEB0aLj+gE0yX9T7sDFkY0QYRqJnwE=</D></RSAKeyValue>";
    
    public const int MaxClient = 1000;
    public const int GamePort = 9200;

    public const int IpLimit = 4;
    
    public static readonly byte[] DefaultKey = [174, 191, 86, 120, 171, 205, 239, 241];
    
    // client Login
    public const int ExpireTime = 5; // 单位分钟
    public const int ExpireTcpTime = 1000; // 单位ms
    
    // client request
    public const int ClientTaskProducerTokens = 10; // 单位 pkg
    public const int ClientRefillTokensTime = 1000; // 单位 ms
    
    // client save
    public const int ClientSave = 5; // 单位分钟
    
}