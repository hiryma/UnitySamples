using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class LightOrienter : MonoBehaviour
	{
		[SerializeField] Light attachedLight;

		void Start()
		{
			if (attachedLight == null)
			{
				attachedLight = gameObject.GetComponent<Light>();
			}
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(LightOrienter))]
		public class MyEditor : Editor
		{
			Vector3 from;
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var newFrom = EditorGUILayout.Vector3Field("from", from);
				if (newFrom != from)
				{
					var self = target as LightOrienter;
					if (self != null)
					{
						from = newFrom;
						var pos = self.transform.localPosition;
						self.transform.LookAt(pos - from);
					}
				}
			}
		}
#endif
	}
}
