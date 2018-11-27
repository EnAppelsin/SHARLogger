using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SHARConsoleTee
{
    class Program
    {
        static StreamWriter writer;
        static Process simpsons;

        static void Main(string[] args)
        {
            string dt = DateTime.Now.ToString("yyyy_MM_dd_hh_mm");
            Console.WriteLine("Recording console to Simpsons_{0}.txt", dt);
            writer = new StreamWriter(string.Format("Simpsons_{0}.txt", dt), false, Encoding.UTF8);
            writer.WriteLine("Begin Logging at {0}", dt);

            RegistryKey settings = Registry.CurrentUser.OpenSubKey(@"Software\Lucas Stuff\Lucas' Simpsons Hit & Run Tools\Lucas' Simpsons Hit & Run Mod Launcher\Mod Settings\Randomiser");
            Console.WriteLine("Changed Randomiser settings are as follows:");
            writer.WriteLine("Changed Randomiser settings are as follows:");
            foreach (string name in settings.GetValueNames())
            {
                var value = settings.GetValue(name);
                Console.WriteLine("{0} = {1}", name, value);
                writer.WriteLine("{0} = {1}", name, value);
            }


            Console.WriteLine("Waiting for Simpsons.exe");
            simpsons = null;
            while (true)
            {
                Process[] procceses = Process.GetProcessesByName("Simpsons");
                if (procceses.Length > 0)
                {
                    simpsons = procceses[0];
                }
                if (simpsons != null)
                    break;
                Thread.Sleep(100);
            }

            simpsons.EnableRaisingEvents = true;
            simpsons.Exited += Simpsons_Exited;

            Console.WriteLine("Simpsons.exe found with PID {0}", simpsons.Id);
            writer.WriteLine("Simpsons.exe has started!!");

            SHARConsoleHook.ServerInterface sif = new SHARConsoleHook.ServerInterface();
            sif.Message += Sif_Message;

            string channelName = null;
            EasyHook.RemoteHooking.IpcCreateServer<SHARConsoleHook.ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, sif);
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "SHARConsoleHook.dll");

            Console.WriteLine("Beginning injection procedure!");
            EasyHook.RemoteHooking.Inject(
                simpsons.Id,
                injectionLibrary,
                injectionLibrary,
                channelName);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Injection complete! <Press any key to exit>");
            writer.WriteLine("!!! Injection has completed, console log follows!");

            Console.ResetColor();
            Console.ReadKey();
            writer.Close();
        }

        private static void Simpsons_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Closing because Simpsons.exe has closed, exit code: {0}", simpsons.ExitCode);
            writer.Write("Closing because Simpsons.exe has closed, exit code: {0}", simpsons.ExitCode);
            writer.Flush();
            writer.Close();
            Environment.Exit(0);
        }

        private static void Sif_Message(string message)
        {
            Console.Write(message);
            writer.Write(message);
            writer.Flush();
        }
    }
}
