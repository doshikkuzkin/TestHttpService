using Newtonsoft.Json;

internal class Event
{
	[JsonProperty("type")]
	public string Type { get; }

	[JsonProperty("data")]
	public string Data { get; }

	public Event(string type, string data)
	{
		Type = type;
		Data = data;
	}
}