using UnityEngine;

public class EventInputHandler : MonoBehaviour
{
	[SerializeField]
	private EventService _eventService;

	[SerializeField]
	private string _type;

	[SerializeField]
	private string _data;

	public void TrackEvent()
	{
		_eventService.TrackEvent(_type, _data);
	}
}
