using UnityEngine;
using System.Collections;

public class GlTF_Technique : GlTF_Writer {
	public override void Write()
	{
		Indent();		jsonWriter.Write ("\"" + name + "\": {\n");
		Indent();		jsonWriter.Write ("}");
	}
}
