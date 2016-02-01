using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class ThreeDDevice{
	[DllImport ("USDS3D")]
	private static extern int usdCreateDevice(int type, ref int device, bool isJavaThread);
	
	[DllImport ("USDS3D")]
	private static extern int usdDestoryDevice(int device);
	
	[DllImport ("USDS3D")]
	private static extern int usdEnable3D(int device, bool withTrack);
	
	[DllImport ("USDS3D")]
	private static extern int usdDisable3D(int device);
	
	[DllImport ("USDS3D")]
	private static extern int usdSetTexture(int device, uint target, uint texture,
	                                        int format);
	
	[DllImport ("USDS3D")]
	private static extern int usdDraw(int device);
	
	[DllImport ("USDS3D")]
	private static extern int usdSetTextureTransformMatrix(int device,float[] mat);
	
	[DllImport ("USDS3D")]
	private static extern int usdInitEGLContext (int device, int screenWidth, int screenHeight,
	                                             int viewLeft, int viewTop, int viewWidth, int viewHeight);
	
	[DllImport ("USDS3D")]
	private static extern int uShowTrackView (int device, bool show);
	
	private int mDeviceId;
	private static int SD_DRIVE_GLES20 = 0;
	private static int SD_OK = 0;
	private static uint GL_TEXTURE_2D = 3553;
	private static int SD_ORDER_LEFT_RIGHT = 0;
	private static int SD_ORDER_RIGHT_LEFT = 1;
	
	private ThreeDDevice(){
	}
	
	~ThreeDDevice(){
		destroyDevice ();
	}
	
	public static ThreeDDevice createThreeDDevice(){
		int deviceId = -1;
		try{
			Debug.Log("Create Start");
			if (SD_OK == usdCreateDevice (SD_DRIVE_GLES20, ref deviceId, false)) {
				Debug.Log("Create End");
				ThreeDDevice ThreeDDevice = new ThreeDDevice ();
				ThreeDDevice.mDeviceId = deviceId;
				//			float[] mat = {
				//				1f,0f,0f,0f,
				//				0f,-1f,0f,0f,
				//				0f,0f,1f,0f,
				//				0f,1f,0f,0f
				//			};
				//usdSetTextureTransformMatrix(ThreeDDevice.mDeviceId, mat);
				return ThreeDDevice;
			} else {
				return null;
			}
		}catch(System.DllNotFoundException e){
			Debug.Log(e.Message);
			return null;
		}
	}
	
	public bool destroyDevice(){
		return usdDestoryDevice (mDeviceId) == SD_OK;
	}
	
	public bool enableTrack(bool withTrack){
		return usdEnable3D (mDeviceId, withTrack) == SD_OK;
	}
	
	public bool disable3D(){
		return usdDisable3D (mDeviceId) == SD_OK;
	}
	
	public bool setTexture(uint textureId){
		return usdSetTexture (mDeviceId, GL_TEXTURE_2D, textureId, SD_ORDER_LEFT_RIGHT) == SD_OK;
	}
	
	public bool draw(){
		return usdDraw (mDeviceId) == SD_OK;
	}
	
	public bool initEGLContext (int screenWidth, int screenHeight,
	                            int viewLeft, int viewTop, int viewWidth, int viewHeight){
		return usdInitEGLContext (mDeviceId, screenWidth, screenHeight,viewLeft, 
		                          viewTop, viewWidth, viewHeight) == SD_OK;
	}
	
	public bool showTrackView(bool show){
		return uShowTrackView (mDeviceId, show) == SD_OK;
	}
}
