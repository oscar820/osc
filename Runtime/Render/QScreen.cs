using System;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
    public static class QScreen
    {
		static Texture2D CaptureTexture2d=null;
		public static Texture Capture()
		{
			if (CaptureTexture2d == null|| CaptureTexture2d.width != Screen.width || CaptureTexture2d.height != Screen.height)
			{
				CaptureTexture2d = new Texture2D(Screen.width, Screen.height);
			}
			CaptureTexture2d.ReadPixels(new Rect(0, 0, Screen.width, Screen.width), 0, 0);
			CaptureTexture2d.Apply();
			return CaptureTexture2d;
		}
		public static async void SetResolution(int width, int height, bool fullScreen, bool hasBorder = true)
		{

#if PLATFORM_STANDALONE_WIN
			var window = GetForegroundWindow();
#endif

#if UNITY_EDITOR
			SetSize(width, height);
			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
					Screen.SetResolution(width, height, fullScreen);
					break;
				default:
					Screen.SetResolution(width, height, true);
					break;
			}
#else

#if PLATFORM_STANDALONE_WIN
			await Task.Delay(5);
			var style= GetWindowLong(window, GWL_STYLE);
			SetWindowLong(window, GWL_STYLE, ( hasBorder?( style | WS_CAPTION) :( style & ~WS_CAPTION)));
#endif

#endif


		}
		#region 分辨率设置逻辑

#if PLATFORM_STANDALONE_WIN
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();
		[System.Runtime.InteropServices.DllImport("USER32.DLL")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		public const int GWL_STYLE = -16;
		public const int WS_CHILD = 0x40000000; //child window
		public const int WS_BORDER = 0x00800000; //window with border
		public const int WS_DLGFRAME = 0x00400000; //window with double border but no title
		public const int WS_CAPTION = WS_BORDER | WS_DLGFRAME; //window with a title bar
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
                Debug.LogError("设置游戏视窗分辨率出错 " + width.ToString() + "*" + height.ToString());
            }
        }
#endif
		#endregion

	}
}
