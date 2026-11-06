using System;
using System.Collections.Generic;
using System.Linq;
using ShareBridge.Core.Contracts;
using ShareBridge.Core.Models;

namespace ShareBridge.Core.Services;

public interface IAdapterRegistry
{
    void RegisterAdapter(ITargetAdapter adapter);
    IReadOnlyList<ITargetAdapter> GetAllAdapters();
    IReadOnlyList<ITargetAdapter> GetCompatibleAdapters(SharePayload payload);
}

public sealed class AdapterRegistry : IAdapterRegistry
{
    private readonly List<ITargetAdapter> _adapters = new();

    public void RegisterAdapter(ITargetAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        if (!_adapters.Any(a => a.Id.Equals(adapter.Id, StringComparison.OrdinalIgnoreCase)))
        {
            _adapters.Add(adapter);
        }
    }

    public IReadOnlyList<ITargetAdapter> GetAllAdapters()
    {
        return _adapters.AsReadOnly();
    }

    public IReadOnlyList<ITargetAdapter> GetCompatibleAdapters(SharePayload payload)
    {
        if (payload == null) return Array.Empty<ITargetAdapter>();

        return _adapters
            .Where(a => a.IsAvailable() && a.CanHandle(payload))
            .ToList()
            .AsReadOnly();
    }
}
