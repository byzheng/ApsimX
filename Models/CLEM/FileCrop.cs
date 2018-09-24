using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using System.ComponentModel.DataAnnotations;

// -----------------------------------------------------------------------
// <copyright file="FileCrop.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.CLEM
{

    ///<summary>
    /// Reads in crop growth data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.CLEMFileCropView")]
    [PresenterName("UserInterface.Presenters.CLEMFileCropPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    [Description("This model holds a crop data file for the CLEM simulation.")]
    public class FileCrop : Model
    {
        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// The character spacing index for the SoilNum column
        /// </summary>
        private int soilNumIndex;

        /// <summary>
        /// The character spacing index for the CropName column
        /// </summary>
        private int cropNameIndex;

        /// <summary>
        /// The character spacing index for the Year column
        /// </summary>
        private int yearIndex;

        /// <summary>
        /// The character spacing index for the Month column
        /// </summary>
        private int monthIndex;

        /// <summary>
        /// The character spacing index for the AmtKg column
        /// </summary>
        private int AmtKgIndex;

        /// <summary>
        /// The character spacing index for the Npct column
        /// </summary>
        private int NpctIndex;

        /// <summary>
        /// The entire Crop File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable ForageFileAsTable;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Crop file name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Crop file name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                else
                    return this.FileName;
            }
            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    this.FileName = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.FileName = value;
            }
        }

        /// <summary>
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        public string ExcelWorkSheetName { get; set; }





        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (!File.Exists(FullFileName))
            {
                string errorMsg = String.Format("Could not locate file ({0}) for ({1})", FullFileName, this.Name);
                throw new ApsimXException(this, errorMsg);
            }

            //this.doSeek = true;
            this.soilNumIndex = 0;
            this.cropNameIndex = 0;
            this.yearIndex = 0;
            this.monthIndex = 0;
            this.AmtKgIndex = 0;
            this.NpctIndex = 0;


            this.ForageFileAsTable = GetAllData();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (this.reader != null)
            {
                this.reader.Close();
                this.reader = null;
            }

        }



        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// Used by the UserInterface to give a warning of what is wrong
        /// 
        /// When the user selects a file using the browse button in the UserInterface 
        /// and the file can not be displayed for some reason in the UserInterface.
        /// </summary>
        [XmlIgnore]
        public string ErrorMessage = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            try
            {
                return GetAllData();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                return null;
            }
        }



        /// <summary>
        /// Get the DataTable view of this data
        /// </summary>
        /// <returns>The DataTable</returns>
        public DataTable GetAllData()
        {
            this.reader = null;


            if (this.OpenDataFile())
            {
                List<string> cropProps = new List<string>();
                cropProps.Add("SoilNum");
                cropProps.Add("CropName");
                cropProps.Add("Year");
                cropProps.Add("Month");
                cropProps.Add("AmtKg");
                //Npct column is optional 
                //Only try to read it in if it exists in the file.
                if (NpctIndex != -1)
                    cropProps.Add("Npct");


                DataTable table = this.reader.ToTable(cropProps);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[0] = table.Columns["SoilNum"];
                primarykeys[1] = table.Columns["CropName"];
                primarykeys[2] = table.Columns["Year"];
                primarykeys[3] = table.Columns["Month"]; 

                table.PrimaryKey = primarykeys;

                CloseDataFile();

                return table;
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// Searches the DataTable created from the Forage File using the specified parameters.
        /// <returns></returns>
        /// </summary>
        /// <param name="SoilNum"></param>
        /// <param name="CropName"></param>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <returns>A struct called CropDataType containing the crop data for this month.
        /// This struct can be null. 
        /// </returns>
        public List<CropDataType> GetCropDataForEntireRun(int SoilNum, string CropName,
                                        DateTime StartDate, DateTime EndDate)
        {
            int startYear = StartDate.Year;
            int startMonth = StartDate.Month;
            int endYear = EndDate.Year;
            int endMonth = EndDate.Month;

            //http://www.csharp-examples.net/dataview-rowfilter/

            string filter = "(SoilNum = " + SoilNum + ") AND (CropName = " +  "'" + CropName + "'" + ")"
                + " AND (" 
                +      "( Year = " + startYear + " AND Month >= " + startMonth + ")" 
                + " OR  ( Year > " + startYear + " AND Year < " + endYear +")"
                + " OR  ( Year = " + endYear + " AND Month <= " + endMonth + ")"
                +      ")";


            DataRow[] foundRows = this.ForageFileAsTable.Select(filter);

            List<CropDataType> filtered = new List<CropDataType>(); 

            foreach (DataRow dr in foundRows)
            {
                filtered.Add(DataRow2CropData(dr));
            }

            filtered.Sort( (r,s) => DateTime.Compare(r.HarvestDate, s.HarvestDate) );

            return filtered;
        }



        private CropDataType DataRow2CropData(DataRow dr)
        {
            CropDataType cropdata = new CropDataType();

            cropdata.SoilNum = int.Parse(dr["SoilNum"].ToString());
            cropdata.CropName = dr["CropName"].ToString();
            cropdata.Year = int.Parse(dr["Year"].ToString());
            cropdata.Month = int.Parse(dr["Month"].ToString());

            cropdata.AmtKg = double.Parse(dr["AmtKg"].ToString());

            //Npct column is optional 
            //Only try to read it in if it exists in the file.
            if (NpctIndex != -1)
                cropdata.Npct = double.Parse(dr["Npct"].ToString());
            else
                cropdata.Npct = double.NaN;


            cropdata.HarvestDate = new DateTime(cropdata.Year, cropdata.Month, 1);

            return cropdata;
        }




        /// <summary>
        /// Open the forage data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (System.IO.File.Exists(this.FullFileName))
            {
                if (this.reader == null)
                {
                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FullFileName, this.ExcelWorkSheetName);

                    this.soilNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "SoilNum");
                    this.cropNameIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "CropName");
                    this.yearIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Year");
                    this.monthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Month");
                    this.AmtKgIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "AmtKg");
                    this.NpctIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Npct");


                    if (this.soilNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("SoilNum") == null)
                            throw new Exception("Cannot find SoilNum column in crop file: " + this.FullFileName);
                    }

                    if (this.cropNameIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("CropName") == null)
                            throw new Exception("Cannot find CropName column in crop file: " + this.FullFileName);
                    }

                    if (this.yearIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Year") == null)
                            throw new Exception("Cannot find Year column in crop file: " + this.FullFileName);
                    }

                    if (this.monthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Month") == null)
                            throw new Exception("Cannot find Month column in crop file: " + this.FullFileName);
                    }

                    if (this.AmtKgIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("AmtKg") == null)
                            throw new Exception("Cannot find AmtKg column in crop file: " + this.FullFileName);
                    }

                    //Npct is an optional column. You don't need to provide it. So don't throw an error.
                    //if (this.NpctIndex == -1)
                    //{
                    //    if (this.reader == null || this.reader.Constant("Npct") == null)
                    //        throw new Exception("Cannot find Npct column in crop file: " + this.FullFileName);
                    //}


                }
                else
                {
                    if (this.reader.IsExcelFile != true)
                        this.reader.SeekToDate(this.reader.FirstDate);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Close the datafile.</summary>
        public void CloseDataFile()
        {
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }
        }


    }



    /// <summary>
    /// A structure containing the commonly used weather data.
    /// </summary>
    [Serializable]
    public class CropDataType
    {

        /// <summary>
        /// Soil Number
        /// </summary>
        public int SoilNum;

        /// <summary>
        /// Name of Crop
        /// </summary>
        public string CropName;

        /// <summary>
        /// Year (eg. 2017)
        /// </summary>
        public int Year;

        /// <summary>
        /// Month (eg. 1 is Jan, 2 is Feb)
        /// </summary>
        public int Month;

        /// <summary>
        /// Amount in Kg (perHa or perTree) 
        /// </summary>
        public double AmtKg;

        /// <summary>
        /// Nitrogen Percentage of the Amount
        /// </summary>
        public double Npct;

        /// <summary>
        /// Combine Year and Month to create a DateTime. 
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime HarvestDate;
    }



}