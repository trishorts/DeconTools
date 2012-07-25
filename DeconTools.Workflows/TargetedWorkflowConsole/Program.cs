﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeconTools.Workflows.Backend.Core;

namespace TargetedWorkflowConsole
{
    class Program
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;


        static void Main(string[] args)
        {
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);     // sets it so that keyboard use does not interrupt things.


            if (args == null || args.Length == 0)
            {
                ReportSyntax();
            }

            if (args.Length == 2 || args.Length == 3)
            {

                string datasetPath = args[0];
                
                FileInfo parametersFileInfo = new FileInfo(args[1]);
                if (!parametersFileInfo.Exists)
                {
                    reportFileProblem(parametersFileInfo.FullName);
                    return;
                }
                else
                {
                    var executorParameters=   WorkflowParameters.CreateParameters(args[1]) as WorkflowExecutorBaseParameters;
					
					// 3 arguments, overriding targets path
					if (args.Length == 3) executorParameters.TargetsFilePath = args[2];

                    TargetedWorkflowExecutor executor = TargetedWorkflowExecutorFactory.CreateTargetedWorkflowExecutor(executorParameters, datasetPath);
                    
                    executor.Execute();

                }
            }
            else
            {
                ReportSyntax();
            }

        }

        private static void reportFileProblem(string p)
        {
            Console.WriteLine("ERROR. Inputted parameter filename does not exist. Inputted file name = " + p);
        }

        private static void ReportSyntax()
        {
            Console.WriteLine();
            Console.WriteLine("This Commandline app requires two arguments.");
            Console.WriteLine("\tArg1 = dataset path");
            Console.WriteLine("\tArg2 = workflow executor parameter file (.xml)");
         
            Console.WriteLine();
        }
    }
}
