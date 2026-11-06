using System.Threading;
using System.Threading.Tasks;
using ShareBridge.Core.Models;

namespace ShareBridge.Core.Contracts;

public interface ITargetAdapter
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string? IconGlyph { get; }
    string? IconPath { get; }

    bool IsAvailable();
    bool CanHandle(SharePayload payload);
    Task<AdapterResult> SendAsync(SharePayload payload, CancellationToken cancellationToken = default);
}
