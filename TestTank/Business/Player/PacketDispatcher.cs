using log4net;
using Microsoft.Extensions.DependencyInjection;
using TestTank.Server.common;

namespace TestTank.Business.Player;

public interface IAsyncPacketHandler
{
    Task HandleAsync(Player playerContext, PacketIn packet, CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PacketHandlerAttribute(int packetId) : Attribute
{
    public int PacketId { get; } = packetId;
}

public interface IPacketDispatcher
{
    Task DispatchAsync(Player playerContext, PacketIn packet, CancellationToken cancellationToken);
}

public class PacketDispatcher(IServiceProvider serviceProvider, IReadOnlyDictionary<int, Type> handlerMap)
    : IPacketDispatcher
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(PacketDispatcher));

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    // _handlerMap 存储 PacketID 到处理器类型的映射
    private readonly IReadOnlyDictionary<int, Type> _handlerMap = handlerMap ?? throw new ArgumentNullException(nameof(handlerMap));

    public async Task DispatchAsync(Player playerContext, PacketIn packet, CancellationToken cancellationToken)
    {
        if (playerContext == null)
        {
            Log.Error("PlayerContext cannot be null in DispatchAsync.");
            return; // 或者抛出异常
        }

        if (packet == null)
        {
            Log.Error($"Player {playerContext.Data.RoleId}: Received null packet in DispatchAsync.");
            return; // 或者抛出异常
        }

        if (_handlerMap.TryGetValue(packet.Pid, out var handlerType))
        {
            // 为每次包处理创建一个新的作用域 (Scope)。
            // 这对于依赖 Scoped 服务 (如 EF Core DbContext) 的处理器非常重要。
            // 如果处理器及其所有依赖都是 Transient 或 Singleton，则可以省略 Scope，
            // 但使用 Scope 是一个更健壮的模式。
            using (var scope = _serviceProvider.CreateScope())
            {
                IAsyncPacketHandler? handler = null;
                try
                {
                    // 从当前作用域解析处理器实例
                    handler = scope.ServiceProvider.GetRequiredService(handlerType) as IAsyncPacketHandler;
                    if (handler == null) // GetRequiredService 理论上不会返回 null，但以防万一类型转换问题
                    {
                        Log.Error(
                            $"Player {playerContext.Data.RoleId}: Could not resolve or cast handler for PID {packet.Pid} to IAsyncPacketHandler. Registered type: {handlerType.FullName}");
                        return;
                    }

                    Log.Debug(
                        $"Player {playerContext.Data.RoleId}: Dispatching PID {packet.Pid} to handler {handlerType.Name}.");
                    // 调用处理器的 HandleAsync 方法，传入 Player 上下文
                    await handler.HandleAsync(playerContext, packet, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Player {playerContext.Data.RoleId}: Error in handler {handlerType.Name} for PID {packet.Pid}: {ex.Message}\n{ex.StackTrace}",
                        ex);
                    // 根据业务需求，这里可以考虑：
                    // 1. 给玩家发送错误提示
                    // await playerContext.SendSystemMessageAsync("处理您的请求时发生错误。");
                    // 2. 断开玩家连接 (如果错误严重)
                    // await playerContext.DisconnectAsync("Internal server error.");
                }
            }
        }
        else
        {
            Log.Warn($"Player {playerContext.Data.RoleId}: No handler found for Packet ID: {packet.Pid}.");
            // 可以给玩家发送 "未知命令" 的提示
            // await playerContext.SendSystemMessageAsync($"未知命令: {packet.Pid}");
        }
    }
}

}