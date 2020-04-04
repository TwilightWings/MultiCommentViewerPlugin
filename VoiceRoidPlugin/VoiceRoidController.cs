using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace VoiceRoidPlugin
{
    class VoiceRoidController
	{
		StringBuilder oldSpeed = new StringBuilder(256);
		Options _options;

		Thread callLoopThread;
		Queue<string> callTasks = new Queue<string>();

		public VoiceRoidController(Options options)
		{
			_options = options;
			callLoopThread = new Thread(new ThreadStart(callLoop));
			callLoopThread.Start();
		}

		~VoiceRoidController()
		{
			callLoopThread.Abort();
		}

		public void queueMessage(string message)
		{
			lock( callTasks)
			{
				callTasks.Enqueue(message);
			}
		}

		private void callLoop()
		{
			while (true)
			{
				//taskを得るまで待機
				var task = getTask();

				//VoiceRoidが暇になるまで待機
				waitForNonBusy();

				//スピード付与
				if(_options.IsVoiceTypeSpecfied)
				{
					setTalkSpeed(_options.VoiceSpeed);
				}

				//読み上げ
				talk(task);

				//スピード付与の場合VoiceRoidが暇になるまで待機
				if(_options.IsVoiceTypeSpecfied)
				{
					//スピードを戻す
					waitForNonBusy();
					clearTalkSpeed();
				}
			}
		}

		private string getTask()
		{
			while (true)
			{
				lock (callTasks)
				{
					if (callTasks.Count > 0) return callTasks.Dequeue();
				}
				Thread.Sleep(500);
			}
		}

		private void waitForNonBusy()
		{
			while( isBusy())
			{
				Thread.Sleep(100);
			}
		}

		private bool isBusy()
		{
			string tag = GetButtenStat();
			if (tag == " 再生" | tag == "")
				return false;
			else
				return true;
		}

		public bool isEnable()
		{
			if (GetMainWindow() == IntPtr.Zero)
			{
				return false;
			}
			return true;
		}

		private void setTalkSpeed(int speed)
		{
			SendMessage(GetSpeedBox(), 0x000d, oldSpeed.Capacity, oldSpeed);

			SendMessage(GetSpeedBox(), 0x000c, 0, new StringBuilder((speed / 100.0).ToString("N1")));
			SendMessage(GetSpeedBox(), 0x0100, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0102, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0101, 0xd, 0x11C0001);
		}

		private void talk(string message)
		{
			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(message));
			SendMessage(GetButten(), 0x00f5, 0, 0);
			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(""));
		}

		private void clearTalkSpeed()
		{
			SendMessage(GetSpeedBox(), 0x000c, 0, oldSpeed);
			WINDOWINFO wi = new WINDOWINFO();
			do
			{
				GetWindowInfo(GetSpeedBox(), ref wi);
			} while ((wi.dwStyle & 0x08000000L) != 0);

			SendMessage(GetSpeedBox(), 0x0100, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0102, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0101, 0xd, 0x11C0001);
		}

		private void output(string mes, int speed)
		{
			SendMessage(GetSpeedBox(), 0x000d, oldSpeed.Capacity, oldSpeed);

			SendMessage(GetSpeedBox(), 0x000c, 0, new StringBuilder((speed / 100.0).ToString("N1")));
			SendMessage(GetSpeedBox(), 0x0100, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0102, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0101, 0xd, 0x11C0001);

			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(mes));
			SendMessage(GetButten(), 0x00f5, 0, 0);
			SendMessage(GetTextWindow(), 0x000c, 0, new StringBuilder(""));

			SendMessage(GetSpeedBox(), 0x000c, 0, oldSpeed);

			Thread rt = new Thread(new ThreadStart(ReturnSpeedSet_old));
			rt.Start();
		}

		private void ReturnSpeedSet_old()
		{
			WINDOWINFO wi = new WINDOWINFO();
			do
			{
				GetWindowInfo(GetSpeedBox(), ref wi);
			} while ((wi.dwStyle & 0x08000000L) != 0);

			SendMessage(GetSpeedBox(), 0x0100, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0102, 0xd, 0x11c0001);
			SendMessage(GetSpeedBox(), 0x0101, 0xd, 0x11C0001);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr FindWindow(
			string lpClassName, string lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, StringBuilder lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

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

		private IntPtr GetButten()
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

		private string GetButtenStat()
		{
			StringBuilder sb = new StringBuilder(256);

			SendMessage(GetButten(), 0x000d, sb.Capacity, sb);

			return sb.ToString();
		}

		private IntPtr GetSpeedBox()
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
			IntPtr edit1 = FindWindowEx(tabc2c, IntPtr.Zero, _options.editboxclassname, null);
			IntPtr edit2 = FindWindowEx(tabc2c, edit1, _options.editboxclassname, null);
			IntPtr edit3 = FindWindowEx(tabc2c, edit2, _options.editboxclassname, null);

			return edit3;
		}
	}
}
