using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace VoiceRoidPlugin
{
    class VoiceRoidController
	{
        private readonly StringBuilder _oldSpeed = new StringBuilder(256);
        private readonly StringBuilder _oldVolume = new StringBuilder(256);
        private readonly StringBuilder _oldTone = new StringBuilder(256);
        private readonly StringBuilder _oldIntonation = new StringBuilder(256);
        private readonly Options _options;

		public VoiceRoidController(Options options)
		{
			_options = options;
		}

        public void TalkMessageNow(string message)
        {
            //VoiceRoidが暇になるまで待機
            WaitForNonBusy();

            //スピード付与
            if (_options.IsVoiceTypeSpecfied)
            {
                SetEffects(
                    _options.VoiceVolume,
                    _options.VoiceSpeed,
                    _options.VoiceTone,
                    _options.VoiceIntonation);
            }

            //読み上げ
            Talk(message);

            WaitForNonBusy();

            //スピード付与の場合VoiceRoidが暇になるまで待機
            if (_options.IsVoiceTypeSpecfied)
            {
                //スピードを戻す
                ClearEffect();
            }
        }

		private void WaitForNonBusy()
		{
			while( IsBusy() )
			{
				Thread.Sleep(100);
			}
		}

		private bool IsBusy()
		{
			string tag = GetButtonStat();
			if (tag == " 再生" | tag == "")
				return false;
			else
				return true;
		}

		public bool IsEnable()
		{
			if (GetMainWindow() == IntPtr.Zero)
			{
				return false;
			}
			return true;
		}

		private void SetEffects(int volume, int speed, int tone, int intonation)
		{
			SetEffectsElement(GetVolumeBox(), _oldVolume, volume);
            SetEffectsElement(GetSpeedBox(), _oldSpeed, speed);
            SetEffectsElement(GetToneBox(), _oldTone, tone);
            SetEffectsElement(GetIntonationBox(), _oldIntonation, intonation);
		}

        private void SetEffectsElement(IntPtr targetForm, StringBuilder oldCache, int newValue)
        {
            SendMessage(targetForm, 0x000d, oldCache.Capacity, oldCache);

            SendMessage(targetForm, 0x000c, 0, new StringBuilder((newValue / 100.0).ToString("N1")));
            SendMessage(targetForm, 0x0100, 0xd, 0x11c0001);
            SendMessage(targetForm, 0x0102, 0xd, 0x11c0001);
            SendMessage(targetForm, 0x0101, 0xd, 0x11C0001);
		}

		private void Talk(string message)
		{
			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(message));
			SendMessage(GetButton(), 0x00f5, 0, 0);
			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(""));
		}

		private void ClearEffect()
		{
            ClearEffectsElement(GetVolumeBox(), _oldVolume);
            ClearEffectsElement(GetSpeedBox(), _oldSpeed);
            ClearEffectsElement(GetToneBox(), _oldTone);
            ClearEffectsElement(GetIntonationBox(), _oldIntonation);
		}

        private void ClearEffectsElement(IntPtr targetForm, StringBuilder oldCache)
        {
            SendMessage(targetForm, 0x000c, 0, oldCache);
            WINDOWINFO wi = new WINDOWINFO();
            do
            {
                GetWindowInfo(targetForm, ref wi);
            } while ((wi.dwStyle & 0x08000000L) != 0);

            SendMessage(targetForm, 0x0100, 0xd, 0x11c0001);
            SendMessage(targetForm, 0x0102, 0xd, 0x11c0001);
            SendMessage(targetForm, 0x0101, 0xd, 0x11C0001);
		}
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr FindWindow(
			string lpClassName, string lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, int wParam, StringBuilder lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, int wParam, int lParam);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct WINDOWINFO
		{
			public uint cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public uint cxWindowBorders;
			public uint cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;

			public WINDOWINFO(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
			{
				cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
			}
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		private IntPtr GetMainWindow()
		{
			IntPtr r = FindWindow(_options.mainclassname, _options.titlename);

			if (r == IntPtr.Zero)
			{
				r = FindWindow(_options.mainclassname, _options.titlename + "*");
			}

			return r;
		}

		private IntPtr GetTextWindow()
		{
			IntPtr child = FindWindowEx(GetMainWindow(), IntPtr.Zero, _options.mainclassname, null);

			IntPtr buf = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);
			child = FindWindowEx(child, buf, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			return FindWindowEx(child, IntPtr.Zero, _options.richtextclassname, null);
		}

		private IntPtr GetButton()
		{
			IntPtr child = FindWindowEx(GetMainWindow(), IntPtr.Zero, _options.mainclassname, null);

			IntPtr buf = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);
			child = FindWindowEx(child, buf, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			buf = FindWindowEx(child, IntPtr.Zero, _options.mainclassname, null);

			child = FindWindowEx(child, buf, _options.mainclassname, null);
			return FindWindowEx(child, IntPtr.Zero, _options.buttenclassname, null);
		}

		private string GetButtonStat()
		{
			StringBuilder sb = new StringBuilder(256);

			SendMessage(GetButton(), 0x000d, sb.Capacity, sb);

			return sb.ToString();
		}

        private IntPtr GetSettingTabArea()
        {
            IntPtr c = FindWindowEx(GetMainWindow(), IntPtr.Zero, _options.mainclassname, null);
            IntPtr cc = FindWindowEx(c, IntPtr.Zero, _options.mainclassname, null);
            IntPtr cc2 = FindWindowEx(c, cc, _options.mainclassname, null);
            IntPtr cc2c = FindWindowEx(cc2, IntPtr.Zero, _options.mainclassname, null);
            IntPtr cc2cc = FindWindowEx(cc2c, IntPtr.Zero, _options.mainclassname, null);
            IntPtr cc2cc2 = FindWindowEx(cc2c, cc2cc, _options.mainclassname, null);
            IntPtr tab = FindWindowEx(cc2cc2, IntPtr.Zero, _options.tabclassname, null);
            if (tab == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            IntPtr tabc = FindWindowEx(tab, IntPtr.Zero, _options.mainclassname, "音声効果");
            while (tabc == IntPtr.Zero)
            {
                SendMessage(tab, 0x0201, 0x1, 0x800b7);
                Thread.Sleep(100);
                tabc = FindWindowEx(tab, IntPtr.Zero, _options.mainclassname, "音声効果");
            }
            IntPtr tabc2c = FindWindowEx(tabc, IntPtr.Zero, _options.mainclassname, null);

            return tabc2c;
        }

		private IntPtr GetSpeedBox()
        {
            IntPtr tabc2c = GetSettingTabArea();
			IntPtr edit1 = FindWindowEx(tabc2c, IntPtr.Zero, _options.editboxclassname, null);
			IntPtr edit2 = FindWindowEx(tabc2c, edit1, _options.editboxclassname, null);
			IntPtr edit3 = FindWindowEx(tabc2c, edit2, _options.editboxclassname, null);

			return edit3;
		}

        private IntPtr GetVolumeBox()
		{
			IntPtr tabc2c = GetSettingTabArea();
			IntPtr edit1 = FindWindowEx(tabc2c, IntPtr.Zero, _options.editboxclassname, null);
            IntPtr edit2 = FindWindowEx(tabc2c, edit1, _options.editboxclassname, null);
            IntPtr edit3 = FindWindowEx(tabc2c, edit2, _options.editboxclassname, null);
            IntPtr edit4 = FindWindowEx(tabc2c, edit3, _options.editboxclassname, null);

			return edit4;
		}

        private IntPtr GetToneBox()
		{
			IntPtr tabc2c = GetSettingTabArea();
			IntPtr edit1 = FindWindowEx(tabc2c, IntPtr.Zero, _options.editboxclassname, null);
            IntPtr edit2 = FindWindowEx(tabc2c, edit1, _options.editboxclassname, null);

            return edit2;
		}

        private IntPtr GetIntonationBox()
		{
			IntPtr tabc2c = GetSettingTabArea();
			IntPtr edit1 = FindWindowEx(tabc2c, IntPtr.Zero, _options.editboxclassname, null);

            return edit1;
		}
	}
}
