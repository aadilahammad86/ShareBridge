namespace ShareBridge.Core.Models;

public enum AdapterErrorKind
{
    None = 0,
    AppNotFound,
    LaunchFailed,
    Cancelled,
    UnsupportedContent,
    InvalidArguments,
    UnexpectedError
}

public sealed record AdapterResult(
    bool Success,
    AdapterErrorKind ErrorKind = AdapterErrorKind.None,
    string? ErrorMessage = null)
{
    public static AdapterResult Ok() => new(true);
    public static AdapterResult Fail(AdapterErrorKind kind, string message) => new(false, kind, message);
}
