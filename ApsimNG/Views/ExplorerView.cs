﻿// -----------------------------------------------------------------------
// <copyright file="ExplorerView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

// The basics are all here, but there are still a few things to be implemented:
// Drag and drop is pinning an object so we can pass its address around as data. Is there a better way?
// (Probably not really, as we go through a native layer, unless we can get by with the serialized XML).
// Shortcuts (accelerators in Gtk terminology) haven't yet been implemented.
// Link doesn't work, but it appears that move and link aren't working in the Windows.Forms implementation either.
// Actually, Move "works" here but doesn't undo correctly

namespace UserInterface.Views
{
    using Gtk;
    using Interfaces;
    using System;
    
    /// <summary>
    /// An ExplorerView is a "Windows Explorer" like control that displays a virtual tree control on the left
    /// and a user interface on the right allowing the user to modify properties of whatever they
    /// click on in the tree control.
    /// </summary>
    public class ExplorerView : ViewBase, IExplorerView
    {
        private Viewport RightHandView;
        private MenuView popup;
        private Gtk.TreeView treeviewWidget;

        /// <summary>Default constructor for ExplorerView</summary>
        public ExplorerView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.ExplorerView.glade");
            _mainWidget = (VBox)builder.GetObject("vbox1");
            ToolStrip = new ToolStripView((Toolbar)builder.GetObject("toolStrip"));

            treeviewWidget = (Gtk.TreeView)builder.GetObject("treeview1");
            Tree = new TreeView(owner, treeviewWidget);
            popup = new MenuView();
            RightHandView = (Viewport)builder.GetObject("RightHandView");
            RightHandView.ShadowType = ShadowType.EtchedOut;
            _mainWidget.Destroyed += OnDestroyed;
        }

        /// <summary>The tree on the left side of the explorer view</summary>
        public ITreeView Tree { get; private set; }

        /// <summary>The toolstrip at the top of the explorer view</summary>
        public IToolStripView ToolStrip { get; private set; }

        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddRightHandView(object control)
        {
            foreach (Widget child in RightHandView.Children)
            {
                RightHandView.Remove(child);
                child.Destroy();
            }
            ViewBase view = control as ViewBase;
            if (view != null)
            {
                RightHandView.Add(view.MainWidget);
                RightHandView.ShowAll();
            }
        }

        /// <summary>Get screenshot of right hand panel.</summary>
        public System.Drawing.Image GetScreenshotOfRightHandPanel()
        {
            // Create a Bitmap and draw the panel
            int width;
            int height;
            Gdk.Window panelWindow = RightHandView.Child.GdkWindow;
            panelWindow.GetSize(out width, out height);
            Gdk.Pixbuf screenshot = Gdk.Pixbuf.FromDrawable(panelWindow, panelWindow.Colormap, 0, 0, 0, 0, width, height);
            byte[] buffer = screenshot.SaveToBuffer("png");
            System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stream);
            return bitmap;
        }
        
        /// <summary>
        /// Widget has been destroyed - clean up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            if (RightHandView != null)
            {
                foreach (Widget child in RightHandView.Children)
                {
                    RightHandView.Remove(child);
                    child.Destroy();
                }
            }
            ToolStrip.Destroy();
            popup.Destroy();
            _mainWidget.Destroyed -= OnDestroyed;
            _owner = null;
        }
    }
}
