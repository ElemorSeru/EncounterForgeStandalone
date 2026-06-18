using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace EncounterForgeStandalone;

static class WindowTheme
{
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int DWMWA_CAPTION_COLOR = 35;
    const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);

    const int CaptionBg = 0x002C2525;
    const int CaptionText = 0x00DEE6E8;

    internal static void ApplyDarkTheme(this Window window)
    {
        window.SourceInitialized += (_, _) => Apply(new WindowInteropHelper(window).Handle);
    }

    static void Apply(nint hwnd)
    {
        int dark = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);

        int bg = CaptionBg;
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref bg, 4);
        int fg = CaptionText;
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref fg, 4);
    }
}
