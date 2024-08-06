using Newtonsoft.Json;

internal class Event
{
	[JsonProperty("type")]
	public string Type { get; }

	[JsonProperty("content")]
	public string Content { get; }

	public Event(string type, string content)
	{
		Type = type;
		Content = content;
	}
}