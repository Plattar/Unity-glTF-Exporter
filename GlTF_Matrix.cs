using UnityEngine;
using System.Collections;

public class GlTF_Matrix : GlTF_FloatArray {
	public GlTF_Matrix() { minItems = 16; maxItems = 16; items = new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }; }
}
