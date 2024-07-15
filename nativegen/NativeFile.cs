using Newtonsoft.Json;

namespace NativeGen;

public class NativeFunction
{
    [JsonProperty("name")]
    public string Name;
    [JsonProperty("jhash")]
    public string JHash;
    [JsonProperty("comment")]
    public string Comment;
    [JsonProperty("params")]
    public List<NativeParams> Params;
    [JsonProperty("return_type")]
    public string ReturnType;
}

public class NativeParams
{
    [JsonProperty("type")]
    public string Type;
    [JsonProperty("name")]
    public string Name;
}