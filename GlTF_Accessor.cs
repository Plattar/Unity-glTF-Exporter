using UnityEngine;
using System.Collections;

public class GlTF_Accessor : GlTF_Writer {
	public GlTF_BufferView bufferView;//	"bufferView": "bufferView_30",
	public long byteOffset; //": 0,
	public int byteStride;// ": 12,
	public int componentType; // GL enum vals ": BYTE (5120), UNSIGNED_BYTE (5121), SHORT (5122), UNSIGNED_SHORT (5123), FLOAT (5126)
	public int count;//": 2399,
	public GlTF_Vector3 max = new GlTF_Vector3();//": [
	public GlTF_Vector3 min = new GlTF_Vector3();//": [
	public string aType = "SCALAR"; // ": "VEC3" NOTE: SHOULD BE ENUM, USE ToString to output it

	public GlTF_Accessor (string n) { name = n; }
	public GlTF_Accessor (string n, string t, string c) {
		name = n;
		aType = t;
		switch (t)
		{
		case "SCALAR":
			byteStride = 0;
			break;
		case "VEC2":
			byteStride = 8;
			break;
		case "VEC3":
			byteStride = 12;
			break;
		case "VEC4":
			byteStride = 16;
			break;
		}
		switch (c)
		{
		case "USHORT":
			componentType = 5123;
			break;
		case "FLOAT":
			componentType = 5126;
			break;
		}
	}

	public static string GetNameFromObject(Object o, string name) 
	{		 		
		return "accessor_" + name + "_"+ GlTF_Writer.GetNameFromObject(o, true);
	}

	public void Populate (int[] vs, bool flippedTriangle)
	{
		if (aType != "SCALAR")
			throw (new System.Exception());
		byteOffset = bufferView.currentOffset;
		bufferView.Populate (vs, flippedTriangle);
		count = vs.Length;
	}

	public void Populate (float[] vs)
	{
		if (aType != "SCALAR")
			throw (new System.Exception());
		byteOffset = bufferView.currentOffset;
		bufferView.Populate (vs);
		count = vs.Length;
	}

	public void Populate (Vector2[] v2s, bool flip = false)
	{
		if (aType != "VEC2")
			throw (new System.Exception());
		byteOffset = bufferView.currentOffset;	
		Bounds b = new Bounds();
		if (flip)
		{
			for (int i = 0; i < v2s.Length; i++)
			{
				bufferView.Populate (v2s[i].x);
				bufferView.Populate (1.0f - v2s[i].y);
				b.Encapsulate (v2s[i]);
			}
		} else {
			for (int i = 0; i < v2s.Length; i++)
			{
				bufferView.Populate (v2s[i].x);
				bufferView.Populate (v2s[i].y);
				b.Encapsulate (v2s[i]);
			}
		}
		count = v2s.Length;
		min.items[0] = b.min.x;
		min.items[1] = b.min.y;
		max.items[0] = b.max.x;
		max.items[1] = b.max.y;
	}

	public void Populate (Vector3[] v3s)
	{
		if (aType != "VEC3")
			throw (new System.Exception());
		Bounds b = new Bounds();
		byteOffset = bufferView.currentOffset;	
		for (int i = 0; i < v3s.Length; i++)
		{
			bufferView.Populate (v3s[i].x);
			bufferView.Populate (v3s[i].y);
			bufferView.Populate (-v3s[i].z);
			b.Encapsulate (v3s[i]);
		}
		count = v3s.Length;
		min.items[0] = b.min.x;
		min.items[1] = b.min.y;
		min.items[2] = b.min.z;
		max.items[0] = b.max.x;
		max.items[1] = b.max.y;
		max.items[2] = b.max.z;
	}

	public void Populate (Vector4[] v4s)
	{
		if (aType != "VEC4")
			throw (new System.Exception());
		//		Bounds b = new Bounds();
		byteOffset = bufferView.currentOffset;	
		for (int i = 0; i < v4s.Length; i++)
		{
			bufferView.Populate (v4s[i].x);
			bufferView.Populate (v4s[i].y);
			bufferView.Populate (v4s[i].z);
			bufferView.Populate (v4s[i].w);
			//			b.Expand (v4s[i]);
		}
		count = v4s.Length;
		/*
		min.items[0] = b.min.x;
		min.items[1] = b.min.y;
		min.items[2] = b.min.z;
		min.items[3] = b.min.w;
		max.items[0] = b.max.x;
		max.items[1] = b.max.y;
		max.items[2] = b.max.z;
		max.items[3] = b.max.w;
		*/
	}

	public override void Write ()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		Indent();		jsonWriter.Write ("\"bufferView\": \"" + bufferView.name+"\",\n");
		Indent();		jsonWriter.Write ("\"byteOffset\": " + byteOffset + ",\n");
		Indent();		jsonWriter.Write ("\"byteStride\": " + byteStride + ",\n");
		Indent();		jsonWriter.Write ("\"componentType\": " + componentType + ",\n");
		Indent();		jsonWriter.Write ("\"count\": " + count + ",\n");
		Indent();		jsonWriter.Write ("\"max\": [ ");
		max.WriteVals();
		jsonWriter.Write (" ],\n");
		Indent();		jsonWriter.Write ("\"min\": [ ");
		min.WriteVals();
		jsonWriter.Write (" ],\n");
		Indent();		jsonWriter.Write ("\"type\": \"" + aType + "\"\n");
		IndentOut();
		Indent();	jsonWriter.Write (" }");
	}
}
