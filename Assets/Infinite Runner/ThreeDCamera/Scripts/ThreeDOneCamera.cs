using UnityEngine;
using System.Collections;

public class ThreeDOneCamera : MonoBehaviour {
	public enum CameraType 
	{ 	Left,
		Right
	};
	private CameraType mCameraType;
	private static ThreeDStereoCamera msThreeDStereoCamera;
	
	public void InitCameraType(CameraType cameraType){
		mCameraType = cameraType;
	}
	
	void Start(){
		msThreeDStereoCamera = transform.parent.GetComponent<ThreeDStereoCamera>();
	}
	
	void LateUpdate() {
		Camera cam = camera;
		if (camera != null && needReComputeProjection()) {
			float bottom = -CameraParaComputer.computeProjectHeight(cam.nearClipPlane, cam.fieldOfView)/2;
			float top = - bottom;
			float nearPlaneWidth = CameraParaComputer.computeProjectWidth(cam.nearClipPlane, cam.fieldOfView, cam.aspect);
			float right = nearPlaneWidth/2;
			float left = - right;
			
			float zeroParallaxW = CameraParaComputer.computeProjectWidth(msThreeDStereoCamera.zeroParallax,cam.fieldOfView,cam.aspect);
			float zeroParallaxInterW = CameraParaComputer.computeIntersectionWidthOfCameras(zeroParallaxW, msThreeDStereoCamera.separation);
			float offsetX = CameraParaComputer.offsetXOfNearPlane(zeroParallaxW,zeroParallaxInterW,nearPlaneWidth);
			if(mCameraType == CameraType.Left){
				left += offsetX;
			}else{
				right -= offsetX;
			}
			Matrix4x4 m = CameraParaComputer.PerspectiveOffCenter(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
			cam.projectionMatrix = m;		
		}
	}
	
	bool needReComputeProjection(){
		return true;
	}
	
	void OnPreRender()
	{
		if (mCameraType == CameraType.Left) {
			LRStereoProcessor.RenderLeft ();
		} else {
			LRStereoProcessor.RenderRight();
		}
	}
}
