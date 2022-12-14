using System;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
    public static class QScreen
    {
		static Texture2D CaptureTexture2d=null;
		public static async Task<Texture2D> Capture()
		{
			if (!CaptureRunning)
			{
				QToolManager.Instance.StartCoroutine(CaptureCoroutine());
				CaptureRunning = true;
			}
			await QTask.Wait(() => !CaptureRunning);
			return CaptureTexture2d;
		}
		static WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
		static bool CaptureRunning = false;
		static IEnumerator CaptureCoroutine()
		{
			yield return waitForEndOfFrame;
			if (CaptureTexture2d == null || CaptureTexture2d.width != Screen.width || CaptureTexture2d.height != Screen.height)
			{
				CaptureTexture2d = new Texture2D(Screen.width, Screen.height,TextureFormat.RGB24,false);
			}
			CaptureTexture2d.ReadPixels(new Rect(0, 0, Screen.width, Screen.width), 0, 0);
			CaptureTexture2d.Apply();
			CaptureRunning = false;
		}
		static bool IsDrag = false;
		static void OnGUI()
		{
			IsDrag = Event.current.mousePosition.y < 40 && Event.current.isMouse;
		}
		static void OnUpdate()
		{
			if(IsDrag&&CurWindow != default)
			{
#if PLATFORM_STANDALONE_WIN
				ReleaseCapture();
				SendMessage(CurWindow, 0xA1, 0x02, 0);
				SendMessage(CurWindow, 0x0202, 0, 0);
#endif
			}
		}
		public static void SetResolution(int width, int height, bool fullScreen)
		{
			

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.LinuxPlayer:
					Screen.SetResolution(width, height, fullScreen);
					break;
				default:
					Screen.SetResolution(width, height, true);
					break;
			}
#if UNITY_EDITOR
			SetSize(width, height);
#else
			if (coroutine != null)
			{
				QToolManager.Instance.StopCoroutine(coroutine);
				coroutine = null;
			}
			QToolManager.Instance.StartCoroutine(SetNoBorder(width, height));
#endif
		}
		static Coroutine coroutine = null;
		static IntPtr CurWindow = default;
		static IEnumerator SetNoBorder(int width,int height)
		{
			CurWindow = default;
			yield return new WaitForEndOfFrame();
			if (Time.timeScale > 0)
			{
				yield return new WaitForFixedUpdate();
			}
			else
			{
				Time.timeScale = 1;
				yield return new WaitForFixedUpdate();
				Time.timeScale = 0;
			}
			
			if (!Screen.fullScreen)
			{
#if PLATFORM_STANDALONE_WIN
				CurWindow = GetForegroundWindow();
				SetWindowLong(CurWindow, GWL_STYLE, WS_POPUP);
				SetWindowPos(CurWindow, 0, (Screen.currentResolution.width - width) / 2, (Screen.currentResolution.height - height) / 2, width, height, SWP_SHOWWINDOW);
#endif
				QToolManager.OnGUIEvent += OnGUI;
				QToolManager.OnUpdate += OnUpdate;
			}
			else
			{
				QToolManager.OnGUIEvent -= OnGUI;
				QToolManager.OnUpdate -= OnUpdate;
			}
		}
		#region ?????????????????????

#if PLATFORM_STANDALONE_WIN
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

		[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint =nameof(GetForegroundWindow))]
		static extern IntPtr GetForegroundWindow();

		[System.Runtime.InteropServices.DllImport("user32.DLL")]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		//???????????????????????????
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool ReleaseCapture();
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

		const uint SWP_SHOWWINDOW = 0x0040;
		const int GWL_STYLE = -16;
		const int WS_BORDER = 1; //window with border
		const int WS_POPUP = 0x800000;
#endif


#if UNITY_EDITOR

		static object gameViewSizesInstance;
        static MethodInfo getGroup;

        static QScreen()
        {
            // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            getGroup = sizesType.GetMethod("GetGroup");
            gameViewSizesInstance = instanceProp.GetValue(null, null);
        }

        private enum GameViewSizeType
        {
            AspectRatio, FixedResolution
        }

        private static void SetSize(int index)
        {
            var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var gvWnd = EditorWindow.GetWindow(gvWndType);
            selectedSizeIndexProp.SetValue(gvWnd, index, null);
        }



        private static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {

            var group = GetGroup(sizeGroupType);
            var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
            var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var ctor = gvsType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
            foreach (var c in gvsType.GetConstructors())
            {
                if (ctor == null && c.GetParameters().Length == 4)
                {
                    ctor = c;
                    break;
                }
            }
            var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
            addCustomSize.Invoke(group, new object[] { newSize });
        }


        private static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
        {
            var group = GetGroup(sizeGroupType);
            var groupType = group.GetType();
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            var getCustomCount = groupType.GetMethod("GetCustomCount");
            int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var gvsType = getGameViewSize.ReturnType;
            var widthProp = gvsType.GetProperty("width");
            var heightProp = gvsType.GetProperty("height");
            var indexValue = new object[1];
            for (int i = 0; i < sizesCount; i++)
            {
                indexValue[0] = i;
                var size = getGameViewSize.Invoke(group, indexValue);
                int sizeWidth = (int)widthProp.GetValue(size, null);
                int sizeHeight = (int)heightProp.GetValue(size, null);
                if (sizeWidth == width && sizeHeight == height)
                    return i;
            }
            return -1;
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }


        private static GameViewSizeGroupType GetCurrentGroupType()
        {
            var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
            return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
        }


        private static void SetSize(int width, int height)
        {
            int index = FindSize(GetCurrentGroupType(), width, height);
            if (index == -1)
            {
                AddCustomSize(GameViewSizeType.FixedResolution, GetCurrentGroupType(), width, height, width+"x"+height);
                index = FindSize(GetCurrentGroupType(), width, height);
            }
            if (index != -1)
            {
                SetSize(index);
            }
            else
            {
                Debug.LogError("????????????????????????????????? " + width.ToString() + "*" + height.ToString());
            }
        }
#endif
		#endregion

	}
}
