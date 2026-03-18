using System.Collections.Concurrent;
using ChatBot.Api.Data;
using ChatBot.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Services;

public class TenantResolver(IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<string, TenantConfig?> _cache = new();

    public async Task<TenantConfig?> ResolveAsync(string tenantId)
    {
        if (_cache.TryGetValue(tenantId, out var cached))
            return cached;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var config = await db.TenantConfigs.FirstOrDefaultAsync(t => t.TenantId == tenantId);

        _cache.TryAdd(tenantId, config);
        return config;
    }

    public void InvalidateCache(string tenantId) => _cache.TryRemove(tenantId, out _);

    public void InvalidateAll() => _cache.Clear();
}
