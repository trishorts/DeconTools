﻿using System.IO;
using System.Collections.Generic;
using DeconTools.Backend.Core;
using DeconTools.Backend.Data;
using DeconTools.Utilities;

namespace DeconTools.Backend.ProcessingTasks.NETAlignment
{
    public class ChromAlignerUsingVIPERInfo
    {
        string m_targetUMCFileStoringAlignmentInfo;

        #region Constructors
        public ChromAlignerUsingVIPERInfo()
        {

        }

        public ChromAlignerUsingVIPERInfo(string targetUMCFileStoringAlignmentInfo)
        {
            this.m_targetUMCFileStoringAlignmentInfo = targetUMCFileStoringAlignmentInfo;
        }


        #endregion

        #region Properties
        #endregion

        #region Public Methods

        public void Execute(Run run)
        {
            Check.Require(run != null, "'Run' has not be defined");
            Check.Require(run.Filename != null &&
                run.Filename.Length > 0, "MS data file ('run') has not been initialized");
            //Check.Require(run.ScanSetCollection != null && run.ScanSetCollection.ScanSetList.Count > 0, "ChromAligner failed. ScanSets have not been defined.");

            if (m_targetUMCFileStoringAlignmentInfo == null)
            {
                var baseFileName = Path.Combine(run.DataSetPath, run.DatasetName);
                m_targetUMCFileStoringAlignmentInfo = baseFileName + "_UMCs.txt";
            }
            Check.Require(File.Exists(m_targetUMCFileStoringAlignmentInfo), "ChromAligner failed. The UMC file from which alignment data is extracted does not exist.");

            var umcs = new UMCCollection();

            var importer = new UMCFileImporter(m_targetUMCFileStoringAlignmentInfo, '\t');
            umcs = importer.Import();

            var scannetPairs = umcs.GetScanNETLookupTable();

            run.NetAlignmentInfo = new NetAlignmentInfoBasic(run.MinLCScan, run.MaxLCScan);
            run.NetAlignmentInfo.SetScanToNETAlignmentData(scannetPairs);
            
        }



        #endregion

        #region Private Methods
        #endregion
    }
}
