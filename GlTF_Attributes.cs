using UnityEngine;
using System.Collections;

public class GlTF_Attributes : GlTF_Writer {
	public GlTF_Accessor normalAccessor;
	public GlTF_Accessor positionAccessor;
	public GlTF_Accessor texCoord0Accessor;
	public GlTF_Accessor texCoord1Accessor;
	public GlTF_Accessor texCoord2Accessor;
	public GlTF_Accessor texCoord3Accessor;

	public void Populate (Mesh m)
	{
		positionAccessor.Populate (m.vertices);
		if (normalAccessor != null) 
		{
			normalAccessor.Populate (m.normals);
		}
		if (texCoord0Accessor != null) 
		{
			texCoord0Accessor.Populate (m.uv);
		}
		if (texCoord1Accessor != null)
		{
			texCoord1Accessor.Populate (m.uv2);
		}
		if (texCoord2Accessor != null) 
		{
			texCoord2Accessor.Populate (m.uv3);
		}
		if (texCoord3Accessor != null) 
		{
			texCoord3Accessor.Populate (m.uv4);
		}
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
