﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using Models.Agroforestry;

namespace Models.Zones
{
    /// <summary>A strip crop zone.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(AgroforestrySystem))]
    [ScopedModel]
    public class StripCropZone : Zone
    {
        /// <summary>Length of the zone.</summary>
        /// <value>The length.</value>
        [Description("Length of zone (m)")]
        public double Length { get; set; }

        /// <summary>Width of the zone.</summary>
        /// <value>The width.</value>
        [Description("Width of zone (m)")]
        public double Width { get; set; }

        /// <summary>
        /// Return the area of the zone.
        /// </summary>
        [XmlIgnore]
        public override double Area
        {
            get
            {
                return Length * Width / 10000;
            }
            set
            {
            }
        }
    }
}
