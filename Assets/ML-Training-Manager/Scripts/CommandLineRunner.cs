using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class CommandLineRunner {
    static List<Process> processes = new List<Process>();
    public static string WorkingDirectory;

    public static Process StartCommandLine(string path, string program)
    {
        var args = new List<string>();
        return StartCommandLine(path, program, args.ToArray());
    }

    public static Process StartCommandLine(string path, string program, string[] arguments)
    {
        var args = string.Join(" ", arguments);
        string test = program + " " + args;//Path.Combine(path, program) + args;
        var anacondaActivate = "/C C:/ProgramData/Anaconda3/Scripts/activate.bat && activate py36 && cd " + path + " && " + test;
        UnityEngine.Debug.Log(anacondaActivate);
        var processInfo = new ProcessStartInfo("CMD.exe", anacondaActivate);
        processInfo.WorkingDirectory = WorkingDirectory;
        var process = Process.Start(processInfo);

        processes.Add(process);
        return process;
    }

    public static void CloseAllProcesses()
    {
        foreach (var process in processes)
        {
            //process.WaitForExit();
            process.Close();
        }       
    }
}
