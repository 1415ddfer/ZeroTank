using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace TestTank.Server;


public class VisitorClientPoolPolicy : IPooledObjectPolicy<VisitorClient>
{
    private readonly IServiceProvider _serviceProvider;
    public VisitorClientPoolPolicy(IServiceProvider serviceProvider) 
        => _serviceProvider = serviceProvider;

    public VisitorClient Create() 
        => ActivatorUtilities.CreateInstance<VisitorClient>(_serviceProvider);

    public bool Return(VisitorClient client)
    {
        client.Reset(); // 重置状态
        return true;
    }
}