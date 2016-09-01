using UnityEngine;
using System.Collections;

public class GlTF_Attributes : GlTF_Writer {
	public GlTF_Accessor normalAccessor;
	public GlTF_Accessor positionAccessor;
	public GlTF_Accessor texCoord0Accessor;
	public GlTF_Accessor texCoord1Accessor;

	public void Populate (Mesh m)
	{
		positionAccessor.Populate (m.vertices);
		normalAccessor.Populate (m.normals);
		texCoord0Accessor.Populate (m.uv);
	}

	public override void Write ()
	{
		Indent();	jsonWriter.Write ("\"attributes\": {\n");
		IndentIn();
		if (normalAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"NORMAL\": \"" + normalAccessor.name + "\"");
		}
		if (normalAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"POSITION\": \"" + positionAccessor.name + "\"");
		}
		if (normalAccessor != null)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\"TEXCOORD_0\": \"" + texCoord0Accessor.name + "\"");
		}
		//CommaNL();
		jsonWriter.WriteLine();
		IndentOut();
		Indent();	jsonWriter.Write ("}");
	}

}
