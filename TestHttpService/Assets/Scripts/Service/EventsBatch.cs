using System.Collections.Generic;
using Newtonsoft.Json;

internal class EventsBatch
{
	[JsonProperty("events")]
	public IEnumerable<Event> Events { get; }

	public EventsBatch(IEnumerable<Event> events)
	{
		Events = events;
	}
}
