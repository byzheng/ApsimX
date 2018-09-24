﻿namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using Gtk;
    using EventArguments;
    using Intellisense;
    using System.Linq;

    class IntellisenseView
    {
        /// <summary>
        /// The popup window.
        /// </summary>
        private Window completionForm;

        /// <summary>
        /// The TreeView which displays the data.
        /// </summary>
        private Gtk.TreeView completionView;

        /// <summary>
        /// The ListStore which holds the data (suggested completion options).
        /// </summary>
        private ListStore completionModel;

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        private event EventHandler<IntellisenseItemSelectedArgs> onItemSelected;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        private event EventHandler<NeedContextItemsArgs> onContextItemsNeeded;

        /// <summary>
        /// Invoked when the intellisense popup loses focus.
        /// </summary>
        private event EventHandler onLoseFocus;

        /// <summary>
        /// Default constructor. Initialises intellisense popup, but doesn't display anything.
        /// </summary>
        public IntellisenseView()
        {
            completionForm = new Window(WindowType.Toplevel)
            {
                HeightRequest = 300,
                WidthRequest = 750,
                Decorated = false,
                SkipPagerHint = true,
                SkipTaskbarHint = true,
            };

            Frame completionFrame = new Frame();
            completionForm.Add(completionFrame);

            ScrolledWindow completionScroller = new ScrolledWindow();
            completionFrame.Add(completionScroller);

            completionModel = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            completionView = new Gtk.TreeView(completionModel);
            completionScroller.Add(completionView);

            TreeViewColumn column = new TreeViewColumn()
            {
                Title = "Item",
                Resizable = true,
            };
            CellRendererPixbuf iconRender = new CellRendererPixbuf();
            column.PackStart(iconRender, false);
            CellRendererText textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 25,
                Ellipsize = Pango.EllipsizeMode.End
            };

            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 0);
            column.SetAttributes(textRender, "text", 1);
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 10,
                Ellipsize = Pango.EllipsizeMode.End
            };
            column = new TreeViewColumn("Units", textRender, "text", 2)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
                WidthChars = 15,
                Ellipsize = Pango.EllipsizeMode.End
            };
            column = new TreeViewColumn("Type", textRender, "text", 3)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            textRender = new CellRendererText()
            {
                Editable = false,
            };
            column = new TreeViewColumn("Descr", textRender, "text", 4)
            {
                Resizable = true
            };
            completionView.AppendColumn(column);

            completionView.HasTooltip = true;
            completionView.TooltipColumn = 5;
            completionForm.FocusOutEvent += OnLeaveCompletion;
            completionView.ButtonPressEvent += OnButtonPress;
            completionView.KeyPressEvent += OnContextListKeyDown;
            completionView.KeyReleaseEvent += OnKeyRelease;
        }

        /// <summary>
        /// Invoked when the user selects an item (via enter or double click).
        /// </summary>
        public event EventHandler<IntellisenseItemSelectedArgs> ItemSelected
        {
            add
            {
                DetachHandlers(ref onItemSelected);
                onItemSelected += value;
            }
            remove
            {
                onItemSelected -= value;
            }
        }

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded
        {
            add
            {
                if (onContextItemsNeeded == null)
                    onContextItemsNeeded += value;
            }
            remove
            {
                onContextItemsNeeded -= value;
            }
        }

        /// <summary>
        /// Fired when the intellisense window loses focus.
        /// </summary>
        public event EventHandler LoseFocus
        {
            add
            {
                if (onLoseFocus == null)
                {
                    onLoseFocus += value;
                }
            }
            remove
            {
                onLoseFocus -= value;
            }
        }

        /// <summary>
        /// Returns true if the intellisense is visible. False otherwise.
        /// </summary>
        public bool Visible { get { return completionForm.Visible; } }

        /// <summary>
        /// Editor being used. This is mainly needed to get a reference to the top level window.
        /// </summary>
        public ViewBase Editor { get; set; }
        
        /// <summary>
        /// Main window reference.
        /// </summary>
        public Window MainWindow { get; set; }

        /// <summary>
        /// Gets the Main/Parent window for the intellisense popup.
        /// </summary>
        private Window GetMainWindow()
        {
            return MainWindow ?? Editor?.MainWidget.Toplevel as Window;
        }

        /// <summary>
        /// Displays the intellisense popup at the specified coordinates. Returns true if the 
        /// popup is successfully generated (e.g. if it finds some auto-completion options). 
        /// Returns false otherwise.        
        /// </summary>
        /// <param name="x">Horizontal coordinate</param>
        /// <param name="y">Vertical coordinate</param>        
        private bool showAtCoordinates(int x, int y)
        {            
            // only display the list if there are options to display
            if (completionModel.IterNChildren() > 0)
            {
                completionForm.TransientFor = GetMainWindow();
                completionForm.ShowAll();
                completionForm.Move(x, y);
                completionForm.Resize(completionForm.WidthRequest, completionForm.HeightRequest);
                completionView.SetCursor(new TreePath("0"), null, false);
                //if (completionForm.GdkWindow != null)
                //    completionForm.GdkWindow.Focus(0);
                completionView.Columns[2].FixedWidth = completionView.WidthRequest / 10;
                while (GLib.MainContext.Iteration()) ;
                return true;
            }
            return false;
        }

        public void SelectItem(int index)
        {
            TreeIter iter;
            if (completionModel.GetIter(out iter, new TreePath(index.ToString())))
            {
                completionView.Selection.SelectIter(iter);
                completionView.SetCursor(new TreePath(index.ToString()), null, false);
            }
        }
        /// <summary>
        /// Tries to display the intellisense popup at the specified coordinates. If the coordinates are
        /// too close to the right or bottom of the screen, they will be adjusted appropriately.
        /// Returns true if the popup is successfully generated (e.g. if it finds some auto-completion options).
        /// Returns false otherwise.
        /// </summary>
        /// <param name="x">Horizontal coordinate</param>
        /// <param name="y">Vertical coordinate</param>
        /// <param name="lineHeight">Font height</param>
        /// <returns></returns>
        public bool SmartShowAtCoordinates(int x, int y, int lineHeight = 17)
        {
            // By default, we use the given coordinates as the top-left hand corner of the popup.
            // If the popup is too close to the right of the screen, we use the x-coordinate as 
            // the right hand side of the popup instead.
            // If the popup is too close to the bottom of the screen, we use the y-coordinate as
            // the bottom side of the popup instead.
            int xres = GetMainWindow().Screen.Width;
            int yres = GetMainWindow().Screen.Height;

            if ((x + completionForm.WidthRequest) > xres)            
                // We are very close to the right-hand side of the screen
                x -= completionForm.WidthRequest;            
            
            if ((y + completionForm.HeightRequest) > yres)
                // We are very close to the bottom of the screen
                // Move the popup one line higher as well, to room allow for the input box in the popup.
                y -= completionForm.HeightRequest + lineHeight;

            return showAtCoordinates(Math.Max(0, x), Math.Max(0, y));
        }

        /// <summary>
        /// Generates a list of auto-completion options.
        /// </summary>
        /// <returns></returns>
        public bool GenerateAutoCompletionOptions(string node)
        {
            // generate list of intellisense options
            List<string> items = new List<string>();
            List<NeedContextItemsArgs.ContextItem> allItems = new List<NeedContextItemsArgs.ContextItem>();
            onContextItemsNeeded?.Invoke(this, new NeedContextItemsArgs() { ObjectName = node, Items = items, AllItems = allItems });

            if (allItems.Count < 1)
                return false;

            Populate(allItems);
            return true;
        }

        /// <summary>
        /// Populates the completion window with data.
        /// </summary>
        /// <param name="items">List of completion data.</param>
        public void Populate(List<CompletionData> items)
        {
            completionModel.Clear();
            foreach (CompletionData item in items)
            {
                IEnumerable<string> descriptionLines = item.Description?.Split(Environment.NewLine.ToCharArray()).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Take(2);
                string description = descriptionLines.Count() < 2 ? descriptionLines.FirstOrDefault() : descriptionLines.Aggregate((x, y) => x + Environment.NewLine + y);
                completionModel.AppendValues(item.Image, item.DisplayText, item.Units, item.ReturnType, description, item.CompletionText);
            }
        }

        /// <summary>
        /// Populates the completion window with data.
        /// </summary>
        /// <param name="items">List of completion data.</param>
        public void Populate(List<NeedContextItemsArgs.ContextItem> items)
        {
            completionModel.Clear();

            Gdk.Pixbuf functionPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Function.png", 16, 16);
            Gdk.Pixbuf propertyPixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Property.png", 16, 16);
            Gdk.Pixbuf pixbufToBeUsed;

            foreach (NeedContextItemsArgs.ContextItem item in items)
            {
                pixbufToBeUsed = item.IsProperty ? propertyPixbuf : functionPixbuf;
                completionModel.AppendValues(pixbufToBeUsed, item.Name, item.Units, item.TypeName, item.Descr, item.ParamString);
            }
        }

        /// <summary>
        /// Safely disposes of several objects.
        /// </summary>
        public void Cleanup()
        {
            completionForm.FocusOutEvent -= OnLeaveCompletion;
            completionView.ButtonPressEvent -= OnButtonPress;
            completionView.KeyPressEvent -= OnContextListKeyDown;
            completionView.KeyReleaseEvent -= OnKeyRelease;

            if (completionForm.IsRealized)
                completionForm.Destroy();
            completionView.Dispose();
            completionForm.Destroy();
            completionForm = null;

            // Detach event handlers so that this object may be safely garbage collected.
            foreach (EventHandler<IntellisenseItemSelectedArgs> handler in onItemSelected?.GetInvocationList())
            {
                onItemSelected -= handler;
            }
        }

        /// <summary>
        /// Detaches all event handlers from an event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        private static void DetachHandlers<T>(ref EventHandler<T> e)
        {
            if (e == null)
                return;
            foreach (EventHandler<T> handler in e?.GetInvocationList())
                e -= handler;
        }

        /// <summary>
        /// Gets the currently selected item.
        /// </summary>
        /// <exception cref="Exception">Exception is thrown if no item is selected.</exception>
        /// <returns></returns>
        private string GetSelectedItem()
        {
            TreeViewColumn col;
            TreePath path;
            completionView.GetCursor(out path, out col);
            if (path != null)
            {
                TreeIter iter;
                completionModel.GetIter(out iter, path);
                return (string)completionModel.GetValue(iter, 1);
            }
            throw new Exception("Unable to get selected intellisense item: no item is selected.");
        }

        /// <summary>
        /// Focus out event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnLeaveCompletion(object sender, FocusOutEventArgs e)
        {
            completionForm.Hide();
            onLoseFocus?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// (Mouse) button press event handler. If it is a left mouse double click, consumes 
        /// the ItemSelected event.
        /// </summary>
        /// <param name="o">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnButtonPress(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1)
            {
                completionForm.Hide();
                onItemSelected?.Invoke(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });                
            }
        }

        /// <summary>
        /// Key down event handler. If the key is enter, consumes the ItemSelected event.
        /// If the key is escape, hides the intellisense.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnContextListKeyDown(object sender, KeyPressEventArgs e)
        {
            // If user clicks ENTER and the context list is visible then insert the currently
            // selected item from the list into the TextBox and close the list.
            if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
            {
                completionForm.Hide();
                onItemSelected?.Invoke(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });
            }

            // If the user presses ESC and the context list is visible then close the list.
            else if (e.Event.Key == Gdk.Key.Escape && completionView.Visible)
            {
                completionForm.Hide();
            }
        }

        /// <summary>
        /// Key release event handler. If the key is enter, consumes the ItemSelected event.
        /// </summary>
        /// <param name="o">Sender</param>
        /// <param name="args">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnKeyRelease(object o, KeyReleaseEventArgs e)
        {            
            if (e.Event.Key == Gdk.Key.Return && completionForm.Visible)
            {
                completionForm.Hide();
                onItemSelected?.Invoke(this, new IntellisenseItemSelectedArgs { ItemSelected = GetSelectedItem() });
                while (GLib.MainContext.Iteration()) ;
            }                
        }
    }
}
