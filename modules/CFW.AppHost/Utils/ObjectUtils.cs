namespace CFW.AppHost.Utils;

public static class ObjectUtils
{
    public static Guid? ToGuid(this string? guidStr)
        => guidStr.IsNullOrWhiteSpace() ? null : new Guid(guidStr!);

    public static bool IsNullOrEmpty(this Guid? guid)
        => guid == null || guid == Guid.Empty;
}
