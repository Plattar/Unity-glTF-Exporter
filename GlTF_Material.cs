using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Material : GlTF_Writer {

	public class Value : GlTF_Writer {
	}

	public class ColorValue : Value {
		public Color color;

		public override void Write()
		{
			jsonWriter.Write ("\"" + name + "\": [");
			jsonWriter.Write (color.r.ToString() + ", " + color.g.ToString() + ", " +color.b.ToString() + ", " + color.a.ToString());
			jsonWriter.Write ("]");
		}
	}

	public class VectorValue : Value {
		public Vector4 vector;

		public override void Write()
		{
			jsonWriter.Write ("\"" + name + "\": [");
			jsonWriter.Write (vector.x.ToString() + ", " + vector.y.ToString() + ", " + vector.z.ToString() + ", " + vector.w.ToString());
			jsonWriter.Write ("]");
		}
	}

	public class FloatValue : Value {
		public float value;

		public override void Write()
		{
			jsonWriter.Write ("\"" + name + "\": " + value + "");
		}
	}

	public class StringValue : Value {
		public string value;

		public override void Write()
		{
			jsonWriter.Write ("\"" + name + "\": \"" + value + "\"");
		}
	}

	public class DictValue: Value
	{
		public Dictionary<string, string> value;
		public DictValue()
		{
			value = new Dictionary<string, string>();
		}
		public override void Write()
		{
			jsonWriter.Write("\""+ name + "\": {\n");
			IndentIn();

			foreach (string key in value.Keys)
			{
				CommaNL();
				Indent();  jsonWriter.Write("\"" + key + "\" : \"" + value[key] + "\"");
			}
			IndentOut();
			jsonWriter.Write("}");
		}
	}

	public string instanceTechniqueName = "technique1";
	public GlTF_ColorOrTexture ambient;// = new GlTF_ColorRGBA ("ambient");
	public GlTF_ColorOrTexture diffuse;
	public string materialModel = "PBR_metal_roughness";
	public float shininess;
	public GlTF_ColorOrTexture specular;// = new GlTF_ColorRGBA ("specular");
	public List<Value> values = new List<Value>();

	public static string GetNameFromObject(Object o)
	{
		return "material_" + GlTF_Writer.GetNameFromObject(o, true);
	}

	public override void Write()
	{
		//Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		//IndentIn();
		//CommaNL();
		//Indent();		jsonWriter.Write ("\"technique\": \"" + instanceTechniqueName + "\",\n");
		//Indent();		jsonWriter.Write ("\"values\": {\n");
		//IndentIn();

		Indent(); jsonWriter.Write("\"" + name + "\": {\n");
		IndentIn();
		//Indent();		jsonWriter.Write ("\"technique\": \"" + instanceTechniqueName + "\",\n");
		Indent(); jsonWriter.Write("\"extensions\": {\n");
		IndentIn();
		Indent(); jsonWriter.Write("\"FRAUNHOFER_materials_pbr\": {\n");
		IndentIn();
		CommaNL();
		Indent(); jsonWriter.Write("\"materialModel\": \"" + materialModel + "\",\n");
		Indent(); jsonWriter.Write("\"values\": {\n");
		IndentIn();
		foreach (var v in values)
		{
			CommaNL();
			Indent();	v.Write();
		}


//		if (ambient != null)
//		{
//			CommaNL();
//			ambient.Write ();
//		}
//		if (diffuse != null)
//		{
//			CommaNL();
//			diffuse.Write ();
//		}
//		CommaNL();
//		Indent();		jsonWriter.Write ("\"shininess\": " + shininess);
//		if (specular != null)
//		{
//			CommaNL();
//			specular.Write ();
//		}
//		jsonWriter.WriteLine();

		jsonWriter.Write ("\n");
		IndentOut();
		Indent(); jsonWriter.Write ("}");
		jsonWriter.Write("\n");
		IndentOut();
		Indent(); jsonWriter.Write("}");
		jsonWriter.Write("\n");
		IndentOut();
		Indent(); jsonWriter.Write("},\n");
		CommaNL();
		Indent();		jsonWriter.Write ("\"name\": \"" + name + "\"\n");
		IndentOut();
		Indent();		jsonWriter.Write ("}");

	}

}
