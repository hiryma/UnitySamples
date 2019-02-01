using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDetector : Graphic, IDragHandler
{
	public System.Action<Vector2> onDrag{ private get; set; }
	public void OnDrag(PointerEventData data)
	{
		if (onDrag != null)
		{
			onDrag(data.delta);
		}
	}

	protected override void OnPopulateMesh(VertexHelper vertexHelper)
	{
		vertexHelper.Clear();
	}
}
