using MessagePack;

namespace SpeechWebGame
{
    [MessagePackObject]
    public class GameInfo
    {
        [Key("id")]
        public string? Id { get; init; }
        [Key("title")]
        public string? Title { get; init; }
        [Key("minPlayers")]
        public int? MinPlayers { get; init; }
        [Key("maxPlayers")]
        public int? MaxPlayers { get; init; }
    }
}
