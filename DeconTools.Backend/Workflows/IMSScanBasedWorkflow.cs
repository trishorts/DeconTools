﻿
using System;
using System.ComponentModel;
using DeconTools.Backend.Core;
using DeconTools.Backend.Parameters;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.Runs;
using DeconTools.Backend.Utilities;

namespace DeconTools.Backend.Workflows
{
    public abstract class IMSScanBasedWorkflow : ScanBasedWorkflow
    {

        protected UIMFDriftTimeExtractor UimfDriftTimeExtractor;
        protected UIMF_TICExtractor UimfTicExtractor;
        protected SaturationDetector SaturationDetector;

        #region Constructors

        internal IMSScanBasedWorkflow(DeconToolsParameters parameters, Run run, string outputFolderPath = null, BackgroundWorker backgroundWorker = null)
            : base(parameters, run, outputFolderPath, backgroundWorker)
        {
            DeconTools.Utilities.Check.Require(run is UIMFRun, "Cannot create workflow. Run is required to be a UIMFRun for this type of workflow");

        }



        #endregion

        protected override void InitializeProcessingTasks()
        {
            base.InitializeProcessingTasks();
            UimfDriftTimeExtractor = new UIMFDriftTimeExtractor();
            UimfTicExtractor = new UIMF_TICExtractor();
            SaturationDetector = new SaturationDetector();
        }

        protected override void CreateTargetMassSpectra()
        {
            var uimfRun = (UIMFRun)Run;
            uimfRun.ScanSetCollection = new ScanSetCollection();
            uimfRun.IMSScanSetCollection = new IMSScanSetCollection();


            int numFramesSummed;
            if (NewDeconToolsParameters.MSGeneratorParameters.SumSpectraAcrossLC)
            {
                numFramesSummed = NewDeconToolsParameters.MSGeneratorParameters.NumLCScansToSum;
            }
            else
            {
                numFramesSummed = 1;
            }

            if (NewDeconToolsParameters.MSGeneratorParameters.UseLCScanRange)   //Defines whether or not to use all LC time points, or a restricted range
            {
                uimfRun.ScanSetCollection.Create(uimfRun, NewDeconToolsParameters.MSGeneratorParameters.MinLCScan,
                   NewDeconToolsParameters.MSGeneratorParameters.MaxLCScan, numFramesSummed, 1, NewDeconToolsParameters.ScanBasedWorkflowParameters.ProcessMS2);

            }
            else
            {
                uimfRun.ScanSetCollection.Create(uimfRun, numFramesSummed, 1);
            }



            var sumAllIMSScansInAFrame = (NewDeconToolsParameters.MSGeneratorParameters.SumAllSpectra);
            if (sumAllIMSScansInAFrame)
            {

                var centerScan = (uimfRun.MinIMSScan + uimfRun.MaxIMSScan + 1) / 2;

                uimfRun.IMSScanSetCollection.ScanSetList.Clear();
                var scanset = new IMSScanSet(centerScan, uimfRun.MinIMSScan, uimfRun.MaxIMSScan);
                uimfRun.IMSScanSetCollection.ScanSetList.Add(scanset);
            }
            else
            {

                var sumAcrossIMSScans = NewDeconToolsParameters.MSGeneratorParameters.SumSpectraAcrossIms;

                int numIMSScanToSum;
                if (sumAcrossIMSScans)
                {
                    numIMSScanToSum = NewDeconToolsParameters.MSGeneratorParameters.NumImsScansToSum;  
                }
                else
                {
                    numIMSScanToSum = 1;
                }

                uimfRun.IMSScanSetCollection.Create(Run,uimfRun.MinIMSScan,uimfRun.MaxIMSScan,numIMSScanToSum, 1);

            }



        }

        protected override void ExecutePreprocessHook()
        {
            base.ExecutePreprocessHook();
            ((UIMFRun)Run).GetFrameDataAllFrameSets();
            ((UIMFRun)Run).SmoothFramePressuresInFrameSets();
        }

        protected override void IterateOverScans()
        {
            var uimfRun = (UIMFRun)Run;

            foreach (var frameset in uimfRun.ScanSetCollection.ScanSetList)
            {
                foreach (IMSScanSet imsScanSet in uimfRun.IMSScanSetCollection.ScanSetList)
                {
                    uimfRun.CurrentScanSet = frameset;
                    uimfRun.CurrentIMSScanSet = imsScanSet;
                    ReportProgress();
                    ExecuteProcessingTasks();
                }
            }
        }

        protected override void ExecuteOtherTasksHook()
        {
            base.ExecuteOtherTasksHook();
            ExecuteTask(UimfDriftTimeExtractor);
            ExecuteTask(UimfTicExtractor);
            ExecuteTask(SaturationDetector);
        }

        public override void ReportProgress()
        {
            var uimfRun = (UIMFRun)Run;
            if (uimfRun.ScanSetCollection == null || uimfRun.ScanSetCollection.ScanSetList.Count == 0) return;

            var userstate = new ScanBasedProgressInfo(Run, uimfRun.CurrentScanSet, uimfRun.CurrentIMSScanSet);
            var framenum = uimfRun.ScanSetCollection.ScanSetList.IndexOf(uimfRun.CurrentScanSet);

            var scanNum = uimfRun.IMSScanSetCollection.ScanSetList.IndexOf(uimfRun.CurrentIMSScanSet);
            var scanTotal = uimfRun.IMSScanSetCollection.ScanSetList.Count;

            var frameTotal = uimfRun.ScanSetCollection.ScanSetList.Count;



            var percentDone = ((double)(framenum) / (double)frameTotal + ((double)scanNum / (double)scanTotal) / (double)frameTotal) * 100;
            userstate.PercentDone = (float)percentDone;


            var logText = "Scan/Frame= " + Run.GetCurrentScanOrFrame() + "; PercentComplete= " + percentDone.ToString("0.0") + "; AccumlatedFeatures= " + Run.ResultCollection.getTotalIsotopicProfiles();

            var numScansBetweenProgress = 1;

            var imsScanIsLastInFrame = uimfRun.IMSScanSetCollection.GetLastScanSet() == uimfRun.CurrentIMSScanSet.PrimaryScanNumber;
            if (imsScanIsLastInFrame)
            {
                Logger.Instance.AddEntry(logText, Logger.Instance.OutputFilename);
                Console.WriteLine(DateTime.Now + "\t" + logText);
            }

            if (BackgroundWorker != null && scanNum % numScansBetweenProgress == 0)
            {

                BackgroundWorker.ReportProgress((int)percentDone, userstate);

            }

        }


    }
}
