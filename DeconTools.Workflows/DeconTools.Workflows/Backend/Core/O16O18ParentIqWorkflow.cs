﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks.FitScoreCalculators;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend.ProcessingTasks.TargetedFeatureFinders;
using DeconTools.Backend.Utilities.IqLogger;
using DeconTools.Workflows.Backend.Core.ChromPeakSelection;
using DeconTools.Workflows.Backend.FileIO;
using GWSGraphLibrary;
using ZedGraph;

namespace DeconTools.Workflows.Backend.Core
{
    public class O16O18ParentIqWorkflow : IqWorkflow
    {

        protected PeakLeastSquaresFitter PeakFitter;

        private DeconTools.Backend.ProcessingTasks.PeakDetectors.DeconToolsPeakDetectorV2 _mspeakDetector;

        private BasicGraphControl _graphGenerator;
        #region Constructors

        public O16O18ParentIqWorkflow(Run run, TargetedWorkflowParameters parameters)
            : base(run, parameters)
        {
            PeakFitter=new PeakLeastSquaresFitter();
            _mspeakDetector = new DeconToolsPeakDetectorV2(parameters.MSPeakDetectorPeakBR,
                                                           parameters.MSPeakDetectorSigNoise,
                                                           DeconTools.Backend.Globals.PeakFitType.QUADRATIC,
                                                           run.IsDataThresholded);

           

            

        }

        public O16O18ParentIqWorkflow(TargetedWorkflowParameters parameters)
            : this(null, parameters)
        {

        }

        #endregion

        #region Properties

        public string OutputFolderForGraphs { get; set; }

        public bool GraphsAreOutputted { get; set; }

        #endregion

        #region Public Methods

        protected override void DoPostInitialization()
        {
            MsfeatureFinder = new O16O18IterativeTff(IterativeTffParameters);
            ChromatogramCorrelator = new O16O18ChromCorrelator(WorkflowParameters.ChromSmootherNumPointsInSmooth, 0.025,
                                                               WorkflowParameters.ChromGenTolerance,
                                                               WorkflowParameters.ChromGenToleranceUnit);

        }

        protected override void ExecuteWorkflow(IqResult result)
        {
            var children = result.Target.ChildTargets().ToList();
            foreach (IqTarget child in children)
            {
                child.DoWorkflow();
                IqResult childResult = child.GetResult();

                var chromPeakLevelResults = childResult.ChildResults();

                var filteredChromPeakResults = chromPeakLevelResults.Where(r => r.IsotopicProfileFound).ToList();

                childResult.FavoriteChild = SelectBestChromPeakIqResult(childResult, filteredChromPeakResults);

                GetDataFromFavoriteChild(childResult);
            }

            result.FavoriteChild = SelectBestChargeStateChildResult(result);
            GetDataFromFavoriteChild(result);

            var favResult = result.FavoriteChild;

            double? rsquaredVal, slope;
            getRsquaredVal(result, out rsquaredVal, out slope);
            int favChargeState = result.FavoriteChild == null ? 0 : result.FavoriteChild.Target.ChargeState;
            double favMz = result.FavoriteChild == null ? 0 : result.FavoriteChild.Target.MZTheor;

            IqLogger.Log.Info("\t" + result.Target.ID + "\t\t\t" + favMz.ToString("0.000") + "\t" + favChargeState + "\t" + result.LcScanObs + "\t" + rsquaredVal + "\t" + slope);

            //now get the mass spectrum given the info from the favorite child charge state result

            if (favResult!=null)
            {
                ScanSet scanset = new ScanSetFactory().CreateScanSet(Run, favResult.LCScanSetSelected.PrimaryScanNumber,
                                                                     WorkflowParameters.NumMSScansToSum);

                var selectedChromPeak = favResult.ChromPeakSelected;
                double sigma = selectedChromPeak.Width/2.35;
                double chromScanWindowWidth = 4 * sigma;

                //Determines where to start and stop chromatogram correlation
                int startScan = scanset.PrimaryScanNumber - (int)Math.Round(chromScanWindowWidth / 2, 0);
                int stopScan = scanset.PrimaryScanNumber + (int)Math.Round(chromScanWindowWidth / 2, 0);

                var massSpectrum=   MSGenerator.GenerateMS(Run, scanset);

                foreach (var iqTarget in children)
                {
                    var childStateIqResult = (O16O18IqResult) iqTarget.GetResult();

                    childStateIqResult.IqResultDetail.MassSpectrum = massSpectrum.TrimData(iqTarget.MZTheor - 3, iqTarget.MZTheor + 8);
                    
                    List<Peak> mspeakList = _mspeakDetector.FindPeaks(childStateIqResult.IqResultDetail.MassSpectrum.Xvalues,
                                                                      childStateIqResult.IqResultDetail.MassSpectrum.Yvalues);


                    childStateIqResult.CorrelationData = ChromatogramCorrelator.CorrelateData(Run, childStateIqResult, startScan, stopScan);


                    childStateIqResult.CorrelationO16O18SingleLabel = childStateIqResult.GetCorrelationO16O18SingleLabel();
                    childStateIqResult.CorrelationO16O18DoubleLabel = childStateIqResult.GetCorrelationO16O18DoubleLabel();
                    childStateIqResult.RatioO16O18 = childStateIqResult.GetRatioO16O18();


                    childStateIqResult.ObservedIsotopicProfile=  MsfeatureFinder.IterativelyFindMSFeature(childStateIqResult.IqResultDetail.MassSpectrum,
                                                             iqTarget.TheorIsotopicProfile);

                   
                    if (childStateIqResult.ObservedIsotopicProfile!=null)
                    {
                        
             

                        List<Peak> observedIsoList = childStateIqResult.ObservedIsotopicProfile.Peaklist.Cast<Peak>().Take(4).ToList();    //first 4 peaks excludes the O18 double label peak (fifth peak)
                        var theorPeakList = iqTarget.TheorIsotopicProfile.Peaklist.Select(p => (Peak) p).Take(4).ToList();
                        childStateIqResult.FitScore = PeakFitter.GetFit(theorPeakList, observedIsoList, 0.05, WorkflowParameters.MSToleranceInPPM);
                        childStateIqResult.InterferenceScore = InterferenceScorer.GetInterferenceScore(childStateIqResult.ObservedIsotopicProfile, mspeakList);
                        childStateIqResult.MZObs = childStateIqResult.ObservedIsotopicProfile.MonoPeakMZ;
                        childStateIqResult.MonoMassObs = childStateIqResult.ObservedIsotopicProfile.MonoIsotopicMass;
                        childStateIqResult.MZObsCalibrated = Run.GetAlignedMZ(childStateIqResult.MZObs);
                        childStateIqResult.MonoMassObsCalibrated = (childStateIqResult.MZObsCalibrated - DeconTools.Backend.Globals.PROTON_MASS) * childStateIqResult.Target.ChargeState;
                        childStateIqResult.ElutionTimeObs = ((ChromPeak) favResult.ChromPeakSelected).NETValue;


                    }
                    else
                    {
                        childStateIqResult.FitScore = -1;
                        childStateIqResult.InterferenceScore = -1;
                    }


                    getRsquaredVal(childStateIqResult, out rsquaredVal, out slope);
                    IqLogger.Log.Info("\t\t\t" + childStateIqResult.Target.ID + "\t" + childStateIqResult.Target.MZTheor.ToString("0.000") + "\t" + childStateIqResult.Target.ChargeState
                        + "\t" + childStateIqResult.LcScanObs + "\t" + childStateIqResult.FitScore.ToString("0.000") + "\t" + rsquaredVal + "\t" + slope);

                    if (GraphsAreOutputted)
                    {
                        if (_graphGenerator==null) _graphGenerator=new BasicGraphControl();

                        ExportGraphs(childStateIqResult);
                    }
                    
                   

                    childStateIqResult.LCScanSetSelected = favResult.LCScanSetSelected;
                    childStateIqResult.LcScanObs = favResult.LcScanObs;
                }
            }
        }

       

        private void ExportGraphs(IqResult result)
        {
            OutputFolderForGraphs = Run.DataSetPath +Path.DirectorySeparatorChar + "OutputGraphs";
            if (!Directory.Exists(OutputFolderForGraphs)) Directory.CreateDirectory(OutputFolderForGraphs);
            
            ExportMassSpectrumGraph(result);
            ExportChromGraph(result);
        }

        private void ExportMassSpectrumGraph(IqResult result)
        {
            _graphGenerator.GraphHeight = 600;
            _graphGenerator.GraphWidth = 800;

            _graphGenerator.GenerateGraph(result.IqResultDetail.MassSpectrum.Xvalues, result.IqResultDetail.MassSpectrum.Yvalues);
            var line = _graphGenerator.GraphPane.CurveList[0] as LineItem;
            line.Line.IsVisible = true;
            line.Symbol.Size = 3;
            line.Line.Width = 2;
            line.Symbol.Type = SymbolType.None;
            line.Color = Color.Black;

            _graphGenerator.GraphPane.XAxis.Title.Text = "m/z";
            _graphGenerator.GraphPane.YAxis.Title.Text = "intensity";
            _graphGenerator.GraphPane.XAxis.Scale.MinAuto = true;
            _graphGenerator.GraphPane.YAxis.Scale.MinAuto = false;
            _graphGenerator.GraphPane.YAxis.Scale.MaxAuto = false;

            _graphGenerator.GraphPane.YAxis.Scale.Min = 0;


            _graphGenerator.GraphPane.YAxis.Scale.Max = result.IqResultDetail.MassSpectrum.getMaxY();
            _graphGenerator.GraphPane.YAxis.Scale.Format = "0";


            _graphGenerator.GraphPane.XAxis.Scale.FontSpec.Size = 12;
            string outputGraphFilename = OutputFolderForGraphs + Path.DirectorySeparatorChar + result.Target.ID + "_" +
                                         result.Target.ChargeState + "_" + result.Target.MZTheor.ToString("0.000") + "_MS.png";

            _graphGenerator.AddAnnotationAbsoluteXRelativeY("*", result.Target.MZTheor, 0.03);
            _graphGenerator.SaveGraph(outputGraphFilename);
        }

        private void ExportChromGraph(IqResult result)
        {
            if (result.IqResultDetail.Chromatogram==null)
            {
                return;
            }


            int minScan = result.LcScanObs - 1000;
            int maxScan = result.LcScanObs + 1000;


            _graphGenerator.GraphHeight = 600;
            _graphGenerator.GraphWidth = 800;

            result.IqResultDetail.Chromatogram = result.IqResultDetail.Chromatogram.TrimData(minScan, maxScan);


            _graphGenerator.GenerateGraph(result.IqResultDetail.Chromatogram.Xvalues, result.IqResultDetail.Chromatogram.Yvalues);
            var line = _graphGenerator.GraphPane.CurveList[0] as LineItem;
            line.Line.IsVisible = true;
            line.Symbol.Size = 3;
            line.Line.Width = 2;
            line.Symbol.Type = SymbolType.None;
            line.Color = Color.Black;

            _graphGenerator.GraphPane.XAxis.Title.Text = "scan";
            _graphGenerator.GraphPane.YAxis.Title.Text = "intensity";
            _graphGenerator.GraphPane.XAxis.Scale.MinAuto = false;
            _graphGenerator.GraphPane.XAxis.Scale.MaxAuto = false;
            _graphGenerator.GraphPane.YAxis.Scale.MinAuto = false;
            _graphGenerator.GraphPane.YAxis.Scale.MaxAuto = false;
            _graphGenerator.GraphPane.YAxis.Scale.Min = 0;
            _graphGenerator.GraphPane.YAxis.Scale.Max = result.IqResultDetail.Chromatogram.getMaxY();
            _graphGenerator.GraphPane.YAxis.Scale.Format = "0";

            _graphGenerator.GraphPane.XAxis.Scale.Min = minScan;
            _graphGenerator.GraphPane.XAxis.Scale.Max = maxScan;

            _graphGenerator.GraphPane.XAxis.Scale.FontSpec.Size = 12;
            string outputGraphFilename = OutputFolderForGraphs + Path.DirectorySeparatorChar + result.Target.ID + "_" +
                                         result.Target.ChargeState + "_" + result.Target.MZTheor.ToString("0.000") + "_chrom.png";

            _graphGenerator.AddAnnotationAbsoluteXRelativeY("*", result.Target.MZTheor, 0.03);
            _graphGenerator.SaveGraph(outputGraphFilename);
        }



        private void getRsquaredVal(IqResult result, out double? rsquaredVal, out double? slope)
        {
            if (result.CorrelationData!=null && result.CorrelationData.CorrelationDataItems.Count>0)
            {
                rsquaredVal = result.CorrelationData.RSquaredValsMedian;
                
                slope = result.CorrelationData.CorrelationDataItems.First().CorrelationSlope;

            }
            else
            {
                rsquaredVal = -1;
                slope = -1;
            }

            

        }

        private void GetDataFromFavoriteChild(IqResult result)
        {
            var fav = result.FavoriteChild;

            if (fav == null)
            {
                result.LCScanSetSelected = null;
                result.LcScanObs = -1;
                result.MZObs = 0;
                result.MZObsCalibrated = 0;
                result.Abundance = 0;
                result.ElutionTimeObs = -1;
                result.IsotopicProfileFound = false;
                result.InterferenceScore = 1;
                result.FitScore = 1;
                result.MassErrorBefore = 0;
                result.MassErrorAfter = 0;
                result.MonoMassObs = 0;
                result.MonoMassObsCalibrated = 0;
            }
            else
            {
                result.LCScanSetSelected = fav.LCScanSetSelected;
                result.LcScanObs = fav.LcScanObs;
                result.CorrelationData = fav.CorrelationData;
                result.FitScore = fav.FitScore;
                result.ChromPeakSelected = fav.ChromPeakSelected;
                result.MZObs = fav.MZObs;
                result.MZObsCalibrated = fav.MZObsCalibrated;
                result.MonoMassObs = fav.MonoMassObs;
                result.MonoMassObsCalibrated = fav.MonoMassObsCalibrated;
                result.ElutionTimeObs = fav.ElutionTimeObs;
                result.Abundance = fav.ChromPeakSelected == null ? 0f : fav.ChromPeakSelected.Height;


            }
        }

        private IqResult SelectBestChargeStateChildResult(IqResult result)
        {
            var filteredChargeStateResults = result.ChildResults().Where(p => p.FavoriteChild != null).ToList();

            if (filteredChargeStateResults.Count == 0) return null;

            if (filteredChargeStateResults.Count == 1) return filteredChargeStateResults.First();

            //now to deal with the tough issue of multiple charge states having a possible results. 

            var filter2 = filteredChargeStateResults.Where(p => p.CorrelationData.CorrelationDataItems.First().CorrelationRSquaredVal > 0.7).ToList();

            if (filter2.Count == 0)
            {
                filter2 = filteredChargeStateResults.Where(p => p.CorrelationData.CorrelationDataItems.First().CorrelationRSquaredVal > 0.5).ToList();
            }

            if (filter2.Count == 0)
            {
                filter2 = filteredChargeStateResults.Where(p => p.CorrelationData.CorrelationDataItems.First().CorrelationRSquaredVal > 0.3).ToList();
            }

            if (filter2.Count == 0)
            {
                //correlation values are no good. Now will sort by fit score.
                filter2 = filteredChargeStateResults.OrderBy(p => p.FitScore).Take(1).ToList();   //sort by fit score let the first one be selected
            }

            if (filter2.Count == 1)
            {
                return filter2.First();
            }
            else if (filter2.Count > 1)
            {
                //if we reached here, there are multiple charge state results with good correlation scores.  Take the one of highest intensity.
                return filter2.OrderByDescending(p => p.Abundance).First();


            }

            return null;


        }

        private IqResult SelectBestChromPeakIqResult(IqResult childResult, List<IqResult> filteredChromPeakResults)
        {
            int numCandidateResults = filteredChromPeakResults.Count;
            IqResult bestChromPeakResult;
            if (numCandidateResults == 0)
            {
                bestChromPeakResult = null;
            }
            else if (numCandidateResults == 1)
            {
                bestChromPeakResult = filteredChromPeakResults.First();
            }
            else
            {
                var furtherFilteredResults =
                    filteredChromPeakResults.OrderByDescending(
                        p => p.CorrelationData.CorrelationDataItems.First().CorrelationRSquaredVal).ToList();

                bestChromPeakResult = furtherFilteredResults.First();
            }


            if (bestChromPeakResult != null)
            {
                childResult.CorrelationData = bestChromPeakResult.CorrelationData;
                childResult.LcScanObs = bestChromPeakResult.LcScanObs;
                childResult.ChromPeakSelected = bestChromPeakResult.ChromPeakSelected; //check this
                childResult.LCScanSetSelected = bestChromPeakResult.LCScanSetSelected;


                var elutionTime = childResult.ChromPeakSelected == null
                                      ? 0d
                                      : ((ChromPeak)bestChromPeakResult.ChromPeakSelected).NETValue;
                childResult.ElutionTimeObs = elutionTime;

                childResult.Abundance = (float)(childResult.ChromPeakSelected == null
                                                     ? 0d
                                                     : ((ChromPeak)bestChromPeakResult.ChromPeakSelected).Height);
            }

            return bestChromPeakResult;
        }

        #endregion

        #region Private Methods

        #endregion


        protected internal override IqResult CreateIQResult(IqTarget target)
        {
            return new O16O18IqResult(target);
        }

        public override TargetedWorkflowParameters WorkflowParameters { get; set; }

        public override IqResultExporter CreateExporter()
        {
            return new O16O18IqResultExporter();
        }
    }
}