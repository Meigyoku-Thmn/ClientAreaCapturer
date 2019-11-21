using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NHotkey.WindowsForms;

namespace ClientAreaCapturer {
   class Program {
      [DllImport("user32.dll")]
      static extern IntPtr GetForegroundWindow();
      [StructLayout(LayoutKind.Sequential)]
      public struct RECT {
         public int Left;        // x position of upper-left corner  
         public int Top;         // y position of upper-left corner  
         public int Right;       // x position of lower-right corner  
         public int Bottom;      // y position of lower-right corner  
      }
      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
      [DllImport("user32.dll")]
      static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
      [DllImport("user32", ExactSpelling = true, SetLastError = true)]
      static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs(UnmanagedType.U4)] int cPoints);
      static (Point, Size) GetClientDimention(IntPtr hwnd, bool getScrollbar = false) {
         GetClientRect(hwnd, out var rectangle);
         var rect = rectangle;
         MapWindowPoints(hwnd, IntPtr.Zero, ref rectangle, 2);
         return (new Point(rectangle.Left, rectangle.Top), new Size(rect.Right, rect.Bottom));
      }
      static void CaptureClientArea() {
         var foregroundHwnd = GetForegroundWindow();
         var (srcPoint, size) = GetClientDimention(foregroundHwnd);
         using (var bitmap = new Bitmap(size.Width, size.Height))
         using (var g = Graphics.FromImage(bitmap)) {
            g.CopyFromScreen(srcPoint, Point.Empty, size);
            Clipboard.SetImage(bitmap);
         }
      }
      static readonly Mutex mutex = new Mutex(true, "ClientAreaCapturer {604a8972-d0e3-4d65-a48c-bbeb030404ff}");
      [STAThread]
      static void Main(string[] args) {
         if (!mutex.WaitOne(TimeSpan.Zero, true)) return;
         // warning: this is a very simple client area capturing method. Menubar, scrollbar will not be included!
         // this maybe not work correctly on other DPI settings
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         var ni = new NotifyIcon {
            Text = "Client Area Capturer",
            Visible = true,
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
            ContextMenuStrip = new ContextMenus().Create()
         };
         HotkeyManager.Current.AddOrReplace(
            "CaptureClientArea", Keys.Control | Keys.Alt | Keys.PrintScreen, (sender, e) => CaptureClientArea());
         Application.Run();
         ni.Dispose();
         mutex.ReleaseMutex();
      }
   }
   class ContextMenus {
      public ContextMenuStrip Create() {
         var menu = new ContextMenuStrip();
         var item = new ToolStripMenuItem {
            Text = "Exit"
         };
         item.Click += (sender, e) => Application.Exit();
         menu.Items.Add(item);
         return menu;
      }
   }
}
