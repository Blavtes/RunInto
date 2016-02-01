using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(ThreeDStereoCamera))]
public class StereoConfigEditor : Editor {

	ThreeDStereoCamera cameraTarget;
	
	public void OnEnable()
	{
		cameraTarget = (ThreeDStereoCamera)target;
//		SceneView.onSceneGUIDelegate = StereoUpdate;
		
		foreach (Transform tran in cameraTarget.transform) {
			switch(tran.name)
			{
			case "SystemCamera":
				cameraTarget.systemCamera = tran.gameObject;
				break;
			case "LC":
				cameraTarget.leftCamera = tran.gameObject;
				break;
			case "RC":
				cameraTarget.rightCamera = tran.gameObject;
				break;
			case "ZeroParallaxPlane":
				cameraTarget.zeroParallaxPlane = tran.gameObject;
				break;
			}
		}
	}
	
	public override void OnInspectorGUI()
	{
		EditorGUILayout.Separator();
		cameraTarget.state = (ThreeDStereoCamera.CameraState)EditorGUILayout.EnumPopup ("Camera Mode",cameraTarget.state);
		cameraTarget.cameraBackgroundColor = EditorGUILayout.ColorField ("BackGround",cameraTarget.cameraBackgroundColor);

		if(cameraTarget.state!=ThreeDStereoCamera.CameraState.WindowsEditorState)
		{
			cameraTarget.separation = EditorGUILayout.FloatField (new GUIContent("Separation"),cameraTarget.separation);
			cameraTarget.zeroParallax = EditorGUILayout.FloatField (new GUIContent("Zero Parallx"),cameraTarget.zeroParallax);
		}

		cameraTarget.fieldOfView = EditorGUILayout.Slider (new GUIContent("Field of View"),cameraTarget.fieldOfView,1,179);
		
		Rect rect = GUILayoutUtility.GetLastRect ();
		Rect frect = new Rect (rect.width * 0.41f, rect.y+rect.height+5, rect.width*0.6f, rect.height);
		
		EditorGUILayout.BeginHorizontal ();
		GUILayout.Label ("Clipping Planes");
		EditorGUILayout.BeginVertical ();
		cameraTarget.near = EditorGUI.FloatField (frect,"Near",cameraTarget.near);
		frect = new Rect (frect.x, frect.y+15, frect.width, frect.height);
		cameraTarget.far = EditorGUI.FloatField (frect,"Far",cameraTarget.far);
		
		EditorGUILayout.EndVertical ();
		EditorGUILayout.EndHorizontal ();
		
		GUILayout.Space (20);

		if (GUI.changed)
		{
			EditorUtility.SetDirty(cameraTarget);
			StereoUpdate(null);
		}
		
	}
	
	
	void StereoUpdate(SceneView sceneview)
	{
		if(cameraTarget==null) return;
		switch (cameraTarget.state) {
			case ThreeDStereoCamera.CameraState.WindowsEditorState:
			{
				cameraTarget.systemCamera.SetActive(true);
				cameraTarget.leftCamera.SetActive(false);
				cameraTarget.rightCamera.SetActive(false);
				cameraTarget.zeroParallaxPlane.SetActive(false);
				
				Camera sc = cameraTarget.systemCamera.GetComponent<Camera>();
				sc.nearClipPlane = cameraTarget.near;
				sc.farClipPlane = cameraTarget.far;
				sc.fieldOfView = cameraTarget.fieldOfView;
				sc.backgroundColor = cameraTarget.cameraBackgroundColor;
				cameraTarget.near = Mathf.Clamp (cameraTarget.near, 0.01f,cameraTarget.far);
				cameraTarget.far = Mathf.Clamp (cameraTarget.far, cameraTarget.near, Mathf.Infinity);
				sc.near = cameraTarget.near;
				sc.far = cameraTarget.far;
				break;
			}
			case ThreeDStereoCamera.CameraState.WindowsStereoState:
			{
				cameraTarget.systemCamera.SetActive(false);
				cameraTarget.leftCamera.SetActive(true);
				cameraTarget.rightCamera.SetActive(true);
				cameraTarget.zeroParallaxPlane.SetActive(true);
				
				computeCameras ();
				computeZeroParallaxPlane ();
				break;
			}
			case ThreeDStereoCamera.CameraState.AndroidRunTimeState:
			{
				cameraTarget.systemCamera.SetActive(false);
				cameraTarget.leftCamera.SetActive(true);
				cameraTarget.rightCamera.SetActive(true);
				cameraTarget.zeroParallaxPlane.SetActive(true);
				
				computeCameras ();
				computeZeroParallaxPlane ();
				break;
			}
		}
	}
	
	void computeCameras(){
		float halfSep = cameraTarget.separation / 2;
		GameObject lcgo = GameObject.Find ("ThreeDCamera/LC");
		lcgo.transform.localPosition = new Vector3 (-halfSep, 0, 0);
		GameObject rcgo = GameObject.Find("ThreeDCamera/RC");
		rcgo.transform.localPosition = new Vector3 (halfSep, 0, 0);
		
		Camera lc = lcgo.GetComponent<Camera>();
		lc.nearClipPlane = cameraTarget.near;
		lc.farClipPlane = cameraTarget.far;
		
		Camera rc = rcgo.GetComponent<Camera>();
		rc.nearClipPlane = cameraTarget.near;
		rc.farClipPlane = cameraTarget.far;
		
		cameraTarget.fieldOfView = Mathf.Clamp (cameraTarget.fieldOfView,1,179);
		cameraTarget.separation = Mathf.Clamp (cameraTarget.separation, 0,cameraTarget.separation);
		cameraTarget.zeroParallax = Mathf.Clamp (cameraTarget.zeroParallax ,0.0001f,Mathf.Infinity);
		cameraTarget.near = Mathf.Clamp (cameraTarget.near, 0.01f,cameraTarget.far);
		cameraTarget.far = Mathf.Clamp (cameraTarget.far, cameraTarget.near, Mathf.Infinity);
		
		lc.fieldOfView = cameraTarget.fieldOfView;
		rc.fieldOfView = cameraTarget.fieldOfView;
		
		lc.backgroundColor = cameraTarget.cameraBackgroundColor;
		rc.backgroundColor = cameraTarget.cameraBackgroundColor;
	}
	
	void computeZeroParallaxPlane(){
		GameObject zereoParallaxPlane = GameObject.Find ("ThreeDCamera/ZeroParallaxPlane");
		zereoParallaxPlane.transform.localPosition = new Vector3 (0, 0, cameraTarget.zeroParallax);
		Camera camera = GameObject.Find ("ThreeDCamera/LC").GetComponent<Camera>();
		float zeroParallaxHeight = CameraParaComputer.computeProjectHeight (cameraTarget.zeroParallax, camera.fieldOfView);
		float zeroParallaxWidth = CameraParaComputer.computeProjectWidth (cameraTarget.zeroParallax, camera.fieldOfView, camera.aspect);
		float zeroParallaxInterWidth = CameraParaComputer.computeIntersectionWidthOfCameras (zeroParallaxWidth, cameraTarget.separation);
		zereoParallaxPlane.transform.localScale = new Vector3 (zeroParallaxInterWidth, zeroParallaxHeight, 1);
	}

	
}