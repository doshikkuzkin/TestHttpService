using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class EventService : MonoBehaviour
{
	private const int MillisecondsInSecond = 1000;
	private static string SaveFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

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
	private string _saveFilePath;

	private float _nextSendTime;

	private void Awake()
	{
		_saveFilePath = Path.Combine(SaveFolderPath, _saveFileName);

		UpdateNextSendTime(Time.unscaledTime);
		Initialize(TimeSpan.FromMilliseconds(_timeoutMs));
	}

	private void Update()
	{
		var currentTime = Time.unscaledTime;

		if (_nextSendTime > currentTime)
		{
			UpdateNextSendTime(currentTime);

			Task.Run(() => SendEvents());
		}
	}

	public void TrackEvent(string type, string content)
	{
		var httpEvent = new Event(type, content);
		_cachedEvents.Add(httpEvent);

		StoreEvents();
	}

	private void Initialize(TimeSpan timeout)
	{
		_httpClient.Timeout = timeout;
	}

	private async Task SendEvents()
	{
		var content = new StringContent(SerializeEvents());

		var request = new HttpRequestMessage(HttpMethod.Post, _serviceUrl);
		request.Content = content;

		try
		{
			var response = await _httpClient.SendAsync(request);

			if (response.IsSuccessStatusCode)
			{
				var responseText = await response.Content.ReadAsStringAsync();

				Debug.Log($"Success response received: {responseText}");

				ClearEvents();
			}
		}
		catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException timeoutEx)
		{
			Debug.Log($"Request timed out: {ex.Message}, {timeoutEx.Message}");
		}
		catch (Exception ex)
		{
			Debug.Log($"Request exception occured: {ex.Message}");
		}
	}

	private void StoreEvents()
	{
		File.WriteAllText(_saveFilePath, SerializeEvents());
	}

	private void ClearEvents()
	{
		_cachedEvents.Clear();

		File.WriteAllText(_saveFilePath, string.Empty);
	}

	private string SerializeEvents()
	{
		return JsonConvert.SerializeObject(new { events = _cachedEvents });
	}

	private void UpdateNextSendTime(float currentTime)
	{
		_nextSendTime = currentTime + (float)_cooldownMs / MillisecondsInSecond;
	}
}
