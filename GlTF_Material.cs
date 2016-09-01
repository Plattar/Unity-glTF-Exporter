using UnityEngine;
using System.Collections;

public class GlTF_Material : GlTF_Writer {

	public string instanceTechniqueName = "technique1";
	public GlTF_ColorOrTexture ambient;// = new GlTF_ColorRGBA ("ambient");
	public GlTF_ColorOrTexture diffuse;
	public float shininess;
	public GlTF_ColorOrTexture specular;// = new GlTF_ColorRGBA ("specular");

	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"instanceTechnique\": {\n");
		IndentIn();
		CommaNL();
		Indent();		jsonWriter.Write ("\"technique\": \"" + instanceTechniqueName + "\",\n");
		Indent();		jsonWriter.Write ("\"values\": {\n");
		IndentIn();
		if (ambient != null)
		{
			CommaNL();
			ambient.Write ();
		}
		if (diffuse != null)
		{
			CommaNL();
			diffuse.Write ();
		}
		CommaNL();
		Indent();		jsonWriter.Write ("\"shininess\": " + shininess);
		if (specular != null)
		{
			CommaNL();
			specular.Write ();
		}
		jsonWriter.WriteLine();
		IndentOut();
		Indent();		jsonWriter.Write ("}");
		jsonWriter.WriteLine();
		IndentOut();
		Indent();		jsonWriter.Write ("},\n");
		Indent();		jsonWriter.Write ("\"name\": \"" + name + "\"\n");
		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}

}
