using System.Text.Json.Serialization;

namespace Squire.NumTic.AI;

/// <summary>
///   A source generation context for JSON serialization and deserialization
///   in an AOT-compatible manner.
/// </summary>
///
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OpenAIPlayer.MoveResponse))]
internal partial class AISerializerContext : JsonSerializerContext
{
}
