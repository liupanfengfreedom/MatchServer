using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Server
{
    class Utility
    {
        static string path = @"C:\Program Files\Epic Games\UE_4.22\Engine\Build\BatchFiles\RunUAT.bat";
        static string Arguments = "BuildCookRun -project=D:\\ueprojecttest/MyProject/MyProject.uproject  -noP4 -platform=Android -clientconfig=Development -serverconfig=Development -cook -allmaps -stage -pak -archive";

        public static void CommandRun(string exe, string arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = true,//true mean can launch .bat file
                };
                Process p = Process.Start(info);
                //p.WaitForExit();
            }
            catch (Exception e)
            {
                // Log.Error(e);
            }
        }
    }
}
