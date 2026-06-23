namespace NestSuite;

internal static class NestSuiteWindowPositionGuard
{
    internal static bool IsOnScreen(double left, double top, double width, double height)
        => IsOnScreen(left, top, width, height,
            System.Windows.SystemParameters.VirtualScreenLeft,
            System.Windows.SystemParameters.VirtualScreenTop,
            System.Windows.SystemParameters.VirtualScreenWidth,
            System.Windows.SystemParameters.VirtualScreenHeight);

    internal static bool IsOnScreen(
        double left, double top, double width, double height,
        double screenLeft, double screenTop, double screenWidth, double screenHeight)
    {
        if (double.IsNaN(left) || double.IsNaN(top)) return false;
        const double minVisible = 100;
        return left + minVisible <= screenLeft + screenWidth  &&
               top  + minVisible <= screenTop  + screenHeight &&
               left + width      >= screenLeft + minVisible   &&
               top  + height     >= screenTop  + minVisible;
    }
}
