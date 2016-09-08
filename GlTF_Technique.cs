using UnityEngine;
using System.Collections;

public class GlTF_Technique : GlTF_Writer {
	public string program;

	public static string GetNameFromObject(Object o) 
	{		 		
		return "technique_" + GlTF_Writer.GetNameFromObject(o);
	}

	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();		
		Indent();		jsonWriter.Write ("\"program\": \"" + program +"\"\n");
		IndentOut();
		Indent();		jsonWriter.Write ("}");
	}
}
