﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend.Core;
using System.IO;
using DeconTools.Backend.Utilities;

namespace DeconTools.Backend.ProcessingTasks.ResultExporters.ScanResultExporters
{
    public class UIMFScanResult_TextFileExporter : ScanResult_TextFileExporter
    {
   
        #region Constructors
        public UIMFScanResult_TextFileExporter(string fileName) : base(fileName) { }
        #endregion




        #region Public Methods
        #endregion

        #region Private Methods
        #endregion

        protected override string buildScansResultOutput(ScanResult result)
        {
            StringBuilder sb = new StringBuilder();

            UIMFScanResult uimfScanResult = (UIMFScanResult)result;


            //sb.Append(uimfScanResult.Frameset.PrimaryFrame);

            //we want to report the unique 'FrameNum', not the non-unique 'Frame_index');
            sb.Append(uimfScanResult.FrameNum);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.ScanTime.ToString("0.###"));
            sb.Append(Delimiter);
            sb.Append(result.SpectrumType);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.BasePeak.Height);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.BasePeak.XValue.ToString("0.#####"));
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.TICValue);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.NumPeaks);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.NumIsotopicProfiles);
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.FramePressureFront.ToString("0.#####"));
            sb.Append(Delimiter);
            sb.Append(uimfScanResult.FramePressureBack.ToString("0.#####"));

            return sb.ToString();


        }

        protected override string buildHeaderLine()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("frame_num");
            sb.Append(Delimiter);
            sb.Append("frame_time");
            sb.Append(Delimiter);
            sb.Append("type");
            sb.Append(Delimiter);
            sb.Append("bpi");
            sb.Append(Delimiter);
            sb.Append("bpi_mz");
            sb.Append(Delimiter);
            sb.Append("tic");
            sb.Append(Delimiter);
            sb.Append("num_peaks");
            sb.Append(Delimiter);
            sb.Append("num_deisotoped");
            sb.Append(Delimiter);
            sb.Append("frame_pressure_front");
            sb.Append(Delimiter);
            sb.Append("frame_pressure_back");
           

            return sb.ToString();
        }


     
    }
}
