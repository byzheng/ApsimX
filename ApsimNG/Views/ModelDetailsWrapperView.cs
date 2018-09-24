﻿// -----------------------------------------------------------------------
// <copyright file="IExplorerView.cs" company="CSIRO">
//     Copyright (c) CSIRO, CLEM model
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using EventArguments;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// This provides a wrapper view to display model type, description adnd help link
    /// These are taken from the namespace and Description Attribute
    /// The Explorer presenter will use this wrapper if a Description attributre is present.
    /// </summary>
    public class ModelDetailsWrapperView : ViewBase, IModelDetailsWrapperView
    {
        private VBox vbox1 = null;
        private Label modelTypeLabel = null;
        private Label modelDescriptionLabel = null;
        private LinkButton modelHelpLinkLabel = null;
        private Viewport bottomView = null;

        public ModelDetailsWrapperView(ViewBase owner) : base(owner)
        {
            vbox1 = new VBox();

            modelTypeLabel = new Label
            {
                Xalign = 0.0f,
                Xpad = 3
            };
            Pango.FontDescription font = new Pango.FontDescription
            {
                Size = Convert.ToInt32(16 * Pango.Scale.PangoScale),
                Weight = Pango.Weight.Semibold
            };
            modelTypeLabel.ModifyFont(font);

            modelDescriptionLabel = new Label()
            {
                Xalign = 0.0f,
                Xpad=4
            };
            modelDescriptionLabel.LineWrapMode = Pango.WrapMode.Word;
            modelDescriptionLabel.Wrap = true;
            modelDescriptionLabel.ModifyBg(StateType.Normal, new Gdk.Color(131, 0, 131));

            modelHelpLinkLabel = new LinkButton("", "more information")
            {
                Xalign = 0.0f,
            };
            modelHelpLinkLabel.Clicked += ModelHelpLinkLabel_Clicked;
            modelHelpLinkLabel.ModifyBase(StateType.Normal, new Gdk.Color(131, 0, 131));
            modelHelpLinkLabel.Visible = false;

            bottomView = new Viewport
            {
                ShadowType = ShadowType.None
            };

            vbox1.PackStart(modelTypeLabel, false, true, 0);
            vbox1.PackStart(modelDescriptionLabel, false, true, 0);
            vbox1.PackStart(modelHelpLinkLabel, false, true, 0);
            vbox1.Add(bottomView);
            vbox1.SizeAllocated += Vbox1_SizeAllocated;


            _mainWidget = vbox1;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// We want to wrap the description text within a space that uses all the available width,
        /// but Gtk doesn't make that easy. Here we respond to changes in the size of the enclosing VBox
        /// and adjust the width of the description label accordingly. We use a bit less than the full width
        /// so that Windows can still be reduced in size. See http://blog.borovsak.si/2009/05/wrapping-adn-resizing-gtklabel.html
        /// </summary>
        private void Vbox1_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            // modelTypeLabel.WidthRequest = args.Allocation.Width - 8;
            modelDescriptionLabel.WidthRequest = args.Allocation.Width - 8;
            // modelHelpLinkLabel.WidthRequest = args.Allocation.Width - 8;
        }

        private void ModelHelpLinkLabel_Clicked(object sender, EventArgs e)
        {
            //TODO: check internet connection and choose either local or remote help files
            if(ModelHelpURL != "")
                System.Diagnostics.Process.Start(ModelHelpURL);
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            modelHelpLinkLabel.Clicked -= ModelHelpLinkLabel_Clicked;
            vbox1.SizeAllocated -= Vbox1_SizeAllocated;
            if (bottomView != null)
            {
                foreach (Widget child in bottomView.Children)
                {
                    bottomView.Remove(child);
                    child.Destroy();
                }
            }
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        public string ModelTypeText
        {
            get { return modelTypeLabel.Text; }
            set { modelTypeLabel.Text = value; }
        }

        public string ModelDescriptionText
        {
            get { return modelDescriptionLabel.Text; }
            set { modelDescriptionLabel.Markup = value; }
        }

        public string ModelHelpURL
        {
            get { return modelHelpLinkLabel.Uri; }
            set
            {
                modelHelpLinkLabel.Uri = value;
                modelHelpLinkLabel.Visible = (value.ToString() != "");
                modelDescriptionLabel.Ypad = (value.ToString() != "") ? 0 : 4;
            }
        }

        public string ModelTypeTextColour
        {
            get { return "N/A"; }
            set
            {
                if (value.Length == 6)
                {
                    byte r = Convert.ToByte(value.Substring(0, 2), 16);
                    byte g = Convert.ToByte(value.Substring(2, 2), 16);
                    byte b = Convert.ToByte(value.Substring(4, 2), 16);
                    modelTypeLabel.ModifyFg(StateType.Normal, new Gdk.Color(r, g, b));
                }
            }
        }

        public void AddLowerView(object Control)
        {
            foreach (Widget child in bottomView.Children)
            {
                bottomView.Remove(child);
                child.Destroy();
            }
            ViewBase view = Control as ViewBase;
            if (view != null)
            {
                bottomView.Add(view.MainWidget);
                bottomView.ShowAll();
            }
        }
    }
}
