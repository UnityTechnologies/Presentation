using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TextGradientVertexColoring : BaseMeshEffect
{

	public Gradient brandingGradient;

	private List<UIVertex> vertices = new List<UIVertex>();

	public override void ModifyMesh(VertexHelper vh)
	{
		if (IsActive())
		{
			vh.GetUIVertexStream(vertices);
			int count = vertices.Count;
			if (count == 0) return;
			float rightY = vertices[0].position.x;
			float leftY = vertices[0].position.x;

			for (int i = 1; i < count; i++)
			{
				float y = vertices[i].position.x;
				if (y > leftY) leftY = y;
				else if (y < rightY) rightY = y;
			}

			float uiElementHeight = leftY - rightY;

			UIVertex v = new UIVertex();
			for (int i = 0; i < vh.currentVertCount; i++)
			{
				vh.PopulateUIVertex(ref v, i);
				byte alpha = v.color.a;
				v.color = brandingGradient.Evaluate((v.position.x - rightY) / uiElementHeight);
				v.color.a = alpha;
				vh.SetUIVertex(v, i);
			}
		}
	}
}