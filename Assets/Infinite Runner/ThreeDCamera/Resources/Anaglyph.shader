Shader "Anaglyph" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
SubShader {
   Pass {
      ZTest Always Cull Off ZWrite Off
      Fog { Mode off }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma fragmentoption ARB_precision_hint_fastest
      #include "UnityCG.cginc"
      
      uniform sampler2D _MainTex;
      
      struct v2f {
         float4 pos : POSITION;
         float2 uv : TEXCOORD0;
      };
      
      v2f vert( appdata_img v )
      {
         v2f o;
         o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
         float2 uv = MultiplyUV( UNITY_MATRIX_TEXTURE0, v.texcoord );
         o.uv = uv;
         return o;
      }
      
      half4 frag (v2f i) : COLOR
      {
         float2 lUV = float2(i.uv.x/2, i.uv.y);
         float2 rUV = float2(i.uv.x/2 + 0.5f, i.uv.y);
         float4 texL = tex2D(_MainTex, lUV);
         float4 texR = tex2D(_MainTex, rUV);
         float4 texRGB;
         
         texRGB = float4(texL.r,texR.g,texR.b,1);
         return texRGB;
      }
      ENDCG
   }
}
	FallBack "Diffuse"
}
