using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace DTE10T_WPF.Services
{
    public static class NotificationService
    {
        private static IntPtr _windowHandle = IntPtr.Zero;
        private static bool _isRegistered;
        private static readonly object _lockObj = new object();

        private static string _lastNotificationMessage = string.Empty;
        private static DateTime _lastNotificationTime = DateTime.MinValue;
        private const int NotificationCooldownSeconds = 30;

        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_INFO = 0x00000010;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIIF_INFO = 0x00000001;
        private const uint NIIF_ERROR = 0x00000003;
        private const uint NIIF_WARNING = 0x00000002;
        private const uint NIIF_NONE = 0x00000000;

        public static void Initialize(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                _windowHandle = windowHandle;
            }
        }

        public static void RegisterTrayIcon()
        {
            lock (_lockObj)
            {
                if (_isRegistered)
                    return;

                if (_windowHandle == IntPtr.Zero)
                    return;

                var data = new NOTIFYICONDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
                data.hWnd = _windowHandle;
                data.uID = 1;
                data.uFlags = NIF_ICON | NIF_TIP;
                data.szTip = "DTE10T_WPF";

                try
                {
                    using (var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/DTE10T_WPF;component/Resources/App.ico"))?.Stream)
                    {
                        if (iconStream != null)
                        {
                            var icon = new System.Drawing.Icon(iconStream);
                            data.hIcon = icon.Handle;
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (Shell_NotifyIcon(NIM_ADD, ref data))
                {
                    _isRegistered = true;
                }
            }
        }

        public static void UnregisterTrayIcon()
        {
            lock (_lockObj)
            {
                if (!_isRegistered)
                    return;

                var data = new NOTIFYICONDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
                data.hWnd = _windowHandle;
                data.uID = 1;

                Shell_NotifyIcon(NIM_DELETE, ref data);
                _isRegistered = false;
            }
        }

        public static void ShowNotification(string title, string message, string subtitle = "")
        {
            try
            {
                if (!ShouldShowNotification(message))
                    return;

                string fullMessage = string.IsNullOrEmpty(subtitle) ? message : $"{subtitle}\n{message}";
                ShowBalloonTip(title, fullMessage, BalloonIcon.Info);
                UpdateLastNotification(message);
            }
            catch (Exception)
            {
            }
        }

        public static void ShowErrorNotification(string message)
        {
            try
            {
                if (!ShouldShowNotification(message))
                    return;

                ShowBalloonTip("DTE10T_WPF - 错误", message, BalloonIcon.Error);
                UpdateLastNotification(message);
            }
            catch (Exception)
            {
            }
        }

        public static void ShowFatalNotification(string message)
        {
            try
            {
                ShowBalloonTip("DTE10T_WPF - 严重错误", message, BalloonIcon.Error);
                UpdateLastNotification(message);
            }
            catch (Exception)
            {
            }
        }

        public static void ShowAlarmNotification(string channel, string alarmType, double value, double limit)
        {
            try
            {
                string message = $"{channel} {alarmType}\n当前值: {value}℃, 限值: {limit}℃";
                if (!ShouldShowNotification(message))
                    return;

                ShowBalloonTip("DTE10T_WPF - 警报触发", message, BalloonIcon.Warning);
                UpdateLastNotification(message);
            }
            catch (Exception)
            {
            }
        }

        private static bool ShouldShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var now = DateTime.Now;
            var timeDiff = now - _lastNotificationTime;

            if (timeDiff.TotalSeconds < NotificationCooldownSeconds &&
                _lastNotificationMessage == message)
            {
                return false;
            }

            return true;
        }

        private static void UpdateLastNotification(string message)
        {
            _lastNotificationMessage = message;
            _lastNotificationTime = DateTime.Now;
        }

        private static void ShowBalloonTip(string title, string message, BalloonIcon icon)
        {
            lock (_lockObj)
            {
                if (!_isRegistered && _windowHandle != IntPtr.Zero)
                {
                    RegisterTrayIcon();
                }

                if (!_isRegistered)
                    return;

                var data = new NOTIFYICONDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
                data.hWnd = _windowHandle;
                data.uID = 1;
                data.uFlags = NIF_INFO;
                data.szInfoTitle = title;
                data.szInfo = message;
                data.uTimeoutOrVersion = 5000;

                switch (icon)
                {
                    case BalloonIcon.Error:
                        data.dwInfoFlags = NIIF_ERROR;
                        break;
                    case BalloonIcon.Warning:
                        data.dwInfoFlags = NIIF_WARNING;
                        break;
                    default:
                        data.dwInfoFlags = NIIF_INFO;
                        break;
                }

                Shell_NotifyIcon(NIM_MODIFY, ref data);
            }
        }

        private enum BalloonIcon
        {
            None = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
    }
}
