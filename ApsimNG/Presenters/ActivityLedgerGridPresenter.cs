﻿// -----------------------------------------------------------------------
// <copyright file="ActivityLedgerGridPresenter.cs" company="CSIRO CLEM">
//     Copyright (c) CSIRO CLEM based  upon GridePresenter APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Data;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Factorial;
    using Views;
    using global::UserInterface.Interfaces;
    using System.Collections.Generic;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class ActivityLedgerGridPresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IStorageReader dataStore;

        /// <summary>The display grid store view to work with.</summary>
        public IActivityLedgerGridView Grid { get; set; }

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        public string ModelName { get; set; }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as IStorageReader;
            this.Grid = view as ActivityLedgerGridView;
            this.explorerPresenter = explorerPresenter;
            this.Grid.ReadOnly = true;
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            using (DataTable data = GetData())
            {
                if (data != null)
                {
                    // get unique rows
                    List<string> activities = data.AsEnumerable().Select(a => a.Field<string>("Name")).Distinct().ToList<string>();
                    // get unique columns
                    List<DateTime> dates = data.AsEnumerable().Select(a => a.Field<DateTime>("Date")).Distinct().ToList<DateTime>();
                    // create table

                    DataTable tbl = new DataTable();
                    tbl.Columns.Add("Activity");
                    foreach (var item in dates)
                    {
                        tbl.Columns.Add(item.Month.ToString("00") + "\n" + item.ToString("yy"));
                    }
                    foreach (var item in activities)
                    {
                        if (item != "TimeStep")
                        {
                            DataRow dr = tbl.NewRow();
                            dr["Activity"] = item;

                            foreach (var activityTick in data.AsEnumerable().Where(a => a.Field<string>("Name") == item))
                            {
                                DateTime dte = (DateTime)activityTick["Date"];
                                string status = activityTick["Status"].ToString();
                                dr[dte.Month.ToString("00") + "\n" + dte.ToString("yy")] = status;
                            }
                            tbl.Rows.Add(dr);
                        }
                    }
                    this.Grid.DataSource = tbl;
                    this.Grid.LockLeftMostColumns(1);  // lock simulation name, zone, date.
                }
            }
        }

        /// <summary>Get data to show in grid.</summary>
        /// <returns>A data table of all data.</returns>
        private DataTable GetData()
        {
            DataTable data = null;
            if (dataStore != null)
            {
                try
                {
                    int count = Utility.Configuration.Settings.MaximumRowsOnReportGrid;
                    data = dataStore.GetData(
                                            tableName: ModelName,
                                            count: Utility.Configuration.Settings.MaximumRowsOnReportGrid);

                }
                catch (Exception e)
                {
                    this.explorerPresenter.MainPresenter.ShowError(e);
                }
            }
            else
            {
                data = new DataTable();
            }

            return data;
        }

        /// <summary>The selected table has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        /// <summary>The column filter has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnFilterChanged(object sender, EventArgs e)
        {
            PopulateGrid();
        }
    }
}
