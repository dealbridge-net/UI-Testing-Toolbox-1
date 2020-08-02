using System.Drawing;

namespace Lombiq.Tests.UI.Constants
{
    /// <summary>
    /// Some common display resolutions to be used when setting browser window sizes with <see
    /// cref="Lombiq.Tests.UI.Extensions.VisibilityUITestContextExtensions.SetBrowserSize(Services.UITestContext,
    /// Size)"/>. Generally it's better to test the given app's responsive breakpoints specifically though instead of
    /// using such standard resolutions.
    /// </summary>
    /// <remarks>
    /// Taken mostly from <see href="https://en.wikipedia.org/wiki/Display_resolution#Common_display_resolutions"/>,
    /// and also from <see href="https://en.wikipedia.org/wiki/List_of_common_resolutions"/>.
    /// </remarks>
    public static class CommonDisplayResolutions
    {
        public static readonly Size Qvga = new Size(320, 240);
        public static readonly Size Hvga = new Size(480, 320);
        public static readonly Size Nhd = new Size(640, 360);
        public static readonly Size Vga = new Size(640, 480);
        public static readonly Size Svga = new Size(800, 600);
        public static readonly Size Qhd = new Size(960, 540);
        public static readonly Size Xga = new Size(1024, 768);
        public static readonly Size Hd = new Size(1280, 720);
        public static readonly Size Sxga = new Size(1280, 1024);
        public static readonly Size WxgaPlus = new Size(1440, 900);
        public static readonly Size HdPlus = new Size(1600, 900);
        public static readonly Size WsxgaPlus = new Size(1680, 1050);
        public static readonly Size Fhd = new Size(1920, 1080);
        public static readonly Size Wuxga = new Size(1920, 1200);
        public static readonly Size Dci2K = new Size(2048, 1080);
        public static readonly Size Qwxga = new Size(2048, 1152);
        public static readonly Size Wqhd = new Size(2560, 1440);
        public static readonly Size Uwqhd = new Size(3440, 1440);
        public static readonly Size FourKUhd = new Size(3840, 3160);
        public static readonly Size FiveK = new Size(5120, 2880);
        public static readonly Size EightKUhd = new Size(7680, 4320);
    }
}