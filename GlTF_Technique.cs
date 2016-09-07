using UnityEngine;
using System.Collections;

public class GlTF_Technique : GlTF_Writer {

	static public string GetNameFromShader(Shader s) 
	{		 
		var ret = "technique_" + s.name + "_" + s.GetInstanceID();
		ret = ret.Replace(" ", "_");
		return ret;
	}

	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		Indent();		jsonWriter.Write ("}");
	}
}
