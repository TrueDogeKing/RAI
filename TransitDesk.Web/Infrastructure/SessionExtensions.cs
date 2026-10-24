using System.Text.Json;

namespace TransitDesk.Web.Infrastructure;

/// <summary>
/// ISession przechowuje wyłącznie bajty/łańcuchy – obiekty złożone serializujemy do JSON.
/// </summary>
public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var rawJson = session.GetString(key);
        return rawJson is null ? default : JsonSerializer.Deserialize<T>(rawJson);
    }
}
