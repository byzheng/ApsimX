﻿// -----------------------------------------------------------------------
// <copyright file="ReportView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using Gtk;

    interface IReportActivityLedgerView
    {
        /// <summary>Provides access to the DataGrid.</summary>
        IDataStoreView DataStoreView { get; }

        /// <summary>Provides access to the DataGrid.</summary>
        IActivityLedgerGridView DisplayView { get; }
    }

    public class ReportActivityLedgerView : ViewBase, IReportActivityLedgerView
    {
        private Notebook notebook1 = null;
        private Alignment alignment1 = null;
        private Alignment alignment2 = null;

        private DataStoreView dataStoreView1;
        private ActivityLedgerGridView displayView1;

        /// <summary>Constructor</summary>
        public ReportActivityLedgerView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.ReportActivityLedgerView.glade");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            alignment1 = (Alignment)builder.GetObject("alignment1");
            alignment2 = (Alignment)builder.GetObject("alignment2");
            _mainWidget = notebook1;

            dataStoreView1 = new DataStoreView(this);
            alignment1.Add(dataStoreView1.MainWidget);

            displayView1 = new ActivityLedgerGridView(this);
            alignment2.Add(displayView1.MainWidget);

            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            dataStoreView1 = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>Provides access to the DataGrid.</summary>
        public IDataStoreView DataStoreView { get { return dataStoreView1; } }
        /// <summary>Provides access to the display Grid.</summary>
        public IActivityLedgerGridView DisplayView { get { return displayView1; } }
    }
}
