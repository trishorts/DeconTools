﻿using System;
using System.IO;
using DeconTools.Backend.Core;
using DeconTools.Utilities;

namespace DeconTools.Backend.Runs
{
#if !Disable_DeconToolsV2
    [Obsolete("Not used in DeconTools; Use BrukerRunV3", true)]
    public class BrukerRun : Run
    {

        public BrukerRun()
        {
            this.xyData = new XYData();
            this.IsDataThresholded = true;
            this.MSFileType = Globals.MSFileType.Bruker;
            this.ContainsMSMSData = false;
        }

        public BrukerRun(string folderName)
            : this()
        {
            this.Filename = folderName;
            this.DatasetName = getDataSetName(this.Filename);
            this.DataSetPath = getDataSetFolderName(this.Filename);

            validateFileNameAndFolderStructure();


            try
            {
                this.rawData = new DeconToolsV2.Readers.clsRawData();
                this.rawData.LoadFile(this.Filename, DeconToolsV2.Readers.FileType.BRUKER);
            }
            catch (Exception ex)
            {

                throw new Exception("ERROR:  Couldn't open the file.  Details: " + ex.Message);
            }
            this.MinLCScan = 1;        //  remember that DeconEngine is 1-based
            this.MaxLCScan = GetMaxPossibleLCScanNum();


        }


        public BrukerRun(string filename, int minScan, int maxScan)
            : this(filename)
        {
            this.MinLCScan = minScan;
            this.MaxLCScan = maxScan;
        }


        #region Properties

        [field: NonSerialized]
        private XYData xyData;
        public override XYData XYData
        {
            get
            {
                return xyData;
            }
            set
            {
                xyData = value;
            }
        }

#if !Disable_DeconToolsV2
        [field: NonSerialized]
        private DeconToolsV2.Readers.clsRawData rawData;
        public DeconToolsV2.Readers.clsRawData RawData
        {
            get { return rawData; }
            set { rawData = value; }
        }
#endif


        #endregion


        #region Methods
        public override int GetNumMSScans()
        {
            if (rawData == null) return 0;
            return this.rawData.GetNumScans();
        }

        public override XYData GetMassSpectrum(DeconTools.Backend.Core.ScanSet scanSet, double minMZ, double maxMZ)
        {
            Check.Require(scanSet != null, "Can't get mass spectrum; inputted set of scans is null");
            Check.Require(scanSet.IndexValues.Count > 0, "Can't get mass spectrum; no scan numbers inputted");

            var totScans = this.GetNumMSScans();

            var xvals = new double[0];
            var yvals = new double[0];

            if (scanSet.IndexValues.Count == 1)            //this is the case of only wanting one MS spectrum
            {
                this.rawData.GetSpectrum(scanSet.IndexValues[0], ref xvals, ref yvals);
            }
            else    // need to sum spectra
            {
                //assume:  each scan has exactly same x values

                //get first spectrum
                this.rawData.GetSpectrum(scanSet.IndexValues[0], ref xvals, ref yvals);

                //
                var summedYvals = new double[xvals.Length];
                yvals.CopyTo(summedYvals, 0);

                for (var i = 1; i < scanSet.IndexValues.Count; i++)
                {
                    this.rawData.GetSpectrum(scanSet.IndexValues[i], ref xvals, ref yvals);

                    for (var n = 0; n < xvals.Length; n++)
                    {
                        summedYvals[n] += yvals[n];
                    }
                }

                yvals = summedYvals;
            }

            var xydata = new XYData();
            xydata.Xvalues = xvals;
            xydata.Yvalues = yvals;
            return xydata;

        }

        public override double GetTime(int scanNum)
        {
            return this.rawData.GetScanTime(scanNum);
        }


        public override int GetMinPossibleLCScanNum()
        {
            return 1;
        }

        public override sealed int GetMaxPossibleLCScanNum()
        {
            return GetNumMSScans();
        }
        #endregion






        public override int GetMSLevelFromRawData(int scanNum)
        {

            if (!ContainsMSMSData) return 1;    // if we know the run doesn't contain MS/MS data, don't waste time checking
            int mslevel = (byte)this.rawData.GetMSLevel(scanNum);

            addToMSLevelData(scanNum, mslevel);

            return mslevel;
        }



        private void validateFileNameAndFolderStructure()
        {
            //check if the datasetPath is the same as the FileName, if so, change the Filename, or else DeconEngine will fail
            if (this.DataSetPath == this.Filename)
            {
                var dirinfo = new DirectoryInfo(this.Filename);

                var childFolders = dirinfo.GetDirectories();

                DirectoryInfo serFolder = null;

                foreach (var folder in childFolders)
                {
                    if (folder.Name.EndsWith("0.ser", StringComparison.OrdinalIgnoreCase))
                    {
                        serFolder = folder;
                        break;
                    }
                }

                if (serFolder != null)
                {
                    this.Filename = serFolder.FullName;

                }
            }


        }

        private string getDataSetFolderName(string p)
        {
            var trimmedPath = p.TrimEnd('\\');

            if (trimmedPath.EndsWith("acqus", StringComparison.OrdinalIgnoreCase))
            {
                var dirinfo = new DirectoryInfo(p);
                return dirinfo.Parent.FullName;
            }
            else if (trimmedPath.EndsWith("0.ser", StringComparison.OrdinalIgnoreCase))
            {
                var dirinfo = new DirectoryInfo(p);
                return dirinfo.Parent.FullName;
            }
            else
            {
                return trimmedPath;
            }
        }


        //TODO:  need to finalize what to expect in an unzipped file structure...  things are inconsistant right now (Oct 2010)
        private string getDataSetName(string filename)
        {


            var trimmedfilename = filename.TrimEnd(new char[] { '\\' });

            if (trimmedfilename.EndsWith("0.ser", StringComparison.OrdinalIgnoreCase))
            {
                var dirinfo = new DirectoryInfo(trimmedfilename);
                trimmedfilename = dirinfo.Parent.FullName.TrimEnd('\\');
            }


            if (trimmedfilename.EndsWith("acqus", StringComparison.OrdinalIgnoreCase))
            {
                var dirinfo = new DirectoryInfo(trimmedfilename);
                trimmedfilename = dirinfo.Parent.FullName.TrimEnd('\\');
            }



            var pathIndex = trimmedfilename.LastIndexOf('\\');
            if (pathIndex == -1) return String.Empty;



            return trimmedfilename.Substring((pathIndex + 1), (trimmedfilename.Length - pathIndex - 1));


        }

    }
#endif

}
