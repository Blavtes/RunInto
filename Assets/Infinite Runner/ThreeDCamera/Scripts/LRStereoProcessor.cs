using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LRStereoProcessor : MonoBehaviour {

	public static int LRMODE_LEFT_RIGHT=0;
	public static int LRMODE_RIGHT_LEFT=1;
	public static int STERE_MODE_2D=0;
	public static int STERE_MODE_3D=1;
	private static List<Mesh> mMeshs = new List<Mesh>();
    public static Vector2[] WUVS = new Vector2[] { new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1) };
    public static Vector2[] LUVS = new Vector2[] { new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 1) };
    public static Vector2[] RUVS = new Vector2[] { new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0.5f, 1) };
	public static int LRMode=LRMODE_LEFT_RIGHT;
	public static void RenderLeft(){

        foreach(MeshUVItem mMeshUVItem in mMeshItemList){
            Mesh mesh = mMeshUVItem.mesh;
			if(mMeshUVItem.stereMode==STERE_MODE_3D)
			{
				if(LRMode==LRMODE_LEFT_RIGHT)
				{
                    mesh.uv = mMeshUVItem.LUVS;
				}else
				{
                    mesh.uv = mMeshUVItem.RUVS;
				}
				if(mMeshUVItem.gameObject!=null)
				{
					float aboveHalf= mesh.uv[0].x>=0.5?1.0f:0.0f;
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Half",aboveHalf);
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Mode",mMeshUVItem.mode);
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Offset",-mMeshUVItem.textureOffset);
					mMeshUVItem.gameObject.renderer.material.mainTextureOffset=new Vector2(-mMeshUVItem.textureOffset,0);
				}
			}
            else
			{
				mesh.uv=WUVS;
				if(mMeshUVItem.gameObject!=null)
				{
					float aboveHalf=-1.0f;
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Half",aboveHalf);
					mMeshUVItem.gameObject.renderer.material.mainTextureOffset=new Vector2(-mMeshUVItem.textureOffset,0);
				}
			}
        }
	}

	public static void RenderRight (){

        foreach (MeshUVItem mMeshUVItem in mMeshItemList)
        {
            Mesh mesh = mMeshUVItem.mesh;
			if (mMeshUVItem.stereMode== STERE_MODE_3D)
            {
                if (LRMode == LRMODE_LEFT_RIGHT)
                {
                    mesh.uv = mMeshUVItem.RUVS;
                }
                else
                {
                    mesh.uv = mMeshUVItem.LUVS;
                }
				if(mMeshUVItem.gameObject!=null)
				{
					float aboveHalf= mesh.uv[0].x>=0.5?1.0f:0.0f;
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Half",aboveHalf);
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Mode",mMeshUVItem.mode);
					mMeshUVItem.gameObject.renderer.material.SetFloat("_Offset",mMeshUVItem.textureOffset);
					mMeshUVItem.gameObject.renderer.material.mainTextureOffset=new Vector2(mMeshUVItem.textureOffset,0);
				}
            }
        }
	}

    public class MeshUVItem
    {
		public GameObject gameObject;
		public float textureOffset;
        public Mesh mesh;
		public float mode;
		public int stereMode;
        public Vector2[] LUVS;
        public Vector2[] RUVS;
    }

    public static List<MeshUVItem> mMeshItemList = new List<MeshUVItem>();

	public static MeshUVItem getMeshUVItem(GameObject gameObject)
	{
		foreach(MeshUVItem meshUVItem in mMeshItemList)
		{
			if(meshUVItem.gameObject!=null && meshUVItem.gameObject==gameObject)
			{
				return meshUVItem;
			}
		}
		return null;

	}

    public static void addMeshItem(MeshUVItem mMeshUVItem)
    {
        if (mMeshUVItem != null)
        {
            mMeshItemList.Add(mMeshUVItem);
        }
    }

    public static void removeMesh(MeshUVItem mMeshUVItem)
    {
        mMeshItemList.Remove(mMeshUVItem);
        mMeshUVItem.mesh.uv = RUVS;
    }
}
