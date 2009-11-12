﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeconTools.Backend.Core
{
    
    /// <summary>
    /// Singleton class that functions to allow only one UIMF file to be opened at a time
    /// and keeps the file open allowing for more efficient use of the UIMFLibrary class. 
    /// (instead of opening and closing it everytime you want to retrieve information from
    /// the class)
    /// </summary>
    public class UIMFLibraryAdapter
    {
        private static UIMFLibraryAdapter instance;
        private string fileName;

        private UIMFLibraryAdapter(string filename)
        {
            this.fileName = filename;
            datareader = new UIMFLibrary.DataReader();
            datareader.OpenUIMF(this.fileName);
        }


        UIMFLibrary.DataReader datareader;

        public UIMFLibrary.DataReader Datareader
        {
            get { return datareader; }
            set { datareader = value; }
        }

        
        public static UIMFLibraryAdapter getInstance(string filename)
        {
            if (instance == null)
            {
                instance = new UIMFLibraryAdapter(filename);
            }
            else
            {
                if (filename != instance.fileName)     // method's caller is trying to open a file different from that already opened
                {
                    instance.CloseCurrentUIMF();      //so close the one already opened
                    instance = null;
                    getInstance(filename);            //re-instantiate using provided filename  
                }
                else     // method's caller is requesting the same filename that is already open
                {
                         // don't need to do anything but return the instance (below)
                }
            }
            
            return instance;
        }

        public void CloseCurrentUIMF()
        {
            if (instance != null)
            {
                datareader.CloseUIMF(instance.fileName);
            }
        }






        internal static string getLibraryVersion()
        {
            return DeconTools.Backend.Utilities.AssemblyInfoRetriever.GetVersion(typeof(UIMFLibrary.DataReader));
        }
    }
}
