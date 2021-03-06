﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Algorithms;
using DeconTools.Backend.Core;
using DeconTools.Backend.DTO;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.ProcessingTasks.MSGenerators;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend.ProcessingTasks.Smoothers;
using DeconTools.Backend.Runs;

namespace DeconTools.Workflows.Backend.Core
{
#if !Disable_DeconToolsV2
    public class IMS_WholisticFeatureFinderWorkflow : WorkflowBase
    {
        List<MSPeakResult> processedMSPeaks;


        #region Constructors
        public IMS_WholisticFeatureFinderWorkflow(Run run)
        {
            this.Run = run;
            InitializeWorkflow();
        }
        #endregion

        #region Properties

        DeconToolsPeakDetectorV2 MasterPeakListPeakDetector { get; set; }

        public SavitzkyGolaySmoother ChromSmoother { get; set; }
        public ChromPeakDetector ChromPeakDetector { get; set; }

        public ChromatogramGenerator ChromGenerator { get; set; }


        public MSGenerator msgen { get; set; }

        public double DriftTimeProfileExtractionPPMTolerance { get; set; }

        public MSGenerator MSgen { get; set; }
        public DeconToolsPeakDetectorV2 MSPeakDetector { get; set; }
        public Deconvolutor Deconvolutor { get; set; }

        public int NumMSScansToSumWhenBuildingMasterPeakList { get; set; }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion


        #region IWorkflow Members

        public string Name { get; set; }

        //public override WorkflowParameters WorkflowParameters
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}


        public void InitializeWorkflow()
        {

            NumMSScansToSumWhenBuildingMasterPeakList = 3;

            MasterPeakListPeakDetector = new DeconToolsPeakDetectorV2();
            MasterPeakListPeakDetector.PeakToBackgroundRatio = 4;
            MasterPeakListPeakDetector.SignalToNoiseThreshold = 3;
            MasterPeakListPeakDetector.IsDataThresholded = false;
            MasterPeakListPeakDetector.PeaksAreStored = true;

            
            msgen = MSGeneratorFactory.CreateMSGenerator(DeconTools.Backend.Globals.MSFileType.PNNL_UIMF);



            this.DriftTimeProfileExtractionPPMTolerance = 15;

            this.ChromSmoother = new SavitzkyGolaySmoother(23,2);
            this.ChromPeakDetector = new ChromPeakDetector(0.5, 0.5);


            this.ChromGenerator = new ChromatogramGenerator();

            processedMSPeaks = new List<MSPeakResult>();
        }

        public override void Execute()
        {

            var uimfRun = (UIMFRun)this.Run;

            //for each frame

            foreach (var frame in uimfRun.ScanSetCollection.ScanSetList)
            {
                uimfRun.CurrentScanSet = frame;


                // detect all peaks in frame
                var masterPeakList = getAllPeaksInFrame(uimfRun, NumMSScansToSumWhenBuildingMasterPeakList);

                // sort peaks
                masterPeakList.Sort(delegate(MSPeakResult peak1, MSPeakResult peak2)
                {
                    return peak2.MSPeak.Height.CompareTo(peak1.MSPeak.Height);
                });


                // for each peak
                var peakCounter = 0;
                var peaksThatGenerateAChromatogram = 0;
                foreach (var peak in masterPeakList)
                {
                    peakCounter++;

                    //if (peakCounter > 500) break;
                    if (peak.MSPeak.Height < 1000) break;


                    var peakFate = "Undefined";

                    var peakResultAlreadyIncludedInChromatogram = (peak.ChromID != -1);
                    if (peakResultAlreadyIncludedInChromatogram)
                    {
                        peakFate = "Chrom_Already";

                        displayPeakInfoAndFate(peak, peakFate);


                        continue;
                    }
                    else
                    {
                        peakFate = "CHROM";

                        //bool peakResultAlreadyFoundInAnMSFeature = findPeakWithinMSFeatureResults(run.ResultCollection.ResultList, peakResult, scanTolerance);
                        //if (peakResultAlreadyFoundInAnMSFeature)
                        //{
                        //    peakFate = "MSFeature_Already";
                        //}
                        //else
                        //{
                        //    peakFate = "CHROM";
                        //}



                    }

                    peaksThatGenerateAChromatogram++;
                    PeakChrom chrom = new BasicPeakChrom();

                    // create drift profile from raw data
                    var driftTimeProfileMZTolerance = this.DriftTimeProfileExtractionPPMTolerance * peak.MSPeak.XValue / 1e6;
                    
                    //TODO: Fix this: update to use UIMF library and not DeconTools
                    //uimfRun.GetDriftTimeProfile  (frame.PrimaryFrame, this.Run.MinScan, this.Run.MaxScan, peak.MSPeak.XValue, driftTimeProfileMZTolerance);

                    var driftTimeProfileIsEmpty = (uimfRun.XYData.Xvalues == null);
                    if (driftTimeProfileIsEmpty)
                    {
                        addPeakToProcessedPeakList(peak);
                        peakFate = peakFate + " DriftProfileEmpty";
                        displayPeakInfoAndFate(peak, peakFate);

                        continue;
                    }

                    chrom.XYData = uimfRun.XYData;


                    // smooth drift profile
                    chrom.XYData = ChromSmoother.Smooth(uimfRun.XYData);

                    // detect peaks in chromatogram
                    chrom.PeakList = this.ChromPeakDetector.FindPeaks(chrom.XYData, 0, 0);

                    if (chrom.PeakDataIsNullOrEmpty)
                    {
                        addPeakToProcessedPeakList(peak);
                        peakFate = peakFate + " NoChromPeaksDetected";
                        displayPeakInfoAndFate(peak, peakFate);


                        continue;
                    }

                    // find which drift profile peak,  if any, the source peak is a member of
                    var chromPeak = chrom.GetChromPeakForGivenSource(peak);
                    if (chromPeak == null)
                    {
                        addPeakToProcessedPeakList(peak);
                        peakFate = peakFate + " TargetChromPeakNotFound";
                        displayPeakInfoAndFate(peak, peakFate);


                        continue;
                    }

                 
                    // find other peaks in the master peaklist that are members of the found drift profile peak
                    // tag these peaks with the source peak's ID
                    var peakWidthSigma = chromPeak.Width / 2.35;      //   width@half-height =  2.35σ   (Gaussian peak theory)

                    var minScanForChrom = (int)Math.Floor(chromPeak.XValue - peakWidthSigma * 4);
                    var maxScanForChrom = (int)Math.Floor(chromPeak.XValue + peakWidthSigma * 4);

                    var peakToleranceInMZ = driftTimeProfileMZTolerance;
                    var minMZForChromFilter = peak.MSPeak.XValue - peakToleranceInMZ;
                    var maxMZForChromFilter = peak.MSPeak.XValue + peakToleranceInMZ;

                    
                    chrom.ChromSourceData = (from n in masterPeakList
                                  where n.Scan_num >= minScanForChrom && n.Scan_num <= maxScanForChrom &&
                                      n.MSPeak.XValue >= minMZForChromFilter && n.MSPeak.XValue < maxMZForChromFilter
                                  select n).ToList();
                    
                    foreach (var item in chrom.ChromSourceData)
                    {
                        item.ChromID = peak.PeakID;
                    }

                    displayPeakInfoAndFate(peak, peakFate);



                }

                Console.WriteLine("peaksProcessed = " + peakCounter);
                Console.WriteLine("peaks generating a chrom = " + peaksThatGenerateAChromatogram);



            }











            // generate MS by integrating over drift profile peak

            // find MS peaks within range

            // find MS Features. 

            // find MS Feature for which the source peak is a member of.  


            // if found, add it. 
            // And, for each MS peaks of the found MS Feature,  mark all peaks of the masterpeak list that correspond to the found drift time peak and m/z

        }

        private void displayPeakInfoAndFate(MSPeakResult peak, string peakFate)
        {
            var sb = new StringBuilder();
            sb.Append(peak.PeakID);
            sb.Append('\t');
            sb.Append(peak.Scan_num);
            sb.Append('\t');
            sb.Append(peak.MSPeak.XValue);
            sb.Append('\t');
            sb.Append(peak.MSPeak.Height);
            sb.Append('\t');
            sb.Append(peakFate);
            sb.Append('\t');
            sb.Append(peak.ChromID);
            Console.WriteLine(sb.ToString());

        }

        private void addPeakToProcessedPeakList(MSPeakResult peak)
        {
            peak.ChromID = peak.PeakID;
            this.processedMSPeaks.Add(peak);
        }

        private List<MSPeakResult> getAllPeaksInFrame(UIMFRun uimfRun, int numIMSScansToSum)
        {
            if (uimfRun.ResultCollection.MSPeakResultList != null)
            {
                uimfRun.ResultCollection.MSPeakResultList.Clear();
            }

            uimfRun.ScanSetCollection.Create(uimfRun, numIMSScansToSum, 1);
            
            foreach (var scan in uimfRun.ScanSetCollection.ScanSetList)
            {
                uimfRun.CurrentScanSet = scan;
                msgen.Execute(uimfRun.ResultCollection);
                MasterPeakListPeakDetector.Execute(uimfRun.ResultCollection);

            }

            return uimfRun.ResultCollection.MSPeakResultList;

        }

        #endregion

        public override void InitializeRunRelatedTasks()
        {
            throw new NotImplementedException();
        }

        public WorkflowParameters WorkflowParameters
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
#endif
}
