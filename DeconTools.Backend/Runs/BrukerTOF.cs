﻿using System;
using System.IO;
using DeconTools.Backend.Core;
using EDAL;

namespace DeconTools.Backend.Runs
{
    public class BrukerTOF : Run
    {
        
        private EDAL.IMSAnalysis _msAnalysis;
        private MSSpectrumCollection _spectrumCollection;

        public BrukerTOF()
        {
            XYData = new XYData();
            IsDataThresholded = true;
            MSFileType = Globals.MSFileType.Bruker;
            ContainsMSMSData = false;
        }


        public BrukerTOF(string folderName)
            : this()
        {

            if (!Directory.Exists(folderName))
            {
                throw new DirectoryNotFoundException(
                    "Could not create Bruker dataset. Folder path does not exist. Ensure you are pointing to parent folder that contains the raw MS files (eg analysis.baf)");

            }

            bool isDir;

            try
            {
                isDir = (File.GetAttributes(folderName) & FileAttributes.Directory)
                 == FileAttributes.Directory;

            }
            catch (Exception exception)
            {
                
                throw new IOException( "Could not create Bruker dataset. Folder path does not exist. Ensure you are pointing to parent folder that contains the raw MS files (eg analysis.baf)",exception);

            }

            
            _msAnalysis = new MSAnalysis();
            _msAnalysis.Open(folderName);

            Filename = folderName;
            _spectrumCollection = _msAnalysis.MSSpectrumCollection;
            DatasetName = getDatasetName(Filename);
            DataSetPath = getDatasetfolderName(Filename);

            MinScan = GetMinPossibleScanNum();
            MaxScan = GetMaxPossibleScanNum();

        }




        public override XYData XYData { get; set; }
        public override int GetNumMSScans()
        {
            return _spectrumCollection.Count;
        }

        public override double GetTime(int scanNum)
        {

            var spectrum = _spectrumCollection[scanNum];

            return spectrum.RetentionTime;



        }

        public override int GetMSLevelFromRawData(int scanNum)
        {

            var spectrum = _spectrumCollection[scanNum];

            return spectrum.MSMSStage;
            

            
           
        }

        public override void GetMassSpectrum(ScanSet scanset, double minMZ, double maxMZ)
        {


            GetMassSpectrum(scanset);


        }

        public override void GetMassSpectrum(ScanSet scanset)
        {
            object mzVals;
            object intensityVals;

            var spectrum = _spectrumCollection[scanset.PrimaryScanNumber];


            spectrum.GetMassIntensityValues(SpectrumTypes.SpectrumType_Profile, out mzVals, out intensityVals);

            XYData.Xvalues = (double[]) mzVals;
            XYData.Yvalues = (double[])intensityVals;

        }


        public override int GetMinPossibleScanNum()
        {
            return 1;     //one-based
        }

        public override int GetMaxPossibleScanNum()
        {
            return GetNumMSScans();
        }

        private string getDatasetName(string fullFolderPath)
        {

            DirectoryInfo dirInfo = new DirectoryInfo(fullFolderPath);

            if (dirInfo.Name.EndsWith(".d", StringComparison.OrdinalIgnoreCase))
            {
                return dirInfo.Name.Substring(0, dirInfo.Name.Length - ".d".Length);
            }
            else
            {
                return dirInfo.Name;
            }

        }

        private string getDatasetfolderName(string fullFolderPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(fullFolderPath);
            return dirInfo.FullName;
        }

    }
}
