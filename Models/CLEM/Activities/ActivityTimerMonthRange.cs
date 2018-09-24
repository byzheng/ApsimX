﻿using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on monthly interval
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This activity timer defines a range between months upon which to perform activities.")]
    public class ActivityTimerMonthRange: CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [XmlIgnore]
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Notify CLEM that this Timer was performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Start month of annual period to perform activities
        /// </summary>
        [Description("Start month of annual period to perform activities (1-12)")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Month]
        public int StartMonth { get; set; }
        /// <summary>
        /// End month of annual period to perform activities
        /// </summary>
        [Description("End month of annual period to perform activities (1-12)")]
        [Required, Month]
        [System.ComponentModel.DefaultValueAttribute(12)]
        public int EndMonth { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerMonthRange()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Method to determine whether the activity is due
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                bool due = false;
                if (StartMonth < EndMonth)
                {
                    if ((Clock.Today.Month >= StartMonth) && (Clock.Today.Month <= EndMonth))
                    {
                        due = true;
                    }
                }
                else
                {
                    if ((Clock.Today.Month >= EndMonth) | (Clock.Today.Month <= StartMonth))
                    {
                        due = true;
                    }
                }
                if (due)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = this.Name
                        }
                    };
                    this.OnActivityPerformed(activitye);
                }
                return due;
            }
        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }
    }
}
