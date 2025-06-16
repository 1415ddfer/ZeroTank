using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace TestTank.Server;


public class VisitorClientPoolPolicy(IServiceProvider serviceProvider) : IPooledObjectPolicy<VisitorClient>
{
    public VisitorClient Create()
    {
        return ActivatorUtilities.CreateInstance<VisitorClient>(serviceProvider);
    }

    public bool Return(VisitorClient client)
    {
        try
        {
            client.Reset();
            return true;
        }
        catch (Exception)
        {
            // 如果重置失败，不要将对象返回到池中
            return false;
        }
    }
}