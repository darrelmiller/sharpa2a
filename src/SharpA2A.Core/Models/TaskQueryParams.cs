using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpA2A.Core;

public class TaskQueryParams : TaskIdParams
{
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }
}


