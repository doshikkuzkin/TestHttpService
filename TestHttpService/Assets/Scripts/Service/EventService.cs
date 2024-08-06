using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Threading;

public class EventService : MonoBehaviour
{
	private const int MillisecondsInSecond = 1000;

	[SerializeField]
	private string _serviceUrl;

	[SerializeField]
	private long _cooldownMs;

	[SerializeField]
	private long _timeoutMs;

	[SerializeField]
	private string _saveFileName;

	private HttpClient _httpClient = new HttpClient();

	private List<Event> _cachedEvents = new List<Event>();
	private List<Event> _eventsToSend = new List<Event>();

	private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private string _saveFilePath;
	private float _nextSendTime;

	private bool _cooldownStarted = false;

	public void TrackEvent(string type, string data)
	{
		var httpEvent = new Event(type, data);

		_cachedEvents.Add(httpEvent);
		_eventsToSend.Add(httpEvent);

		SaveCachedEvents();

		UpdateCooldown();
	}

	private void Awake()
	{
		_saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);

		Initialize(TimeSpan.FromMilliseconds(_timeoutMs));
		SendSavedEvents();
	}

	private void Update()
	{
		if (!_cooldownStarted)
		{
			return;
		}

		var currentTime = Time.unscaledTime;

		if (_nextSendTime < currentTime)
		{
			if (_eventsToSend.Count > 0)
			{
				var events = _eventsToSend.ToArray();
				_eventsToSend.Clear();
				_cooldownStarted = false;

				SendEvents(events).Forget();
			}
		}
	}

	private void OnDestroy()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
	}

	private void UpdateCooldown()
	{
		if (!_cooldownStarted)
		{
			UpdateNextSendTime(Time.unscaledTime);
			_cooldownStarted = true;
		}
	}

	private void UpdateNextSendTime(float currentTime)
	{
		_nextSendTime = currentTime + (float)_cooldownMs / MillisecondsInSecond;
	}

	private void Initialize(TimeSpan timeout)
	{
		_httpClient.Timeout = timeout;
	}

	private void SendSavedEvents()
	{
		var savedEvents = DeserializeEvents();

		if (savedEvents != null && savedEvents.Any())
		{
			_cachedEvents.AddRange(savedEvents);
			SendEvents(savedEvents).Forget();
		}
	}

	private async UniTaskVoid SendEvents(IEnumerable<Event> events)
	{
		var requestString = SerializeEvents(events);
		var content = new StringContent(requestString);

		var request = new HttpRequestMessage(HttpMethod.Post, _serviceUrl);
		request.Content = content;

		var isSuccessResponse = false;

		try
		{
			Debug.Log($"Try send events: {requestString}");

			var response = await _httpClient.SendAsync(request, _cancellationTokenSource.Token);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				isSuccessResponse = true;

				var responseText = await response.Content.ReadAsStringAsync();

				Debug.Log($"Success response received: {responseText}");

				foreach (var e in events)
				{
					_cachedEvents.Remove(e);
				}

				SaveCachedEvents();
			}
		}
		catch (Exception ex)
		{
			Debug.Log($"Request exception occured: {ex.Message}");
		}
		finally
		{
			if (!isSuccessResponse)
			{
				UpdateCooldown();

				_eventsToSend.AddRange(events);
			}
		}
	}

	private void SaveCachedEvents()
	{
		var fileContent = _cachedEvents.Count > 0 ? SerializeEvents(_cachedEvents) : string.Empty;

		File.WriteAllText(_saveFilePath, fileContent);
	}

	private string SerializeEvents(IEnumerable<Event> eventsToSerialize)
	{
		return JsonConvert.SerializeObject(new EventsBatch(eventsToSerialize));
	}

	private IEnumerable<Event> DeserializeEvents()
	{
		if (!File.Exists(_saveFilePath))
		{
			return null;
		}

		var jsonText = File.ReadAllText(_saveFilePath);

		try
		{
			return JsonConvert.DeserializeObject<EventsBatch>(jsonText).Events;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
