using UnityEngine;
using System.Collections;
using System.Threading; 

public class ThreeDStereoCamera : MonoBehaviour {
	
	public enum CameraState
	{
		WindowsEditorState, // system camera effect
		WindowsStereoState,  // 3d effect with 3d screen
		AndroidRunTimeState, // android runtime effect, in windows editor show red-blue effect
	}
	public CameraState state;
	
	public float separation = 0.02f;
	public float zeroParallax = 10;
	public float fieldOfView = 60;
	public float near = 0.01f;
	public float far = 1000;
	public Color cameraBackgroundColor;
	
	public GameObject leftCamera;
	public GameObject rightCamera;
	public GameObject systemCamera;
	public GameObject zeroParallaxPlane;
	
	private RenderTexture rt;
	
	
	void Start(){
		foreach (Transform tran in transform) {
			switch(tran.name)
			{
			case "SystemCamera":
				systemCamera = tran.gameObject;
				break;
			case "LC":
				leftCamera = tran.gameObject;
				break;
			case "RC":
				rightCamera = tran.gameObject;
				break;
			case "ZeroParallaxPlane":
				zeroParallaxPlane = tran.gameObject;
				break;
			}
		}
		
		if(Application.platform == RuntimePlatform.Android)
		{
			state = CameraState.AndroidRunTimeState;
			systemCamera.SetActive(false);
			leftCamera.SetActive(true);
			rightCamera.SetActive(true);
			zeroParallaxPlane.SetActive(true);
		}	
		
		
		switch(state)
		{
		case CameraState.WindowsEditorState:
		{
			
			break;
		}
			
		case CameraState.WindowsStereoState:
		{
			initLRCameras ();
			break;
		}
			
		case CameraState.AndroidRunTimeState:
		{
			initCameras ();
			break;
		}
		}
		
		zeroParallaxPlane.GetComponent<MeshRenderer> ().enabled = false;
		
	}
	
	
	void createAnaglyphEffectCamera(){
		AnaglyphEffect anaglyphEffect = gameObject.AddComponent<AnaglyphEffect> ();
		anaglyphEffect.setRenderTexture (rt);
	}
	
	void initLRCameras(){
		
		int screenWidth = Screen.width;
		int screenHeight = Screen.height;
		rt = new RenderTexture (screenWidth, screenHeight / 2 ,24,RenderTextureFormat.ARGB32);
		rt.antiAliasing = 8;
		
		float screenAspect = (float)screenWidth / screenHeight;
		float projectHeight = CameraParaComputer.computeProjectHeight (zeroParallax, fieldOfView);
		float aspect = screenAspect + separation / projectHeight;
		
		GameObject lcgo = GameObject.Find ("ThreeDCamera/LC");
		Camera lc = lcgo.GetComponent<Camera> ();
		lc.aspect = aspect;
		lc.targetTexture = rt;
		ThreeDOneCamera lcamSP = lcgo.AddComponent<ThreeDOneCamera>();
		lcamSP.InitCameraType ( ThreeDOneCamera.CameraType.Left);
		
		GameObject rcgo = GameObject.Find ("ThreeDCamera/RC");
		Camera rc = rcgo.GetComponent<Camera> ();
		rc.aspect = aspect;
		rc.targetTexture = rt;
		ThreeDOneCamera rcamSP = rcgo.AddComponent<ThreeDOneCamera>();
		rcamSP.InitCameraType ( ThreeDOneCamera.CameraType.Right);
		
		gameObject.AddComponent<Camera>();
		camera.cullingMask = 0;
		
		createAnaglyphEffectCamera ();
	}
	
	void initCameras(){
		int screenWidth = Screen.width;
		int screenHeight = Screen.height;
		float screenAspect = (float)screenWidth / screenHeight;
		float projectHeight = CameraParaComputer.computeProjectHeight (zeroParallax, fieldOfView);
		float aspect = screenAspect + separation / projectHeight;
		
		GameObject lcgo = GameObject.Find ("ThreeDCamera/LC");
		Camera lc = lcgo.GetComponent<Camera> ();
		lc.aspect = aspect;
		ThreeDOneCamera lcamSP = lcgo.AddComponent<ThreeDOneCamera>();
		lcamSP.InitCameraType ( ThreeDOneCamera.CameraType.Left);
		
		GameObject rcgo = GameObject.Find ("ThreeDCamera/RC");
		Camera rc = rcgo.GetComponent<Camera> ();
		rc.aspect = aspect;
		ThreeDOneCamera rcamSP = rcgo.AddComponent<ThreeDOneCamera>();
		rcamSP.InitCameraType ( ThreeDOneCamera.CameraType.Right);
		
	}
	
	public void setCameraParameters(float _seperation, float _zeroParallax, float _fieldOfView)
	{
		this.separation = _seperation;
		this.zeroParallax = _zeroParallax;
		this.fieldOfView = _fieldOfView;
		
		switch (state) {
		case ThreeDStereoCamera.CameraState.WindowsEditorState:
		{
			systemCamera.SetActive(true);
			leftCamera.SetActive(false);
			rightCamera.SetActive(false);
			zeroParallaxPlane.SetActive(false);
			
			systemCamera.camera.nearClipPlane = near;
			systemCamera.camera.farClipPlane = far;
			systemCamera.camera.fieldOfView = fieldOfView;
			systemCamera.camera.backgroundColor = cameraBackgroundColor;
			break;
		}
		case ThreeDStereoCamera.CameraState.WindowsStereoState:
		{
			systemCamera.SetActive(false);
			leftCamera.SetActive(true);
			rightCamera.SetActive(true);
			zeroParallaxPlane.SetActive(true);
			
			setCameras();
			break;
		}
		case ThreeDStereoCamera.CameraState.AndroidRunTimeState:
		{
			systemCamera.SetActive(false);
			leftCamera.SetActive(true);
			rightCamera.SetActive(true);
			zeroParallaxPlane.SetActive(true);
			
			setCameras();
			break;
		}
		}
	}
	void setCameras(){
		float halfSep = this.separation / 2;
		leftCamera.transform.localPosition = new Vector3 (-halfSep, 0, 0);
		rightCamera.transform.localPosition = new Vector3 (halfSep, 0, 0);
		
		leftCamera.camera.nearClipPlane = this.near;
		leftCamera.camera.farClipPlane = this.far;
		
		rightCamera.camera.nearClipPlane = this.near;
		rightCamera.camera.farClipPlane = this.far;
		
		leftCamera.camera.fieldOfView = this.fieldOfView;
		rightCamera.camera.fieldOfView = this.fieldOfView;
		
		zeroParallaxPlane.transform.localPosition = new Vector3 (0, 0, this.zeroParallax);
		float zeroParallaxHeight = CameraParaComputer.computeProjectHeight (this.zeroParallax, this.fieldOfView);
		float zeroParallaxWidth = CameraParaComputer.computeProjectWidth (this.zeroParallax, this.fieldOfView, leftCamera.camera.aspect);
		float zeroParallaxInterWidth = CameraParaComputer.computeIntersectionWidthOfCameras (zeroParallaxWidth, this.separation);
		zeroParallaxPlane.transform.localScale = new Vector3 (zeroParallaxInterWidth, zeroParallaxHeight, 1);
	}
	
	void OnDrawGizmos() {
		
		Gizmos.color = Color.yellow;
		
		Gizmos.matrix = Matrix4x4.TRS(transform.localPosition,transform.localRotation,transform.lossyScale);
		Vector3 scale = new Vector3 (zeroParallaxPlane.transform.localScale.x,zeroParallaxPlane.transform.localScale.y,0);
		Gizmos.DrawWireCube(new Vector3 (0,0,this.zeroParallax), scale);
		
		//Gizmos.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
		//Gizmos.DrawSphere(transform.position,1.5f);
		
	}
	
	public void SetFieldOfView(float value)
	{
		this.fieldOfView = value;
		switch (state)
		{
		case CameraState.AndroidRunTimeState:
		case CameraState.WindowsStereoState:
			this.leftCamera.camera.fieldOfView = value;
			this.rightCamera.camera.fieldOfView = value;
			break;
		case CameraState.WindowsEditorState:
			this.systemCamera.camera.fieldOfView = value;
			break;
		}
	}
	
	public Camera getCamera()
	{
		switch (state)
		{
		case CameraState.AndroidRunTimeState:
		case CameraState.WindowsStereoState:
			return this.leftCamera.camera;
		default:
			return this.systemCamera.camera;
		}
	}
}
