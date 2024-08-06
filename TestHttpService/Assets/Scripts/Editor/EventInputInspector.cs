using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EventInputHandler))]
public class EventInputInspector : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var inputHandler = (EventInputHandler)target;

		if (GUILayout.Button("Track event"))
		{
			inputHandler.TrackEvent();
		}
	}
}
