using UnityEngine;
using System.Collections;

public class GlTF_Sampler : GlTF_Writer {
	public int magFilter = 9729;
	public int minFilter = 9729;
	public int wrapS = 10497;
	public int wrapT = 10497;

	public GlTF_Sampler (string n) { name = n; }

	public override void Write()
	{
		Indent();	jsonWriter.Write ("\"" + name + "\": {\n");
		IndentIn();
		Indent();	jsonWriter.Write ("\"magFilter\": " + magFilter + ",\n");
		Indent();	jsonWriter.Write ("\"minFilter\": " + minFilter + ",\n");
		Indent();	jsonWriter.Write ("\"wrapS\": " + wrapS + ",\n");
		Indent();	jsonWriter.Write ("\"wrapT\": " + wrapT + "\n");
		IndentOut();
		Indent();	jsonWriter.Write ("}");		
	}
}
