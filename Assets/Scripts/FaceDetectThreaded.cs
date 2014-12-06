using UnityEngine;
using System.Collections;
using System.Threading;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp;
using System;

public class FaceDetectThreaded : MonoBehaviour
{
	public int Width = 640;
	public int Height = 480;
	public int FPS = 30;

	public bool Mirror = false;

	public Camera Camera;
	Camera _Camera;

	static VideoCapture video;

	public int VideoIndex = 0;

	static Mat capImage;
	static byte[] cvtImageJpeg;
	static Texture2D texture;

	static CascadeClassifier cascade;
	static int faceX;
	static int faceY;

	public GameObject go;

	private Thread captureThread;
	private Thread faceThread;
	private static System.Object capSyncObj = new System.Object();
	private static System.Object faceSyncObj = new System.Object();

	static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
	static long lastTime;
	static float fps;


	// Use this for initialization
	void Awake()
	{
		// カメラを列挙する
		// 使いたいカメラのインデックスをVideoIndexに入れる
		// 列挙はUnityで使うのはOpenCVだけど、インデックスは同じらしい
		var devices = WebCamTexture.devices;
		for ( int i = 0; i < devices.Length; i++ )
		{
			print( string.Format( "index {0}:{1}", i, devices[i].name ) );
		}

		// ビデオの設定
		video = new VideoCapture( VideoIndex );
		video.Set( CaptureProperty.FrameWidth, Width );
		video.Set( CaptureProperty.FrameHeight, Height );
		video.Set( CaptureProperty.Fps, FPS );

		print( string.Format( "{0},{1}", Width, Height ) );

		capImage = new Mat();

		// 顔検出器の作成
		cascade = new CascadeClassifier( Application.dataPath + @"/haarcascade_frontalface_alt.xml" );

		// テクスチャの作成
		texture = new Texture2D( Width, Height, TextureFormat.RGB24, false );
		renderer.material.mainTexture = texture;

		// 変換用のカメラの作成
		_Camera = GameObject.Find( Camera.name ).camera;
		print( string.Format( "({0},{1})({2},{3})", Screen.width, Screen.height, _Camera.pixelWidth, _Camera.pixelHeight ) );

		// Thread
		captureThread = new Thread( captureThreadWork );
		captureThread.Start();

		faceThread = new Thread( faceThreadWork );
		faceThread.Start();

		// 顔認識スレッドのFPS計測のためStopWatch
		sw.Start();

	}

	private void captureThreadWork()
	{
		while ( true )
		{
			Thread.Sleep( 1 );
			var job = new FaceDetectThreaded.CaptureJob();
			job.Work();
		}
	}

	private void faceThreadWork()
	{
		while ( true )
		{
			Thread.Sleep( 1 );
			var job = new FaceDetectThreaded.FaceJob();
			job.Work();
		}
	}

	// Update is called once per frame
	void Update()
	{
		lock ( capSyncObj )
		{
			if (cvtImageJpeg != null)
			{
				texture.LoadImage(cvtImageJpeg);
				texture.Apply();
			}
		}

		// カメラ画像のミラー表示 ON/OFF
		Vector3 tf = transform.localScale;
		float tfx = tf.x;
		if (Mirror)
		{
			if (tf.x > 0) tfx = -1 * tf.x;
		}
		else
		{
			if (tf.x < 0) tfx = -1 * tf.x;
		}
		transform.localScale = new Vector3(tfx, tf.y, tf.z);

		if ( go != null )
		{
			var _faceX = faceX;
			if (Mirror) _faceX = Width - _faceX;
			go.transform.localPosition = Vector2ToVector3( new Vector2( _faceX, faceY ) );
		}

	}

	void OnGUI()
	{
		float w = Screen.width;
		float h = Screen.height;

		GUI.Label(
			new UnityEngine.Rect( w * 0.0f, h * 0.0f, w * 0.5f, h * 0.1f ),
			"FaceRecognition FPS:" + fps.ToString( "0.00000" )
		);
	}

	public class CaptureJob
	{
		public CaptureJob()
		{
		}

		public void Work()
		{
			// Webカメラから画像を取得する
			lock (faceSyncObj)
			{
				video.Read( capImage );
			}

			lock ( capSyncObj )
			{
				cvtImageJpeg = capImage.ImEncode(".jpg");
			}
		}
	}

	public class FaceJob
	{
		public FaceJob()
		{
		}

		public void Work()
		{
			// 顔認識スレッド用に画像をコピー
			Mat capImageCopy;
			lock (faceSyncObj)
			{
				capImageCopy = capImage.Clone();
			}

			// 顔を検出する
			var faces = cascade.DetectMultiScale(capImageCopy);
			if ( faces.Length > 0 )
			{
				var face = faces[0];

				// 中心の座標を計算する
				faceX = face.TopLeft.X + (face.Size.Width / 2);
				faceY = face.TopLeft.Y + (face.Size.Height / 2);
			}

			// 顔検出スレッドのFPS計算
			long currentTime = sw.ElapsedMilliseconds;
			fps = 1000.0f / (float)( currentTime - lastTime );
			if ( fps < 0 )
			{
				Debug.Log( "fps error:" + fps + " lastTime: " + lastTime );
			}
			lastTime = currentTime;
		}
	}

	void OnApplicationQuit()
	{
		captureThread.Abort();
		faceThread.Abort();

		if ( video != null )
		{
			video.Dispose();
			video = null;
		}
	}

	/// <summary>
	/// OpenCVの2次元座標をUnityの3次元座標に変換する
	/// </summary>
	/// <param name="vector2"></param>
	/// <returns></returns>
	private Vector3 Vector2ToVector3( Vector2 vector2 )
	{
		if ( Camera == null )
		{
			throw new Exception( "" );
		}

		// スクリーンサイズで調整(WebCamera->Unity)
		vector2.x = vector2.x * Screen.width / Width;
		vector2.y = vector2.y * Screen.height / Height;

		// Unityのワールド座標系(3次元)に変換
		var vector3 = _Camera.ScreenToWorldPoint( vector2 );

		// 座標の調整
		// Y座標は逆、Z座標は0にする(Xもミラー状態によって逆にする必要あり)
		vector3.y *= -1;
		vector3.z = 0;

		return vector3;
	}
}
