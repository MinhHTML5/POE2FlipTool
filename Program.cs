using POE2FlipTool.Utilities;

namespace POE2FlipTool
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();



            var windowUtil = new WindowsUtil();
            var inputHook = new InputHook();
            var colorUtil = new ColorUtil();
            var ocrUtil = new OCRUtil();

            var mainForm = new Main(windowUtil, inputHook, colorUtil, ocrUtil);

            Application.Run(mainForm);
        }
    }
}