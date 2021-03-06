﻿
using System;
using System.ComponentModel;
using System.Text;
using DeconTools.Backend.Core;
using DeconTools.Backend.Parameters;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.Utilities;

namespace DeconTools.Backend.Workflows
{
    public class TraditionalScanBasedWorkflow : ScanBasedWorkflow
    {

        private readonly O16O18PeakDataAppender _o16O18PeakDataAppender = new O16O18PeakDataAppender();
        private int _scanCounter = 1;
        private const int NumScansBetweenProgress = 10;

        #region Constructors

        public TraditionalScanBasedWorkflow(DeconToolsParameters parameters, Run run, string outputFolderPath = null, BackgroundWorker backgroundWorker = null)
            : base(parameters, run, outputFolderPath, backgroundWorker)
        {
        }
        #endregion


        #region Public Methods

        protected override void IterateOverScans()
        {
            var startTime = DateTime.UtcNow;
            var maxRuntimeHours = NewDeconToolsParameters.MiscMSProcessingParameters.MaxHoursPerDataset;
            if (maxRuntimeHours <= 0)
                maxRuntimeHours = int.MaxValue;

            _scanCounter = 1;
            foreach (var scanset in Run.ScanSetCollection.ScanSetList)
            {
                Run.CurrentScanSet = scanset;

                ExecuteProcessingTasks();

                if (BackgroundWorker != null)
                {
                    if (BackgroundWorker.CancellationPending)
                    {
                        return;
                    }

                }
                ReportProgress();

                _scanCounter++;

                if (DateTime.UtcNow.Subtract(startTime).TotalHours >= maxRuntimeHours)
                {
                    Console.WriteLine(
                        "Aborted processing because {0} hours have elapsed; ScanCount processed = {1}",
                        (int)DateTime.UtcNow.Subtract(startTime).TotalHours,
                        _scanCounter);

                    break;
                }
            }
        }


        protected string getErrorInfo(Run run, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append("ERROR THROWN. Scan/Frame = ");
            sb.Append(run.GetCurrentScanOrFrameInfo());
            sb.Append("; ");
            sb.Append(DiagnosticUtilities.GetCurrentProcessInfo());
            sb.Append("; errorObject details: ");
            sb.Append(ex.Message);
            sb.Append("; ");
            sb.Append(PRISM.clsStackTraceFormatter.GetExceptionStackTraceMultiLine(ex));

            return sb.ToString();

        }

        protected override void ExecuteOtherTasksHook()
        {
            base.ExecuteOtherTasksHook();
            if (NewDeconToolsParameters.ThrashParameters.IsO16O18Data)
            {
                ExecuteTask(_o16O18PeakDataAppender);
            }


        }




        public override void ReportProgress()
        {
            if (Run.ScanSetCollection == null || Run.ScanSetCollection.ScanSetList.Count == 0) return;

            var userstate = new ScanBasedProgressInfo(Run, Run.CurrentScanSet, null);

            var percentDone = (float)(_scanCounter) / (float)(Run.ScanSetCollection.ScanSetList.Count) * 100;
            userstate.PercentDone = percentDone;

            var logText = "Scan/Frame= " + Run.GetCurrentScanOrFrame() + "; PercentComplete= " + percentDone.ToString("0.0") + "; AccumlatedFeatures= " + Run.ResultCollection.getTotalIsotopicProfiles();

            if (BackgroundWorker != null)
            {
                BackgroundWorker.ReportProgress((int)percentDone, userstate);
            }

            if (_scanCounter % NumScansBetweenProgress == 0 || mShowTraceMessages)
            {
                Logger.Instance.AddEntry(logText, Logger.Instance.OutputFilename);

                if (BackgroundWorker == null)
                {
                    Console.WriteLine(DateTime.Now + "\t" + logText);
                }

            }
        }


        #endregion






    }
}
