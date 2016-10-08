﻿using SWPatcherTest.Helpers;
using SWPatcherTest.Helpers.GlobalVariables;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SWPatcherTest
{
    static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (!Directory.Exists(UserSettings.PatcherPath))
                UserSettings.PatcherPath = "";
            Directory.SetCurrentDirectory(UserSettings.PatcherPath);
            Methods.LogMethodFullName(System.Reflection.MethodBase.GetCurrentMethod());
            Logger.Debug($"Main thread ID=[{Thread.CurrentThread.ManagedThreadId}]");
            Logger.Run();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            var controller = new SingleInstanceController();
            controller.Run(Environment.GetCommandLineArgs());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Critical(e.ExceptionObject as Exception);
            MsgBox.Error("Critical unhandled exception occured. Application will now exit.\n" + Logger.ExeptionParser(e.ExceptionObject as Exception));
            
            Application.Exit();
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.Critical(e.Exception);
            MsgBox.Error("Critical thread exception occured. Application will now exit.\n" + Logger.ExeptionParser(e.Exception));
            
            Application.Exit();
        }

        private class SingleInstanceController : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
        {
            public SingleInstanceController()
            {
                this.IsSingleInstance = true;
                this.StartupNextInstance += new Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventHandler(SingleInstanceController_StartupNextInstance);
            }

            private void SingleInstanceController_StartupNextInstance(object sender, Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
            {
                var mainForm = this.MainForm as Forms.MainForm;
                mainForm.RestoreFromTray();
            }

            protected override void OnCreateMainForm()
            {
                this.MainForm = new Forms.MainForm();
            }
        }
    }
}
