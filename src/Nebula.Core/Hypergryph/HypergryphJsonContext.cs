using System.Text.Json.Serialization;
using Nebula.Core.HoYoPlay;

namespace Nebula.Core.Hypergryph;

[JsonSerializable(typeof(List<GameInfo>))]
public partial class HypergryphJsonContext : JsonSerializerContext;
