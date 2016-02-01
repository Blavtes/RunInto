using UnityEngine;
using System.Collections;

public class AnaglyphEffect : MonoBehaviour {

	private Material anaglyphMat = new Material (Shader.Find("Anaglyph"));

	public void setRenderTexture(RenderTexture rt){
		anaglyphMat.SetTexture ("_MainTex", rt);
	}

	void OnRenderImage ( RenderTexture source, RenderTexture destination ) {
		RenderTexture.active = destination;
		GL.PushMatrix();
		GL.LoadOrtho();
		for(int i = 0; i < anaglyphMat.passCount; i++) {
			anaglyphMat.SetPass(i);
			DrawQuad();
		}
		GL.PopMatrix();
	}
	
	private void DrawQuad() {
		GL.Begin (GL.QUADS);      
		GL.TexCoord2( 0.0f, 1.0f ); GL.Vertex3( 0.0f, 1.0f, 0 );
		GL.TexCoord2( 1.0f, 1.0f ); GL.Vertex3( 1.0f, 1.0f, 0 );
		GL.TexCoord2( 1.0f, 0.0f ); GL.Vertex3( 1.0f, 0.0f, 0 );
		GL.TexCoord2( 0.0f, 0.0f ); GL.Vertex3( 0.0f, 0.0f, 0 );
		GL.End();
	}
}
