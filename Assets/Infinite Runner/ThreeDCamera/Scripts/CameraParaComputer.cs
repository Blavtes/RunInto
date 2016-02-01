using UnityEngine;
using System.Collections;

public class CameraParaComputer{
	//投影面高度
	public static float computeProjectHeight(float d, float fov){
		return 2 * d * Mathf.Tan (fov * 0.5f * Mathf.Deg2Rad);
	}
	//投影面的宽度
	public static float computeProjectWidth(float d, float fov, float aspect){
		return aspect*computeProjectHeight (d, fov);
	}
	//求出零视差面的宽度
	public static float computeIntersectionWidthOfCameras(float projectWidth, float separation){
		return projectWidth - separation;
	}
	//为了求出零视差面，对应在近截面上的宽度
	public static float offsetXOfNearPlane(float projectWidth, float interProjectWidth, float nearPlaneWidth){
		return nearPlaneWidth * (projectWidth - interProjectWidth) / projectWidth;
	}

	//计算非对称矩阵
	public static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far) {
		float x = 2.0F * near / (right - left);
		float y = 2.0F * near / (top - bottom);
		float a = (right + left) / (right - left);
		float b = (top + bottom) / (top - bottom);
		float c = -(far + near) / (far - near);
		float d = -(2.0F * far * near) / (far - near);
		float e = -1.0F;
		Matrix4x4 m = new Matrix4x4();
		m[0, 0] = x;m[0, 1] = 0;m[0, 2] = a;m[0, 3] = 0;
		m[1, 0] = 0;m[1, 1] = y;m[1, 2] = b;m[1, 3] = 0;
		m[2, 0] = 0;m[2, 1] = 0;m[2, 2] = c;m[2, 3] = d;
		m[3, 0] = 0;m[3, 1] = 0;m[3, 2] = e;m[3, 3] = 0;
		return m;
	}
}
