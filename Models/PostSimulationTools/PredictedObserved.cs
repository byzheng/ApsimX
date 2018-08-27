﻿

namespace Models.PostSimulationTools
{
    using Models.Core;
    using Models.Factorial;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// # [Name]
    /// Reads the contents of a file (in apsim format) and stores into the DataStore.
    /// If the file has a column name of 'SimulationName' then this model will only input data for those rows
    /// where the data in column 'SimulationName' matches the name of the simulation under which
    /// this input model sits.
    /// If the file does NOT have a 'SimulationName' column then all data will be input.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(Folder))]
    public class PredictedObserved : Model, IPostSimulationTool
    {
        /// <summary>Gets or sets the name of the predicted table.</summary>
        [Description("Predicted table")]
        [Display(Type = DisplayType.TableName)]
        public string PredictedTableName { get; set; }

        /// <summary>Gets or sets the name of the observed table.</summary>
        [Description("Observed table")]
        [Display(Type = DisplayType.TableName)]
        public string ObservedTableName { get; set; }

        /// <summary>Gets or sets the field name used for match.</summary>
        [Description("Field name to use for matching predicted with observed data")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldNameUsedForMatch { get; set; }

        /// <summary>Gets or sets the second field name used for match.</summary>
        [Description("Second field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldName2UsedForMatch { get; set; }

        /// <summary>Gets or sets the third field name used for match.</summary>
        [Description("Third field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldName3UsedForMatch { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        /// <param name="dataStore">The data store.</param>
        /// <exception cref="ApsimXException">
        /// Could not find model data table:  + PredictedTableName
        /// or
        /// Could not find observed data table:  + ObservedTableName
        /// </exception>
        public void Run(IStorageReader dataStore)
        {
            if (PredictedTableName != null && ObservedTableName != null)
            {
                dataStore.DeleteDataInTable(this.Name);

                List<string> predictedDataNames = dataStore.GetTableColumns(PredictedTableName);
                List<string> observedDataNames = dataStore.GetTableColumns(ObservedTableName);

                if (predictedDataNames == null)
                    throw new ApsimXException(this, "Could not find model data table: " + PredictedTableName);

                if (observedDataNames == null)
                    throw new ApsimXException(this, "Could not find observed data table: " + ObservedTableName);

                // get the common columns between these lists of columns
                IEnumerable<string> commonCols = predictedDataNames.Intersect(observedDataNames);

                StringBuilder query = new StringBuilder("SELECT ");
                foreach (string s in commonCols)
                {
                    if (s == FieldNameUsedForMatch || s == FieldName2UsedForMatch || s == FieldName3UsedForMatch)
                        query.Append("I.'@field', ");
                    else
                        query.Append("I.'@field' AS 'Observed.@field', R.'@field' AS 'Predicted.@field', ");

                    query.Replace("@field", s);
                }

                query.Append("FROM " + ObservedTableName + " I INNER JOIN " + PredictedTableName + " R USING (SimulationID) WHERE I.'@match1' = R.'@match1'");
                if (FieldName2UsedForMatch != null && FieldName2UsedForMatch != string.Empty)
                    query.Append(" AND I.'@match2' = R.'@match2'");
                if (FieldName3UsedForMatch != null && FieldName3UsedForMatch != string.Empty)
                    query.Append(" AND I.'@match3' = R.'@match3'");

                int checkpointID = dataStore.GetCheckpointID("Current");
                query.Append(" AND R.CheckpointID = " + checkpointID);

                query.Replace(", FROM", " FROM"); // get rid of the last comma
                query.Replace("I.'SimulationID' AS 'Observed.SimulationID', R.'SimulationID' AS 'Predicted.SimulationID'", "I.'SimulationID' AS 'SimulationID'");

                query = query.Replace("@match1", FieldNameUsedForMatch);
                query = query.Replace("@match2", FieldName2UsedForMatch);
                query = query.Replace("@match3", FieldName3UsedForMatch);

                if (Parent is Folder)
                {
                    // Limit it to particular simulations in scope.
                    List<string> simulationNames = new List<string>();
                    foreach (Experiment experiment in Apsim.FindAll(this, typeof(Experiment)))
                        simulationNames.AddRange(experiment.GetSimulationNames());
                    foreach (Simulation simulation in Apsim.FindAll(this, typeof(Simulation)))
                        if (!(simulation.Parent is Experiment))
                            simulationNames.Add(simulation.Name);

                    query.Append(" AND I.SimulationID in (");
                    foreach (string simulationName in simulationNames)
                    {
                        if (simulationName != simulationNames[0])
                            query.Append(',');
                        query.Append(dataStore.GetSimulationID(simulationName));
                    }
                    query.Append(")");
                }

                DataTable predictedObservedData = dataStore.RunQuery(query.ToString());

                if (predictedObservedData != null)
                {
                    predictedObservedData.TableName = this.Name;
                    dataStore.WriteTable(predictedObservedData);

                    List<string> unitFieldNames = new List<string>();
                    List<string> unitNames = new List<string>();

                    // write units to table.
                    foreach (string fieldName in commonCols)
                    {
                        string units = dataStore.GetUnits(PredictedTableName, fieldName);
                        if (units != null && units != "()")
                        {
                            string unitsMinusBrackets = units.Replace("(", "").Replace(")", "");
                            unitFieldNames.Add("Predicted." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                            unitFieldNames.Add("Observed." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                        }
                    }
                    if (unitNames.Count > 0)
                        dataStore.AddUnitsForTable(Name, unitFieldNames, unitNames);
                }

                else
                {
                    // Determine what went wrong.
                    DataTable predictedData = dataStore.RunQuery("SELECT * FROM " + PredictedTableName);
                    DataTable observedData = dataStore.RunQuery("SELECT * FROM " + ObservedTableName);
                    if (predictedData == null || predictedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any predicted data.");
                    else if (observedData == null || observedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any observed data in node: " + ObservedTableName + ". Check for missing observed file or move " + ObservedTableName + " to top of child list under DataStore (order is important!)");
                    else
                        throw new Exception(Name + ": Observed data was found but didn't match the predicted values. Make sure the values in the SimulationName column match the simulation names in the user interface. Also ensure column names in the observed file match the APSIM report column names.");
                }
            }
        }
    }
}
