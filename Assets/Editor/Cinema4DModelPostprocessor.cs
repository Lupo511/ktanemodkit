using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Cinema4DModelPostprocessor : AssetPostprocessor
{
	private readonly Quaternion rotation = Quaternion.Euler(0, 180, 0);

	private void OnPostprocessModel(GameObject go)
	{
		var gameObjects = new List<GameObject>();
		var worldPositions = new List<Vector3>();
		var worldRotations = new List<Quaternion>();
		var queue = new Queue<GameObject>();
		queue.Enqueue(go);
		while (queue.Any())
		{
			var item = queue.Dequeue();
			gameObjects.Add(item);
			worldPositions.Add(item.transform.position);
			worldRotations.Add(item.transform.rotation);
			foreach (Transform childTransform in item.transform)
			{
				queue.Enqueue(childTransform.gameObject);
			}
		}

		for (int i = 0; i < gameObjects.Count; i++)
		{
			ApplyTransformRotation(gameObjects[i], worldPositions[i], worldRotations[i]);
			ApplyGeometryRotation(gameObjects[i]);
			//if (gameObjects[i] != go)
			{
				FixAnimation(go, gameObjects[i]);
			}
		}

		go.transform.rotation = Quaternion.identity;
	}

	private void FixAnimation(GameObject root, GameObject go)
	{
		var clips = AnimationUtility.GetAnimationClips(root);
		foreach (var clip in clips)
		{
			var curves = FindRotationCurves(clip, GetRelativePath(root, go));
			if (curves != null)
			{
				InvertCurve(curves[0].curve);
				InvertCurve(curves[2].curve);

				clip.SetCurve(curves[0].path, curves[0].type, curves[0].propertyName, curves[0].curve);
				clip.SetCurve(curves[2].path, curves[2].type, curves[2].propertyName, curves[2].curve);
			}

			curves = FindPositionCurves(clip, GetRelativePath(root, go));
			if (curves != null)
			{
				InvertCurve(curves[0].curve);
				InvertCurve(curves[2].curve);

				clip.SetCurve(curves[0].path, curves[0].type, curves[0].propertyName, curves[0].curve);
				clip.SetCurve(curves[2].path, curves[2].type, curves[2].propertyName, curves[2].curve);
			}
		}
	}

	private string GetRelativePath(GameObject root, GameObject child)
	{
		string path = "";
		var transform = child.transform;
		while (transform.gameObject != root)
		{
			if (path == "")
			{
				path = transform.name;
			}
			else
			{
				path = transform.name + "/" + path;
			}
			transform = transform.parent;
		}
		return path;
	}

	private AnimationClipCurveData[] FindRotationCurves(AnimationClip clip, string path)
	{
		AnimationClipCurveData xCurveData = null, yCurveData = null, zCurveData = null, wCurveData = null;

		var curves = AnimationUtility.GetAllCurves(clip, true);
		foreach (var curveData in curves)
		{
			if (curveData.path != path)
			{
				continue;
			}

			switch (curveData.propertyName)
			{
				case "m_LocalRotation.x":
					xCurveData = curveData;
					break;
				case "m_LocalRotation.y":
					yCurveData = curveData;
					break;
				case "m_LocalRotation.z":
					zCurveData = curveData;
					break;
				case "m_LocalRotation.w":
					wCurveData = curveData;
					break;
			}

			if (xCurveData != null && yCurveData != null && zCurveData != null && wCurveData != null)
			{
				return new[] { xCurveData, yCurveData, zCurveData, wCurveData };
			}
		}

		return null;
	}

	private AnimationClipCurveData[] FindPositionCurves(AnimationClip clip, string path)
	{
		AnimationClipCurveData xCurveData = null, yCurveData = null, zCurveData = null;

		var curves = AnimationUtility.GetAllCurves(clip, true);
		foreach (var curveData in curves)
		{
			if (curveData.path != path)
			{
				continue;
			}

			switch (curveData.propertyName)
			{
				case "m_LocalPosition.x":
					xCurveData = curveData;
					break;
				case "m_LocalPosition.y":
					yCurveData = curveData;
					break;
				case "m_LocalPosition.z":
					zCurveData = curveData;
					break;
			}

			if (xCurveData != null && yCurveData != null && zCurveData != null)
			{
				return new[] { xCurveData, yCurveData, zCurveData };
			}
		}

		return null;
	}

	private void InvertCurve(AnimationCurve curve)
	{
		var keyframes = curve.keys;

		for (int i = 0; i < keyframes.Length; i++)
		{
			var keyframe = keyframes[i];
			keyframe.value = -keyframe.value;
			keyframes[i] = keyframe;
		}

		curve.keys = keyframes;
	}

	private void ApplyTransformRotation(GameObject go, Vector3 position, Quaternion initialRotation)
	{
		go.transform.position = position;
		go.transform.rotation = initialRotation * rotation;
	}

	private void ApplyGeometryRotation(GameObject go)
	{
		var meshFilter = go.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			return;
		}

		var mesh = meshFilter.sharedMesh;
		var vertices = mesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = rotation * vertices[i];
		}

		mesh.vertices = vertices;
		var normals = mesh.normals;
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = rotation * normals[i];
		}

		mesh.normals = normals;
		meshFilter.sharedMesh.RecalculateBounds();
		mesh.name = go.name;
	}
}