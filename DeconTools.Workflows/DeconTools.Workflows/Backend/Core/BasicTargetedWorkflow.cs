﻿using System;
using System.Collections.Generic;
using DeconTools.Backend;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.ProcessingTasks.ChromatogramProcessing;
using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend.ProcessingTasks.ResultValidators;
using DeconTools.Backend.ProcessingTasks.Smoothers;
using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
using DeconTools.Backend.ProcessingTasks.TheorFeatureGenerator;
using DeconTools.Utilities;

namespace DeconTools.Workflows.Backend.Core
{
    public class BasicTargetedWorkflow : WorkflowBase
    {
        private DeconToolsTargetedWorkflowParameters _workflowParameters;
        private TomTheorFeatureGenerator theorFeatureGen;
        private PeakChromatogramGenerator chromGen;
        private DeconToolsSavitzkyGolaySmoother chromSmoother;
        private ChromPeakDetector chromPeakDetector;
        private SmartChromPeakSelector chromPeakSelector;
        private DeconToolsPeakDetector msPeakDetector;
        private IterativeTFF msfeatureFinder;
        private MassTagFitScoreCalculator fitScoreCalc;
        private ResultValidatorTask resultValidator;


        #region Constructors

        public BasicTargetedWorkflow(Run run, DeconToolsTargetedWorkflowParameters parameters)
        {
            this.WorkflowParameters = parameters;
            this.Run = run;

            InitializeWorkflow();
        }

        public BasicTargetedWorkflow(DeconToolsTargetedWorkflowParameters parameters)
            : this(null, parameters)
        {

        }

        #endregion

        #region Properties
        string _name;
        public string Name
        {
            get
            { return this.ToString(); }
            set
            {
                _name = value;
            }

        }
        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion


        public override WorkflowParameters WorkflowParameters
        {
            get
            {
                return _workflowParameters;
            }
            set
            {
                _workflowParameters = value as DeconToolsTargetedWorkflowParameters;
            }
        }

        public override void InitializeWorkflow()
        {
            ValidateParameters();

            theorFeatureGen = new TomTheorFeatureGenerator(DeconTools.Backend.Globals.LabellingType.NONE, 0.005);

            chromGen = new PeakChromatogramGenerator(_workflowParameters.ChromToleranceInPPM, _workflowParameters.ChromGeneratorMode);
            chromGen.TopNPeaksLowerCutOff = 0.333;

            int pointsToSmooth = (_workflowParameters.ChromSmootherNumPointsInSmooth + 1) / 2;   // adding 0.5 prevents rounding problems
            chromSmoother = new DeconToolsSavitzkyGolaySmoother(pointsToSmooth, pointsToSmooth, 2);
            chromPeakDetector = new ChromPeakDetector(_workflowParameters.ChromPeakDetectorPeakBR, _workflowParameters.ChromPeakDetectorSigNoise);

            chromPeakSelector = new SmartChromPeakSelector((float)_workflowParameters.ChromNETTolerance, _workflowParameters.NumMSScansToSum);

            //chromPeakSelector.SetDefaultMSPeakDetectorSettings(_workflowParameters.SmartChromSelectorPeakBR, _workflowParameters.SmartChromSelectorPeakSigNoiseRatio, DeconTools.Backend.Globals.PeakFitType.QUADRATIC, false);


            msPeakDetector = new DeconToolsPeakDetector(_workflowParameters.MSPeakDetectorPeakBR, _workflowParameters.MSPeakDetectorSigNoise, DeconTools.Backend.Globals.PeakFitType.QUADRATIC, false);

            IterativeTFFParameters iterativeTFFParameters = new IterativeTFFParameters();
            iterativeTFFParameters.ToleranceInPPM = _workflowParameters.MSToleranceInPPM;

            msfeatureFinder = new IterativeTFF(iterativeTFFParameters);

            fitScoreCalc = new MassTagFitScoreCalculator();

            resultValidator = new ResultValidatorTask();


            ChromatogramXYData = new XYData();
            MassSpectrumXYData = new XYData();
            ChromPeaksDetected = new List<ChromPeak>();
        }

        private void ValidateParameters()
        {
            bool pointsInSmoothIsEvenNumber = (_workflowParameters.ChromSmootherNumPointsInSmooth % 2 == 0);
            if (pointsInSmoothIsEvenNumber)
            {
                throw new ArgumentOutOfRangeException("Points in chrom smoother is an even number, but must be an odd number.");
            }
        }

        public override void Execute()
        {
            Check.Require(this.Run != null, "Run has not been defined.");

            //TODO: remove this later:
            //this.Run.CreateDefaultScanToNETAlignmentData();

            this.Run.ResultCollection.MassTagResultType = DeconTools.Backend.Globals.MassTagResultType.BASIC_MASSTAG_RESULT;


            ResetStoredData();

            try
            {

                this.Result = this.Run.ResultCollection.GetMassTagResult(this.Run.CurrentMassTag);
                this.Result.ResetResult();
                

                executeTask(theorFeatureGen);
                executeTask(chromGen);
                executeTask(chromSmoother);
                updateChromDataXYValues(this.Run.XYData);

                executeTask(chromPeakDetector);
                updateChromDetectedPeaks(this.Run.PeakList);

                executeTask(chromPeakSelector);
                this.ChromPeakSelected = this.Result.ChromPeakSelected;

                executeTask(MSGenerator);
                updateMassSpectrumXYValues(this.Run.XYData);

                executeTask(msPeakDetector);
                executeTask(msfeatureFinder);

                executeTask(fitScoreCalc);
                executeTask(resultValidator);

            }
            catch (Exception ex)
            {
                MassTagResultBase result = (MassTagResultBase)this.Run.ResultCollection.GetMassTagResult(this.Run.CurrentMassTag);
                result.ErrorDescription = ex.Message + "\n" + ex.StackTrace;

                return;
            }
        }

        private void executeTask(Task task)
        {
            if (!Result.FailedResult)
            {
                task.Execute(this.Run.ResultCollection);
            }
        }
    }
}
