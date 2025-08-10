using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace UzunsClicker_V2
{
    public partial class MainWindow : Window
    {
        // Windows API çağrıları
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, MouseHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Structs ve Delegates
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public delegate IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Mouse event flags
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        // Hotkey modifiers
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;

        // Mouse button constants
        private const int VK_LBUTTON = 0x01;   // Sol mouse
        private const int VK_RBUTTON = 0x02;   // Sağ mouse  
        private const int VK_MBUTTON = 0x04;   // Orta mouse (wheel click)
        private const int VK_XBUTTON1 = 0x05;  // Mouse Button 4 (Back)
        private const int VK_XBUTTON2 = 0x06;  // Mouse Button 5 (Forward)

        // Mouse hook constants
        private const int WH_MOUSE_LL = 14;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_MBUTTONDOWN = 0x0207;

        // Global değişkenler
        private bool isAppActive = false;
        private bool isClicking = false;
        private bool isDarkTheme = true;
        private bool isListeningForHotkey = false;
        private bool isWaitingForSecondKey = false;
        private bool isMousePolling = false;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationTokenSource mousePollingToken;
        private int clickSpeed = 1;
        private int clickTarget = 0;
        private int totalClicks = 0;
        private bool useLeftClick = true;
        private bool useToggleMode = true;
        private int hotkeyId = 1;
        private uint hotkeyModifiers = 0;
        private uint hotkeyVirtualKey = 0x70; // F1
        private string currentHotkeyText = "F1";
        private string currentLanguage = "tr";
        private uint pendingModifiers = 0;
        private bool isUsingMouseHotkey = false;
        private IntPtr mouseHook = IntPtr.Zero;
        private MouseHookProc mouseProc;

        // Dil sözlükleri
        private Dictionary<string, Dictionary<string, string>> languages;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Varsayılan değerler
                currentLanguage = "tr";
                isDarkTheme = true;
                isAppActive = false;

                InitializeLanguages();

                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;
                this.KeyDown += MainWindow_KeyDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama başlatma hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeLanguages()
        {
            try
            {
                languages = new Dictionary<string, Dictionary<string, string>>();

                // Türkçe dil paketi
                var turkishDict = new Dictionary<string, string>
                {
                    ["title"] = "🎮 UzunsClicker-V2",
                    ["start"] = "▶️ BAŞLAT",
                    ["stop"] = "⏹️ DURDUR",
                    ["ready"] = "💤 Hazır",
                    ["active"] = "Aktif",
                    ["inactive"] = "Pasif",
                    ["running"] = "Çalışıyor",
                    ["completed"] = "Tamamlandı",
                    ["unlimited"] = "∞",
                    ["hotkeyHelper"] = "Tuş atamak için tıklayın",
                    ["developer"] = "👨‍💻 Ömer Uzunsoy tarafından geliştirildi",
                    ["waitingForKey"] = "Tuşa basın...",
                    ["waitingForMainKey"] = "Ana tuşa basın...",
                    // Başlıklar
                    ["statsTitle"] = "📊 İstatistikler",
                    ["statusTitle"] = "⚡ Durum",
                    ["speedTitle"] = "⚡ Click Hızı (CPS)",
                    ["clickTypeTitle"] = "🖱️ Click Türü",
                    ["targetTitle"] = "🎯 Tıklama Hedefi",
                    ["hotkeyTitle"] = "⌨️ Kısayol Tuşu",
                    ["workModeTitle"] = "🔄 Çalışma Modu",
                    // Alt metinler
                    ["totalText"] = "💥 Toplam:",
                    ["targetText"] = "🎯 Hedef:",
                    ["targetHelper"] = "(0 = Sınırsız)",
                    // Radio butonlar
                    ["leftClick"] = "👆 Sol Click",
                    ["rightClick"] = "👉 Sağ Click",
                    ["toggleMode"] = "🔘 Aç/Kapa",
                    ["holdMode"] = "⏸️ Basılı Tut",
                    // Butonlar
                    ["reset"] = "🔄 Reset"
                };

                // İngilizce dil paketi
                var englishDict = new Dictionary<string, string>
                {
                    ["title"] = "🎮 UzunsClicker-V2",
                    ["start"] = "▶️ START",
                    ["stop"] = "⏹️ STOP",
                    ["ready"] = "💤 Ready",
                    ["active"] = "Active",
                    ["inactive"] = "Inactive",
                    ["running"] = "Running",
                    ["completed"] = "Completed",
                    ["unlimited"] = "∞",
                    ["hotkeyHelper"] = "Click to set hotkey",
                    ["developer"] = "👨‍💻 Developed by Ömer Uzunsoy",
                    ["waitingForKey"] = "Press a key...",
                    ["waitingForMainKey"] = "Press main key...",
                    // Başlıklar
                    ["statsTitle"] = "📊 Statistics",
                    ["statusTitle"] = "⚡ Status",
                    ["speedTitle"] = "⚡ Click Speed (CPS)",
                    ["clickTypeTitle"] = "🖱️ Click Type",
                    ["targetTitle"] = "🎯 Click Target",
                    ["hotkeyTitle"] = "⌨️ Hotkey",
                    ["workModeTitle"] = "🔄 Work Mode",
                    // Alt metinler
                    ["totalText"] = "💥 Total:",
                    ["targetText"] = "🎯 Target:",
                    ["targetHelper"] = "(0 = Unlimited)",
                    // Radio butonlar
                    ["leftClick"] = "👆 Left Click",
                    ["rightClick"] = "👉 Right Click",
                    ["toggleMode"] = "🔘 Toggle",
                    ["holdMode"] = "⏸️ Hold",
                    // Butonlar
                    ["reset"] = "🔄 Reset"
                };

                languages["tr"] = turkishDict;
                languages["en"] = englishDict;

                if (string.IsNullOrEmpty(currentLanguage))
                {
                    currentLanguage = "tr";
                }
            }
            catch (Exception ex)
            {
                languages = new Dictionary<string, Dictionary<string, string>>();
                languages["tr"] = new Dictionary<string, string>
                {
                    ["start"] = "BAŞLAT",
                    ["stop"] = "DURDUR",
                    ["ready"] = "Hazır",
                    ["active"] = "Aktif",
                    ["inactive"] = "Pasif",
                    ["running"] = "Çalışıyor"
                };
                currentLanguage = "tr";

                System.Diagnostics.Debug.WriteLine($"InitializeLanguages Error: {ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (languages == null || !languages.Any())
                {
                    InitializeLanguages();
                }

                RegisterHotkeys();
                InitializeMouseHook();
                UpdateDisplay();
                UpdateLanguage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yükleme hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                currentLanguage = "tr";
                if (btnMainAction != null) btnMainAction.Content = "BAŞLAT";
                if (lblStatus != null) lblStatus.Text = "Hazır";
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                StopClicking();
                StopMousePolling();

                // Hooks temizle
                UnregisterHotKey(new System.Windows.Interop.WindowInteropHelper(this).Handle, hotkeyId);
                if (mouseHook != IntPtr.Zero)
                    UnhookWindowsHookEx(mouseHook);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closing Error: {ex.Message}");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                source?.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSourceInitialized Error: {ex.Message}");
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (isListeningForHotkey)
            {
                HandleHotkeySelection(e.Key);
                e.Handled = true;
            }
        }

        // Mouse Hook Sistemi
        private void InitializeMouseHook()
        {
            try
            {
                mouseProc = MouseHookCallback;
                mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc,
                    GetModuleHandle("user32"), 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse Hook Error: {ex.Message}");
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && isListeningForHotkey)
                {
                    // Mouse tuşu dinleme
                    if (wParam.ToInt32() == WM_XBUTTONDOWN)
                    {
                        // X1 veya X2 tuşu basıldı
                        var mouseData = Marshal.ReadInt32(lParam, 16);
                        if ((mouseData >> 16) == 1) // X1 (Back)
                        {
                            SetMouseHotkey("Mouse4");
                            return (IntPtr)1; // Consume event
                        }
                        else if ((mouseData >> 16) == 2) // X2 (Forward)
                        {
                            SetMouseHotkey("Mouse5");
                            return (IntPtr)1; // Consume event
                        }
                    }
                    else if (wParam.ToInt32() == WM_MBUTTONDOWN) // Wheel click
                    {
                        SetMouseHotkey("MouseWheel");
                        return (IntPtr)1; // Consume event
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MouseHookCallback Error: {ex.Message}");
            }

            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private void SetMouseHotkey(string mouseButton)
        {
            try
            {
                isListeningForHotkey = false;
                isUsingMouseHotkey = true;
                currentHotkeyText = mouseButton;

                // Mouse tuşları için özel handling
                if (mouseButton == "Mouse4")
                    hotkeyVirtualKey = VK_XBUTTON1;
                else if (mouseButton == "Mouse5")
                    hotkeyVirtualKey = VK_XBUTTON2;
                else if (mouseButton == "MouseWheel")
                    hotkeyVirtualKey = VK_MBUTTON;

                if (btnHotkeySelector != null)
                    btnHotkeySelector.Content = $"🖱️ {mouseButton}";
                if (lblHotkeyHelper != null)
                    lblHotkeyHelper.Text = GetText("hotkeyHelper");

                // Mouse tuşları için polling sistemi başlat
                StartMousePolling();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetMouseHotkey Error: {ex.Message}");
            }
        }

        // Mouse Polling Sistemi
        private void StartMousePolling()
        {
            try
            {
                StopMousePolling();

                isMousePolling = true;
                mousePollingToken = new CancellationTokenSource();

                Task.Run(() => MousePollingLoop(mousePollingToken.Token));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartMousePolling Error: {ex.Message}");
            }
        }

        private void StopMousePolling()
        {
            try
            {
                isMousePolling = false;
                mousePollingToken?.Cancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopMousePolling Error: {ex.Message}");
            }
        }

        private async void MousePollingLoop(CancellationToken cancellationToken)
        {
            try
            {
                bool wasPressed = false;

                while (!cancellationToken.IsCancellationRequested && isMousePolling && isAppActive)
                {
                    bool mousePressed = false;

                    // Mouse tuşu durumunu kontrol et
                    if (hotkeyVirtualKey == VK_XBUTTON1 && (GetAsyncKeyState(VK_XBUTTON1) & 0x8000) != 0)
                        mousePressed = true;
                    else if (hotkeyVirtualKey == VK_XBUTTON2 && (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) != 0)
                        mousePressed = true;
                    else if (hotkeyVirtualKey == VK_MBUTTON && (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0)
                        mousePressed = true;

                    // Edge detection - sadece tuşa basıldığında trigger et
                    if (mousePressed && !wasPressed)
                    {
                        Dispatcher.Invoke(() => HandleHotkey());
                        await Task.Delay(200, cancellationToken); // Debounce
                    }

                    wasPressed = mousePressed;
                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal iptal
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MousePollingLoop Error: {ex.Message}");
            }
        }

        private void HandleHotkeySelection(Key key)
        {
            try
            {
                bool isModifier = key == Key.LeftCtrl || key == Key.RightCtrl ||
                                 key == Key.LeftShift || key == Key.RightShift ||
                                 key == Key.LeftAlt || key == Key.RightAlt;

                if (isModifier && !isWaitingForSecondKey)
                {
                    isWaitingForSecondKey = true;
                    pendingModifiers = 0;

                    if (key == Key.LeftCtrl || key == Key.RightCtrl)
                        pendingModifiers |= MOD_CTRL;
                    if (key == Key.LeftShift || key == Key.RightShift)
                        pendingModifiers |= MOD_SHIFT;
                    if (key == Key.LeftAlt || key == Key.RightAlt)
                        pendingModifiers |= MOD_ALT;

                    if (lblHotkeyHelper != null)
                        lblHotkeyHelper.Text = GetText("waitingForMainKey");
                    return;
                }

                if (!isModifier)
                {
                    SetHotkey(key, isWaitingForSecondKey ? pendingModifiers : 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HandleHotkeySelection Error: {ex.Message}");
                isListeningForHotkey = false;
                isWaitingForSecondKey = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            try
            {
                if (msg == WM_HOTKEY)
                {
                    int id = wParam.ToInt32();
                    if (id == hotkeyId && isAppActive && !isUsingMouseHotkey)
                    {
                        HandleHotkey();
                        handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WndProc Error: {ex.Message}");
            }
            return IntPtr.Zero;
        }

        private void HandleHotkey()
        {
            try
            {
                if (useToggleMode)
                {
                    if (isClicking)
                    {
                        StopClicking();
                    }
                    else
                    {
                        StartClicking();
                    }
                }
                else
                {
                    if (!isClicking)
                    {
                        StartClicking();
                        Task.Run(() => WaitForKeyRelease());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HandleHotkey Error: {ex.Message}");
            }
        }

        private void WaitForKeyRelease()
        {
            try
            {
                while (isClicking && !useToggleMode && isAppActive)
                {
                    bool keyReleased = false;

                    if (isUsingMouseHotkey)
                    {
                        // Mouse tuşu için kontrol
                        if (hotkeyVirtualKey == VK_XBUTTON1 && (GetAsyncKeyState(VK_XBUTTON1) & 0x8000) == 0)
                            keyReleased = true;
                        else if (hotkeyVirtualKey == VK_XBUTTON2 && (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) == 0)
                            keyReleased = true;
                        else if (hotkeyVirtualKey == VK_MBUTTON && (GetAsyncKeyState(VK_MBUTTON) & 0x8000) == 0)
                            keyReleased = true;
                    }
                    else
                    {
                        // Klavye tuşu için kontrol
                        if ((hotkeyModifiers & MOD_CTRL) != 0 && (GetAsyncKeyState(0x11) & 0x8000) == 0) keyReleased = true;
                        if ((hotkeyModifiers & MOD_SHIFT) != 0 && (GetAsyncKeyState(0x10) & 0x8000) == 0) keyReleased = true;
                        if ((hotkeyModifiers & MOD_ALT) != 0 && (GetAsyncKeyState(0x12) & 0x8000) == 0) keyReleased = true;

                        if ((GetAsyncKeyState((int)hotkeyVirtualKey) & 0x8000) == 0) keyReleased = true;
                    }

                    if (keyReleased) break;

                    Thread.Sleep(50);
                }

                if (!useToggleMode && isAppActive)
                {
                    Dispatcher.Invoke(() => StopClicking());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WaitForKeyRelease Error: {ex.Message}");
            }
        }

        private void RegisterHotkeys()
        {
            try
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero && !isUsingMouseHotkey)
                {
                    UnregisterHotKey(handle, hotkeyId);

                    bool success = RegisterHotKey(handle, hotkeyId, hotkeyModifiers, hotkeyVirtualKey);

                    if (!success)
                    {
                        MessageBox.Show("Kısayol tuşu kaydedilemedi!", "Hata",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hotkey kaydı hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetHotkey(Key key, uint modifiers = 0)
        {
            try
            {
                isListeningForHotkey = false;
                isWaitingForSecondKey = false;
                isUsingMouseHotkey = false;

                if (modifiers == 0)
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        modifiers |= MOD_CTRL;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        modifiers |= MOD_SHIFT;
                    if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                        modifiers |= MOD_ALT;
                }

                hotkeyModifiers = modifiers;
                hotkeyVirtualKey = GetVirtualKeyFromKey(key);
                currentHotkeyText = GetHotkeyDisplayText(key);

                string prefix = "";
                if ((hotkeyModifiers & MOD_CTRL) != 0) prefix += "Ctrl+";
                if ((hotkeyModifiers & MOD_SHIFT) != 0) prefix += "Shift+";
                if ((hotkeyModifiers & MOD_ALT) != 0) prefix += "Alt+";

                if (btnHotkeySelector != null)
                    btnHotkeySelector.Content = $"🎯 {prefix}{currentHotkeyText}";
                if (lblHotkeyHelper != null)
                    lblHotkeyHelper.Text = GetText("hotkeyHelper");

                StopMousePolling(); // Mouse polling'i durdur
                RegisterHotkeys();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetHotkey Error: {ex.Message}");
            }
        }

        private uint GetVirtualKeyFromKey(Key key)
        {
            try
            {
                return (uint)KeyInterop.VirtualKeyFromKey(key);
            }
            catch
            {
                return 0x70;
            }
        }

        private string GetHotkeyDisplayText(Key key)
        {
            switch (key)
            {
                case Key.F1: return "F1";
                case Key.F2: return "F2";
                case Key.F3: return "F3";
                case Key.F4: return "F4";
                case Key.F5: return "F5";
                case Key.F6: return "F6";
                case Key.F7: return "F7";
                case Key.F8: return "F8";
                case Key.F9: return "F9";
                case Key.F10: return "F10";
                case Key.F11: return "F11";
                case Key.F12: return "F12";
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Tab: return "Tab";
                case Key.Escape: return "Esc";
                case Key.A: return "A";
                case Key.B: return "B";
                case Key.C: return "C";
                case Key.D: return "D";
                case Key.E: return "E";
                case Key.F: return "F";
                case Key.G: return "G";
                case Key.H: return "H";
                case Key.I: return "I";
                case Key.J: return "J";
                case Key.K: return "K";
                case Key.L: return "L";
                case Key.M: return "M";
                case Key.N: return "N";
                case Key.O: return "O";
                case Key.P: return "P";
                case Key.Q: return "Q";
                case Key.R: return "R";
                case Key.S: return "S";
                case Key.T: return "T";
                case Key.U: return "U";
                case Key.V: return "V";
                case Key.W: return "W";
                case Key.X: return "X";
                case Key.Y: return "Y";
                case Key.Z: return "Z";
                default: return key.ToString();
            }
        }

        // Event Handlers
        private void btnMainAction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isAppActive)
                {
                    isAppActive = false;
                    StopClicking();
                    StopMousePolling();
                }
                else
                {
                    isAppActive = true;
                    if (isUsingMouseHotkey)
                    {
                        StartMousePolling();
                    }
                }

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ana buton hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHotkeySelector_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isListeningForHotkey = true;
                isWaitingForSecondKey = false;
                pendingModifiers = 0;
                if (lblHotkeyHelper != null)
                    lblHotkeyHelper.Text = GetText("waitingForKey");
                this.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"btnHotkeySelector_Click Error: {ex.Message}");
            }
        }

        private void btnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"btnThemeToggle_Click Error: {ex.Message}");
            }
        }

        private void btnLanguageToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleLanguage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"btnLanguageToggle_Click Error: {ex.Message}");
            }
        }

        private void btnSlowSpeed_Click(object sender, RoutedEventArgs e)
        {
            SetPresetSpeed(25);
        }

        private void btnMediumSpeed_Click(object sender, RoutedEventArgs e)
        {
            SetPresetSpeed(500);
        }

        private void btnFastSpeed_Click(object sender, RoutedEventArgs e)
        {
            SetPresetSpeed(2500);
        }

        private void btnTarget100_Click(object sender, RoutedEventArgs e)
        {
            SetPresetTarget(100);
        }

        private void btnTarget1000_Click(object sender, RoutedEventArgs e)
        {
            SetPresetTarget(1000);
        }

        private void btnTargetUnlimited_Click(object sender, RoutedEventArgs e)
        {
            SetPresetTarget(0);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResetSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reset hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtClickSpeed_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                UpdateSpeedEmoji();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"txtClickSpeed_TextChanged Error: {ex.Message}");
            }
        }

        private void txtClickTarget_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                UpdateTargetDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"txtClickTarget_TextChanged Error: {ex.Message}");
            }
        }

        // Helper Methods
        private void SetPresetSpeed(int speed)
        {
            try
            {
                if (txtClickSpeed != null)
                {
                    txtClickSpeed.Text = speed.ToString();
                    clickSpeed = speed;
                    UpdateSpeedEmoji();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetPresetSpeed Error: {ex.Message}");
            }
        }

        private void SetPresetTarget(int target)
        {
            try
            {
                if (txtClickTarget != null)
                {
                    txtClickTarget.Text = target.ToString();
                    clickTarget = target;
                    UpdateTargetDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetPresetTarget Error: {ex.Message}");
            }
        }

        private void ToggleTheme()
        {
            try
            {
                isDarkTheme = !isDarkTheme;

                if (isDarkTheme)
                {
                    Resources["BackgroundBrush"] = Resources["DarkBackground"];
                    Resources["SurfaceBrush"] = Resources["DarkSurface"];
                    Resources["CardBrush"] = Resources["DarkCard"];
                    Resources["AccentBrush"] = Resources["DarkAccent"];
                    Resources["AccentHoverBrush"] = Resources["DarkAccentHover"];
                    Resources["DangerBrush"] = Resources["DarkDanger"];
                    Resources["DangerHoverBrush"] = Resources["DarkDangerHover"];
                    Resources["TextBrush"] = Resources["DarkText"];
                    Resources["SubTextBrush"] = Resources["DarkSubText"];
                    Resources["BorderBrush"] = Resources["DarkBorder"];
                    if (btnThemeToggle != null) btnThemeToggle.Content = "🌙";
                }
                else
                {
                    Resources["BackgroundBrush"] = Resources["LightBackground"];
                    Resources["SurfaceBrush"] = Resources["LightSurface"];
                    Resources["CardBrush"] = Resources["LightCard"];
                    Resources["AccentBrush"] = Resources["LightAccent"];
                    Resources["AccentHoverBrush"] = Resources["LightAccentHover"];
                    Resources["DangerBrush"] = Resources["LightDanger"];
                    Resources["DangerHoverBrush"] = Resources["LightDangerHover"];
                    Resources["TextBrush"] = Resources["LightText"];
                    Resources["SubTextBrush"] = Resources["LightSubText"];
                    Resources["BorderBrush"] = Resources["LightBorder"];
                    if (btnThemeToggle != null) btnThemeToggle.Content = "☀️";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleTheme Error: {ex.Message}");
            }
        }

        private void ToggleLanguage()
        {
            try
            {
                currentLanguage = currentLanguage == "tr" ? "en" : "tr";
                if (btnLanguageToggle != null)
                    btnLanguageToggle.Content = currentLanguage == "tr" ? "TR" : "EN";
                UpdateLanguage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleLanguage Error: {ex.Message}");
            }
        }

        private void UpdateLanguage()
        {
            try
            {
                // Başlık
                this.Title = GetText("title");

                // Ana kontroller
                if (btnMainAction != null)
                {
                    if (isAppActive)
                    {
                        btnMainAction.Content = GetText("stop");
                    }
                    else
                    {
                        btnMainAction.Content = GetText("start");
                    }
                }

                // Tüm başlıkları güncelle
                if (lblTitle != null) lblTitle.Text = GetText("title");
                if (lblStatsTitle != null) lblStatsTitle.Text = GetText("statsTitle");
                if (lblStatusTitle != null) lblStatusTitle.Text = GetText("statusTitle");
                if (lblSpeedTitle != null) lblSpeedTitle.Text = GetText("speedTitle");
                if (lblClickTypeTitle != null) lblClickTypeTitle.Text = GetText("clickTypeTitle");
                if (lblTargetTitle != null) lblTargetTitle.Text = GetText("targetTitle");
                if (lblHotkeyTitle != null) lblHotkeyTitle.Text = GetText("hotkeyTitle");
                if (lblWorkModeTitle != null) lblWorkModeTitle.Text = GetText("workModeTitle");

                // Alt metinleri güncelle
                if (lblTotalText != null) lblTotalText.Text = GetText("totalText");
                if (lblTargetText != null) lblTargetText.Text = GetText("targetText");
                if (lblTargetHelper != null) lblTargetHelper.Text = GetText("targetHelper");
                if (lblHotkeyHelper != null) lblHotkeyHelper.Text = GetText("hotkeyHelper");
                if (lblDeveloper != null) lblDeveloper.Text = GetText("developer");

                // Radio butonları güncelle
                if (rbLeftClick != null) rbLeftClick.Content = GetText("leftClick");
                if (rbRightClick != null) rbRightClick.Content = GetText("rightClick");
                if (rbToggleMode != null) rbToggleMode.Content = GetText("toggleMode");
                if (rbHoldMode != null) rbHoldMode.Content = GetText("holdMode");

                // Reset butonunu güncelle
                if (btnReset != null) btnReset.Content = GetText("reset");

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLanguage Error: {ex.Message}");

                if (btnMainAction != null)
                {
                    if (isAppActive)
                    {
                        btnMainAction.Content = currentLanguage == "tr" ? "DURDUR" : "STOP";
                    }
                    else
                    {
                        btnMainAction.Content = currentLanguage == "tr" ? "BAŞLAT" : "START";
                    }
                }
            }
        }

        private string GetText(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(currentLanguage))
                {
                    currentLanguage = "tr";
                }

                if (languages == null)
                {
                    InitializeLanguages();
                }

                if (languages.ContainsKey(currentLanguage) &&
                    languages[currentLanguage] != null &&
                    languages[currentLanguage].ContainsKey(key))
                {
                    return languages[currentLanguage][key];
                }

                if (languages.ContainsKey("tr") &&
                    languages["tr"] != null &&
                    languages["tr"].ContainsKey(key))
                {
                    return languages["tr"][key];
                }

                return key;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetText Error: {ex.Message}");
                return key;
            }
        }

        private void UpdateSpeedEmoji()
        {
            try
            {
                if (txtClickSpeed != null && lblSpeedEmoji != null)
                {
                    if (int.TryParse(txtClickSpeed.Text, out int speed))
                    {
                        string emoji = GetSpeedEmoji(speed);
                        lblSpeedEmoji.Text = emoji;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSpeedEmoji Error: {ex.Message}");
            }
        }

        private string GetSpeedEmoji(int speed)
        {
            if (speed <= 10) return "🐢";
            else if (speed <= 25) return "🐌";
            else if (speed <= 50) return "🐱";
            else if (speed <= 100) return "🏃";
            else if (speed <= 250) return "🏃‍♂️";
            else if (speed <= 500) return "🚗";
            else if (speed <= 1000) return "🚀";
            else return "⚡";
        }

        private void UpdateTargetDisplay()
        {
            try
            {
                if (txtClickTarget != null && lblTargetDisplay != null)
                {
                    if (int.TryParse(txtClickTarget.Text, out int target))
                    {
                        clickTarget = target;
                        lblTargetDisplay.Text = target == 0 ? GetText("unlimited") : target.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTargetDisplay Error: {ex.Message}");
            }
        }

        private void ResetSettings()
        {
            try
            {
                StopClicking();
                StopMousePolling();
                isAppActive = false;

                if (txtClickSpeed != null) txtClickSpeed.Text = "1";
                if (txtClickTarget != null) txtClickTarget.Text = "0";

                clickSpeed = 1;
                clickTarget = 0;
                totalClicks = 0;

                if (rbLeftClick != null) rbLeftClick.IsChecked = true;
                if (rbToggleMode != null) rbToggleMode.IsChecked = true;

                useLeftClick = true;
                useToggleMode = true;
                currentHotkeyText = "F1";
                hotkeyVirtualKey = 0x70;
                hotkeyModifiers = 0;
                isUsingMouseHotkey = false;

                if (btnHotkeySelector != null) btnHotkeySelector.Content = "🎯 F1";

                UpdateSpeedEmoji();
                UpdateTargetDisplay();
                UpdateDisplay();
                RegisterHotkeys();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetSettings Error: {ex.Message}");
            }
        }

        private void StartClicking()
        {
            try
            {
                if (isClicking || !isAppActive) return;

                UpdateSettings();

                isClicking = true;
                cancellationTokenSource = new CancellationTokenSource();

                Task.Run(() => ClickLoop(cancellationTokenSource.Token));

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlatma hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                isClicking = false;
            }
        }

        private void StopClicking()
        {
            try
            {
                if (!isClicking) return;

                isClicking = false;
                cancellationTokenSource?.Cancel();

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopClicking Error: {ex.Message}");
            }
        }

        private void UpdateSettings()
        {
            try
            {
                if (txtClickSpeed != null)
                {
                    if (int.TryParse(txtClickSpeed.Text, out int speed))
                    {
                        clickSpeed = Math.Max(1, Math.Min(10000, speed));
                        txtClickSpeed.Text = clickSpeed.ToString();
                    }
                    else
                    {
                        clickSpeed = 1;
                        txtClickSpeed.Text = "1";
                    }
                }

                if (txtClickTarget != null)
                {
                    if (int.TryParse(txtClickTarget.Text, out int target))
                    {
                        clickTarget = Math.Max(0, target);
                        txtClickTarget.Text = clickTarget.ToString();
                    }
                    else
                    {
                        clickTarget = 0;
                        txtClickTarget.Text = "0";
                    }
                }

                useLeftClick = rbLeftClick?.IsChecked == true;
                useToggleMode = rbToggleMode?.IsChecked == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSettings Error: {ex.Message}");
            }
        }

        private async void ClickLoop(CancellationToken cancellationToken)
        {
            try
            {
                int delayMs = Math.Max(1, 1000 / clickSpeed);

                while (!cancellationToken.IsCancellationRequested && isClicking && isAppActive)
                {
                    PerformClick();
                    totalClicks++;

                    Dispatcher.Invoke(() => {
                        try
                        {
                            if (lblTotalClicks != null)
                                lblTotalClicks.Text = totalClicks.ToString();

                            if (clickTarget > 0)
                            {
                                if (lblProgress != null)
                                    lblProgress.Text = $"{totalClicks}/{clickTarget}";

                                if (totalClicks >= clickTarget)
                                {
                                    StopClicking();
                                    if (lblStatus != null)
                                        lblStatus.Text = $"✅ {GetText("completed")}";
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ClickLoop Dispatcher Error: {ex.Message}");
                        }
                    });

                    await Task.Delay(delayMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal iptal
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => {
                    try
                    {
                        MessageBox.Show($"Click işlemi hatası: {ex.Message}", "Hata",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        StopClicking();
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"ClickLoop Error Handler Error: {innerEx.Message}");
                    }
                });
            }
        }

        private void PerformClick()
        {
            try
            {
                if (useLeftClick)
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
                else
                {
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PerformClick Error: {ex.Message}");
            }
        }

        private void UpdateDisplay()
        {
            try
            {
                Dispatcher.Invoke(() => {
                    try
                    {
                        if (isClicking)
                        {
                            // Şu anda tıklıyor - YEŞİL DURUM
                            if (lblStatus != null)
                            {
                                lblStatus.Text = $"🟢 {GetText("running")} ({clickSpeed} CPS)";
                                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)); // Yeşil
                            }
                            if (btnMainAction != null)
                            {
                                btnMainAction.Content = GetText("stop");
                                btnMainAction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 58, 73)); // Kırmızı
                            }
                        }
                        else if (isAppActive)
                        {
                            // Aktif ama tıklamıyor - YEŞİL DURUM
                            if (lblStatus != null)
                            {
                                if (clickTarget > 0 && totalClicks >= clickTarget)
                                {
                                    lblStatus.Text = $"✅ {GetText("completed")}";
                                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)); // Yeşil
                                }
                                else
                                {
                                    lblStatus.Text = $"🟢 {GetText("active")}";
                                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)); // Yeşil
                                }
                            }
                            if (btnMainAction != null)
                            {
                                btnMainAction.Content = GetText("stop");
                                btnMainAction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 58, 73)); // Kırmızı
                            }
                        }
                        else
                        {
                            // Pasif durumda - KIRMIZI DURUM
                            if (lblStatus != null)
                            {
                                lblStatus.Text = $"🔴 {GetText("inactive")}";
                                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 58, 73)); // Kırmızı
                            }
                            if (btnMainAction != null)
                            {
                                btnMainAction.Content = GetText("start");
                                btnMainAction.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)); // Yeşil
                            }
                        }

                        if (lblTotalClicks != null)
                            lblTotalClicks.Text = totalClicks.ToString();
                        if (lblTargetDisplay != null)
                            lblTargetDisplay.Text = clickTarget == 0 ? GetText("unlimited") : clickTarget.ToString();

                        if (lblProgress != null)
                        {
                            if (clickTarget > 0)
                            {
                                lblProgress.Text = $"{totalClicks}/{clickTarget}";
                            }
                            else
                            {
                                lblProgress.Text = "";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateDisplay Inner Error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDisplay Error: {ex.Message}");
            }
        }
    }
}