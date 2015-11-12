﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeconTools.Backend.Core;
using DeconTools.Backend.ProcessingTasks;
using DeconTools.Backend.ProcessingTasks.PeakDetectors;
using DeconTools.Backend.Runs;
using NUnit.Framework;

namespace DeconTools.UnitTesting2.Run_relatedTests
{
    [TestFixture]
    public class MzRun_Tests
    {
        [TestFixtureSetUp]
        public void ConfigureDllLookup()
        {
            pwiz.ProteowizardWrapper.DependencyLoader.AddAssemblyResolver();
        }

        [Test]
        public void constructorTest1()
        {
            string testfile =
                @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzXML";

            Run run = new MzRun(testfile);
            Assert.AreEqual(Backend.Globals.MSFileType.MZXML_Rawdata, run.MSFileType);
            Assert.AreEqual(18505, run.GetNumMSScans());

            Assert.AreEqual(0, run.MinLCScan);
            Assert.AreEqual(18504, run.MaxLCScan);

            Assert.AreEqual("QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18", run.DatasetName);
            Assert.AreEqual(@"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML", run.DataSetPath);

        }


        [Test]
        public void GetMassSpectrumTest1()
        {
            int testscan = 6004;

            string testfile =
               @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzXML";

            Run run = new MzRun(testfile);

            ScanSet scanSet = new ScanSet(testscan);
            run.GetMassSpectrum(scanSet);

            Assert.AreEqual(481.274514196002, (decimal)run.XYData.Xvalues[3769]);
            Assert.AreEqual(13084442, run.XYData.Yvalues[3769]);

            //TestUtilities.DisplayXYValues(run.XYData);
        }

        [Test]
        [ExpectedException(typeof(System.ArgumentOutOfRangeException))]
        public void GetMassSpectrum_higherThanTotalScans()
        {
            int testscan = 18506;

            string testfile =
               @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzXML";

            Run run = new MzRun(testfile);

            ScanSet scanSet = new ScanSet(testscan);
            run.GetMassSpectrum(scanSet);
        }

        [Test]
        public void GetMSLevelsTest1()
        {

            int testscan = 6004;
            int testscan2 = 6005;

            string testfile =
               @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzXML";

            Run run = new MzRun(testfile);

            int level=  run.GetMSLevel(testscan);

			var scanTime = run.GetTime(testscan);


			Assert.AreEqual(1, run.GetMSLevel(testscan));
			Assert.AreEqual(2, run.GetMSLevel(testscan2));

			Assert.AreEqual(1961.65, (decimal)run.GetTime(testscan));

        }


        [Test]
        public void ValidateDataTest1()    //purpose is to compare 
        {

            string testMz5File = @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mz5";
            string testThermoFile = FileRefs.RawDataMSFiles.OrbitrapStdFile1;

            Run mz5run = new RunFactory().CreateRun(testMz5File);

            Run thermoRun = new RunFactory().CreateRun(testThermoFile);

            int testScanMz5 = 6004;
            int testScanThermo = 6005;

            ScanSet testScanSet1 = new ScanSet(testScanMz5);
            ScanSet testScanSetThermo = new ScanSet(testScanThermo);

            mz5run.CurrentScanSet = testScanSet1;
            thermoRun.CurrentScanSet = testScanSetThermo;


            mz5run.XYData=   mz5run.GetMassSpectrum(testScanSet1);
            thermoRun.XYData=   thermoRun.GetMassSpectrum(testScanSetThermo);


            Assert.AreEqual(mz5run.XYData.Xvalues.Length, thermoRun.XYData.Xvalues.Length);

            for (int i = 0; i < mz5run.XYData.Xvalues.Length; i++)
            {
                Assert.AreEqual(mz5run.XYData.Xvalues[i], thermoRun.XYData.Xvalues[i]);
            }

            var peakDetector = new DeconToolsPeakDetectorV2();
            peakDetector.PeakToBackgroundRatio = 3;
            peakDetector.SignalToNoiseThreshold = 2;

            //peakDetector.Execute(mz5run.ResultCollection);

            peakDetector.Execute(thermoRun.ResultCollection);


            //TestUtilities.DisplayPeaks(mz5run.PeakList);

            Console.WriteLine();
            Console.WriteLine();

            TestUtilities.DisplayPeaks(thermoRun.PeakList);


        }


        [Test]
        public void ValidateDataTest_temp()    //purpose is to compare 
        {

            string testMz5File = @"C:\Sipper\SipperDemo\RawDataFiles\Yellow_C13_070_23Mar10_Griffin_10-01-28.mz5";
            string testThermoFile =
                @"\\protoapps\UserData\Slysz\Standard_Testing\Targeted_FeatureFinding\SIPPER_standard_testing\Yellow_C13_070_23Mar10_Griffin_10-01-28.raw";

            Run mz5run = new RunFactory().CreateRun(testMz5File);
            mz5run.IsDataThresholded = true;

            Run thermoRun = new RunFactory().CreateRun(testThermoFile);

            int testScanMz5 = 6010;
            int testScanThermo = 6011;

            ScanSet testScanSet1 = new ScanSet(testScanMz5);
            ScanSet testScanSetThermo = new ScanSet(testScanThermo);

            mz5run.CurrentScanSet = testScanSet1;
            thermoRun.CurrentScanSet = testScanSetThermo;


            mz5run.GetMassSpectrum(testScanSet1);
            thermoRun.GetMassSpectrum(testScanSetThermo);


            Assert.AreEqual(mz5run.XYData.Xvalues.Length, thermoRun.XYData.Xvalues.Length);

            for (int i = 0; i < mz5run.XYData.Xvalues.Length; i++)
            {
                Assert.AreEqual(mz5run.XYData.Xvalues[i], thermoRun.XYData.Xvalues[i]);
            }

            DeconToolsPeakDetector peakDetector = new DeconToolsPeakDetector();
            peakDetector.PeakToBackgroundRatio = 3;
            peakDetector.SignalToNoiseThreshold = 2;




            peakDetector.Execute(mz5run.ResultCollection);

            //peakDetector.Execute(thermoRun.ResultCollection);


            TestUtilities.DisplayPeaks(mz5run.PeakList);

            Console.WriteLine();
            Console.WriteLine();

           // TestUtilities.DisplayPeaks(thermoRun.PeakList);


        }

        [Test]
        public void GetScanInfoTest1()
        {
            string testfile =
             @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mz5";

            Run run = new MzRun(testfile);

            int ms2scan = 6005;

            string info= run.GetScanInfo(ms2scan);

            Console.WriteLine(info);

        }

        [Test]
        public void Speedtest1()
        {
            //string testfile =
            //  @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzXML";

            string testfile2 =
             @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mz5";

			//string testfile3 =
			//@"\\protoapps\UserData\Slysz\DeconTools_TestFiles\mzXML\QC_Shew_08_04-pt5-2_11Jan09_Sphinx_08-11-18.mzML";

            Run run = new MzRun(testfile2);

            Stopwatch stopwatch = new Stopwatch();

            int startScan = 5500;
            int stopScan = 6500;

            ScanSet scanSet = new ScanSet(startScan);
            List<long> times = new List<long>();

            run.GetMSLevel(startScan);


            for (int i = startScan; i < stopScan; i++)
            {
                scanSet = new ScanSet(i);

                if (run.GetMSLevel(i) == 2) continue;
                

                stopwatch.Start();
                run.GetMassSpectrum(scanSet);
                stopwatch.Stop();

                
                times.Add(stopwatch.ElapsedMilliseconds);

                stopwatch.Reset();
            }


            //foreach (var time in times)
            //{
            //    Console.WriteLine(time);
            //}

            Console.WriteLine("Average = " + times.Average());






        }

        [Test]
        public void WatersMzXmlTest1()
        {
            string testfile =
                @"\\protoapps\UserData\Slysz\DeconTools_TestFiles\Waters\LC_MS_pHis_Caulo_meth_110207.mzXML";

            Run run = new RunFactory().CreateRun(testfile);
            Console.WriteLine(TestUtilities.DisplayRunInformation(run));

            run.ScanSetCollection = new ScanSetCollection();
            run.ScanSetCollection.Create(run, 500, 600, 1, 1);

            foreach (var scanSet in run.ScanSetCollection.ScanSetList)
            {
                int level =   run.GetMSLevel(scanSet.PrimaryScanNumber);

                Console.WriteLine(scanSet + "\t" + level);
            }

        }

    }
}
