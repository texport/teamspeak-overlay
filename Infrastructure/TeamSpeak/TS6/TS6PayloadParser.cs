using System.Text.Json;

namespace TeamSpeakOverlay.Infrastructure.TeamSpeak.TS6
{
    /// <summary>
    /// Полный и надежный парсер входящих JSON элементов от TS6 API.
    /// Безопасно проверяет все возможные вариации расположения полей (на верхнем уровне и внутри properties).
    /// </summary>
    public static class TS6PayloadParser
    {
        public static int ExtractClientId(JsonElement item)
        {
            // 1. Проверка корневого уровня элемента
            if (item.TryGetProperty("clientId", out var idProp) && TryGetInt(idProp, out int id)) return id;
            if (item.TryGetProperty("clientSelfId", out idProp) && TryGetInt(idProp, out id)) return id;
            if (item.TryGetProperty("clid", out idProp) && TryGetInt(idProp, out id)) return id;
            if (item.TryGetProperty("id", out idProp) && TryGetInt(idProp, out id)) return id;

            // 2. Проверка объекта properties
            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("clientId", out idProp) && TryGetInt(idProp, out id)) return id;
                if (props.TryGetProperty("clid", out idProp) && TryGetInt(idProp, out id)) return id;
                if (props.TryGetProperty("id", out idProp) && TryGetInt(idProp, out id)) return id;
            }

            return 0;
        }

        private static bool TryGetInt(JsonElement element, out int result)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                result = element.GetInt32();
                return true;
            }
            if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out result))
            {
                return true;
            }
            result = 0;
            return false;
        }

        public static string ExtractChannelId(JsonElement item)
        {
            // 1. Явные свойства ID канала на корневом уровне
            if (item.TryGetProperty("channelId", out var chProp)) return chProp.ToString();
            if (item.TryGetProperty("newChannelId", out chProp)) return chProp.ToString();
            if (item.TryGetProperty("cid", out chProp)) return chProp.ToString();

            // 2. Проверка объекта properties (включая унаследованный ID канала от TS6)
            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("channelGroupInheritedChannelId", out chProp)) return chProp.ToString();
                if (props.TryGetProperty("channelId", out chProp)) return chProp.ToString();
                if (props.TryGetProperty("newChannelId", out chProp)) return chProp.ToString();
                if (props.TryGetProperty("cid", out chProp)) return chProp.ToString();
            }

            // 3. Если это объект канала (а не клиента, у которого id равен clientId), проверяем "id"
            if (!item.TryGetProperty("clientId", out _) && !item.TryGetProperty("clid", out _))
            {
                if (item.TryGetProperty("id", out var idProp)) return idProp.ToString();
                if (item.TryGetProperty("properties", out props) && props.ValueKind == JsonValueKind.Object && props.TryGetProperty("id", out idProp)) return idProp.ToString();
            }

            return string.Empty;
        }

        public static string ExtractNicknameFromElement(JsonElement item)
        {
            string nick = string.Empty;

            // 1. Проверка объекта properties
            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("client_nickname", out var nProp)) nick = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("nickname", out nProp)) nick = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("displayName", out nProp)) nick = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("clientDisplayName", out nProp)) nick = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("clientNickname", out nProp)) nick = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("name", out nProp)) nick = nProp.GetString() ?? string.Empty;
            }

            // 2. Проверка корневого уровня
            if (string.IsNullOrEmpty(nick))
            {
                if (item.TryGetProperty("client_nickname", out var nProp2)) nick = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("nickname", out nProp2)) nick = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("displayName", out nProp2)) nick = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("clientDisplayName", out nProp2)) nick = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("clientNickname", out nProp2)) nick = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("name", out nProp2)) nick = nProp2.GetString() ?? string.Empty;
            }

            return nick;
        }

        public static string ExtractChannelNameFromElement(JsonElement item)
        {
            string chName = string.Empty;

            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                if (props.TryGetProperty("channel_name", out var nProp)) chName = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("name", out nProp)) chName = nProp.GetString() ?? string.Empty;
                else if (props.TryGetProperty("channelName", out nProp)) chName = nProp.GetString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(chName))
            {
                if (item.TryGetProperty("channel_name", out var nProp2)) chName = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("name", out nProp2)) chName = nProp2.GetString() ?? string.Empty;
                else if (item.TryGetProperty("channelName", out nProp2)) chName = nProp2.GetString() ?? string.Empty;
            }

            return chName;
        }
    }
}
