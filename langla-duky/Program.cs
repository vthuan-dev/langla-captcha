using System.Windows.Forms;
using System.Text;
using System;

namespace langla_duky
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch (IOException)
            {
                // Ignore if there's no console handle
            }

            // Check if we should run a simple test
            if (args.Length > 0 && args[0] == "SimpleTesseractTest")
            {
                SimpleTesseractTest.RunTest();
                return;
            }
            
            // Check if we should run a direct test
            if (args.Length > 0 && args[0] == "--direct-test")
            {
                TestOCRDirect.RunDirectTest();
                return;
            }
            
            // Check if we should run a quick test
            if (args.Length > 0 && args[0] == "--quick-test")
            {
                QuickOCRTest.RunQuickTest();
                return;
            }
            
            // Check if we should run a simple debug
            if (args.Length > 0 && args[0] == "--simple-debug")
            {
                SimpleOCRDebug.RunSimpleDebug();
                return;
            }
            
            // Check if we should test OCR API
            if (args.Length > 0 && args[0] == "--test-ocr-api")
            {
                TestOCRSpaceAPI.TestOCRAPI().Wait();
                return;
            }
            
            // Check if we should test OCR simple
            if (args.Length > 0 && args[0] == "--test-ocr-simple")
            {
                TestOCRSimple.TestOCRAPI().Wait();
                return;
            }
            
            // Check if we should test OCR sync
            if (args.Length > 0 && args[0] == "--test-ocr-sync")
            {
                TestOCRSync.TestOCRAPI();
                return;
            }
            
            // Check if we should test basic
            if (args.Length > 0 && args[0] == "--test-basic")
            {
                TestBasic.RunTest();
                return;
            }
            
            // Check if we should test OCR direct
            if (args.Length > 0 && args[0] == "--test-ocr-direct")
            {
                TestOCRDirect.RunDirectTest();
                return;
            }
            
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
    