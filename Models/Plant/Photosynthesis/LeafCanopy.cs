using System;
using System.Linq;


namespace Models.PMF.Phenology
{
    public class LeafCanopy
    {
        public PathwayParameters CPath;

        public PathwayParameters C3 { get; set; }
        public PathwayParameters C4 { get; set; }

        public delegate void Notifier();
        //[System.Xml.Serialization.XmlIgnore]
        public Notifier notifyChanged;

        public delegate void LayerNumberChangeNotifier(int n);
        //[System.Xml.Serialization.XmlIgnore]
        public LayerNumberChangeNotifier layerNumberChanged;

        protected double _LAI = 5;
        [ModelPar("IlH1M", "Total LAI of the plant", "L", "c", "m2/m2", "", "m2 leaf / m2 ground")]
        public double LAI
        {
            get
            {
                return _LAI;
            }
            set
            {
                _LAI = value;

                calcLAILayers();
            }
        }

        protected int _nLayers;
        [ModelPar("umPBy", "Number of layers in canopy", "ln", "", "")]
        public int nLayers
        {
            get
            {
                return _nLayers;
            }
            set
            {
                _nLayers = value;
                initArrays();
                if (layerNumberChanged != null)
                {
                    layerNumberChanged(_nLayers);
                }
            }
        }

        [ModelPar("yVwlh", "Number of layers in canopy", "ln", "", "")]
        public double nLayersD
        {
            get
            {
                return _nLayers;
            }
            set
            {
                _nLayers = (int)value;
                initArrays();

                if (layerNumberChanged != null)
                {
                    layerNumberChanged(_nLayers);
                }
            }
        }

        protected double _leafAngle;
        [ModelPar("fV3qT", "Leaf angle (elevation)", "β", "", "°")]
        public double leafAngle
        {
            get
            {
                return _leafAngle;
            }
            set
            {
                _leafAngle = value;

                for (int i = 0; i < _nLayers; i++)
                {
                    leafAngles[i] = new Angle(value, AngleType.Deg);
                }
            }
        }

        [ModelVar("xt5rv", "Leaf angle (elevation) of layer(l)", "β", "", "°", "l")]
        public Angle[] leafAngles { get; set; }

        [ModelVar("6kWOL", "Leaf angle (elevation) of layer(l)", "β", "", "°", "l")]
        public double[] leafAnglesD
        {
            get
            {
                double[] vals = new double[nLayers];
                for (int i = 0; i < nLayers; i++)
                {
                    vals[i] = leafAngles[i].deg;
                }
                return vals;
            }
        }

        [ModelVar("dW3lC", "LAI of layer(l)", "LAI", "", "m2/m2", "l", "m2 leaf / m2 ground")]
        public double[] LAIs { get; set; }

        [ModelVar("GPoT1", "Total leaf nitrogen", "N", "c", "mmol N/m2", "", "")]
        public double totalLeafNitrogen { get; set; }

        [ModelVar("7VTsD", "Accumulated LAI in each layer", "LAIc(l)", "", "")]
        public double[] LAIAccums { get; set; }

        [ModelVar("ryt1m", "Beam Penetration", "fsun(l)", "", "")]
        public double[] beamPenetrations { get; set; }

        [ModelVar("202s3", "Sunlit LAI (integrated fun(l))", "LAISun(L)", "", "m2 leaf m-2 ground")]
        public double[] sunlitLAIs { get; set; }

        [ModelVar("gesEM", "Shaded LAI", "LAISh(l)", "", "m2 leaf m-2 ground")]
        public double[] shadedLAIs { get; set; }

        [ModelVar("XeX9e", "Shadow projection coefficient", "G", "", "", "l,t")]
        public double[] shadowProjectionCoeffs { get; set; }

        [ModelVar("yZD0V", "Radiation extinction coefficient of canopy", "kb", "", "", "l,t")]
        public double[] beamExtCoeffs { get; set; }

        [ModelVar("BglXi", "Direct and scattered-direct PAR extinction co-efficient", "kb'", "", "", "l,t")]
        public double[] beamScatteredBeams { get; set; }

        [ModelVar("yZD0V", "Radiation extinction coefficient of canopy", "kb", "", "", "l,t")]
        public double[] beamExtCoeffsNIR { get; set; }

        [ModelVar("BglXi", "Direct and scattered-direct PAR extinction co-efficient", "kb'", "", "", "l,t")]
        public double[] beamScatteredBeamsNIR { get; set; }

        [ModelVar("FZIMS", "Beam and scattered beam", "kb'", "", "")]
        public double kb_
        {
            get
            {
                if (beamScatteredBeams.Length > 0)
                {
                    return beamScatteredBeams[0];
                }
                else
                {
                    return 0;
                }
            }
        }

        [ModelVar("FZIMS", "Beam and scattered beam", "kb'", "", "")]
        public double kb_NIR
        {
            get
            {
                if (beamScatteredBeamsNIR.Length > 0)
                {
                    return beamScatteredBeamsNIR[0];
                }
                else
                {
                    return 0;
                }
            }
        }

        [ModelVar("tp8wi", "Beam radiation extinction coefficient of canopy", "kb", "", "")]
        public double kb
        {
            get
            {
                if (beamExtCoeffs.Length > 0)
                {
                    return beamExtCoeffs[0];
                }
                else
                {
                    return 0;
                }
            }
        }

        [ModelVar("tp8wi", "Beam radiation extinction coefficient of canopy", "kb", "", "")]
        public double kbNIR
        {
            get
            {
                if (beamExtCoeffsNIR.Length > 0)
                {
                    return beamExtCoeffsNIR[0];
                }
                else
                {
                    return 0;
                }
            }
        }

        protected double _diffuseExtCoeff = 0.78;
        [ModelPar("naFkz", "Diffuse PAR extinction coefficient", "kd", "", "")]
        public double diffuseExtCoeff
        {
            get
            {
                return _diffuseExtCoeff;
            }
            set
            {
                _diffuseExtCoeff = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    diffuseExtCoeffs[i] = value;
                }
            }
        }

        [ModelVar("4Vt4l", "Diffuse PAR extinction coefficient", "kd", "", "")]
        public double[] diffuseExtCoeffs { get; set; }

        protected double _diffuseExtCoeffNIR = 0.78;
        [ModelPar("naFkz", "Diffuse PAR extinction coefficient", "kd", "", "")]
        public double diffuseExtCoeffNIR
        {
            get
            {
                return _diffuseExtCoeffNIR;
            }
            set
            {
                _diffuseExtCoeffNIR = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    diffuseExtCoeffsNIR[i] = value;
                }
            }
        }

        [ModelVar("4Vt4l", "Diffuse PAR extinction coefficient", "kd", "", "")]
        public double[] diffuseExtCoeffsNIR { get; set; }

        [ModelVar("ODCVR", "Diffuse and scattered-diffuse PAR extinction coefficient", "kd'", "", "", "l,t")]
        public double[] diffuseScatteredDiffuses { get; set; }

        [ModelVar("ODCVR", "Diffuse and scattered-diffuse PAR extinction coefficient", "kd'", "", "", "l,t")]
        public double[] diffuseScatteredDiffusesNIR { get; set; }

        protected double _diffuseScatteredDiffuse = 0.719;
        [ModelPar("sF61n", "Diffuse and scattered diffuse", "kd'", "", "")]
        public double diffuseScatteredDiffuse
        {
            get
            {
                return _diffuseScatteredDiffuse;
            }
            set
            {
                _diffuseScatteredDiffuse = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    diffuseScatteredDiffuses[i] = value;
                }
            }
        }

        protected double _D0 = 0.04;
        [ModelPar("N5Imn", "Leaf to Air vapour pressure difference", "Do", "", "")]
        public double D0
        {
            get
            {
                return _D0;
            }
            set
            {
                _D0 = value;
            }
        }

        protected double _leafReflectionCoeff = 0.1;
        [ModelPar("XHAOb", "Leaf reflection coefficient for PAR", "ρ1", "", "")]
        public double leafReflectionCoeff
        {
            get
            {
                return _leafReflectionCoeff;
            }
            set
            {
                _leafReflectionCoeff = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    leafReflectionCoeffs[i] = value;
                }
            }
        }

        protected double _spectralCorrection = 0.1;
        [ModelVar("RmjoK", "Spectral Correction for J", "ja", "", "")] //
        public double ja { get; set; }

        [ModelVar("tmH8F", "Leaf reflection coefficient for PAR", "ρ", "l", "")]
        public double[] leafReflectionCoeffs { get; set; }

        protected double _leafTransmissivity = 0.05;
        [ModelPar("9EEzT", "Leaf transmissivity to PAR", "τ", "1", "")]
        public double leafTransmissivity
        {
            get
            {
                return _leafTransmissivity;
            }
            set
            {
                _leafTransmissivity = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    leafTransmissivitys[i] = value;
                }
            }
        }

        [ModelVar("LIt10", "Leaf transmissivity to PAR", "τ", "1", "")]
        public double[] leafTransmissivitys { get; set; }

        [ModelPar("k1pe7", "Leaf scattering coefficient of PAR", "σ", "", "")]
        public double[] leafScatteringCoeffs { get; set; }

        protected double _leafScatteringCoeff = 0.15;
        [ModelPar("taH6E", "Leaf scattering coefficient of PAR", "σ", "", "")]
        public double leafScatteringCoeff
        {
            get
            {
                return _leafScatteringCoeff;
            }
            set
            {
                _leafScatteringCoeff = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    leafScatteringCoeffs[i] = value;
                }
            }
        }

        [ModelPar("k1pe7", "Leaf scattering coefficient of PAR", "σ", "", "")]
        public double[] leafScatteringCoeffsNIR { get; set; }

        protected double _leafScatteringCoeffNIR = 0.15;
        [ModelPar("taH6E", "Leaf scattering coefficient of PAR", "σ", "", "")]
        public double leafScatteringCoeffNIR
        {
            get
            {
                return _leafScatteringCoeffNIR;
            }
            set
            {
                _leafScatteringCoeffNIR = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    leafScatteringCoeffsNIR[i] = value;
                }
            }
        }

        [ModelVar("mkTm5", "Reflection coefficient of a canopy with horizontal leaves", "ρh", "", "")]
        public double[] reflectionCoefficientHorizontals { get; set; }

        [ModelVar("mkTm5", "Reflection coefficient of a canopy with horizontal leaves", "ρh", "", "")]
        public double[] reflectionCoefficientHorizontalsNIR { get; set; }

        [ModelVar("2tQ4l", "Canopy-level reflection coefficient for direct PAR", "ρ", "cb", "", "l,t")]
        public double[] beamReflectionCoeffs { get; set; }

        [ModelVar("2tQ4l", "Canopy-level reflection coefficient for direct PAR", "ρ", "cb", "", "l,t")]
        public double[] beamReflectionCoeffsNIR { get; set; }

        protected double _diffuseReflectionCoeff = 0.036;
        [ModelPar("z7i2v", "Canopy-level reflection coefficient for diffuse PAR", "ρ", "cd", "")]
        public double diffuseReflectionCoeff
        {
            get
            {
                return _diffuseReflectionCoeff;
            }
            set
            {
                _diffuseReflectionCoeff = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    diffuseReflectionCoeffs[i] = value;
                }
            }
        }

        [ModelVar("ftev5", "Canopy-level reflection coefficient for diffuse PAR", "ρ", "cd", "", "l,t")]
        public double[] diffuseReflectionCoeffs { get; set; }

        protected double _diffuseReflectionCoeffNIR = 0.036;
        [ModelPar("z7i2v", "Canopy-level reflection coefficient for diffuse PAR", "ρ", "cd", "")]
        public double diffuseReflectionCoeffNIR
        {
            get
            {
                return _diffuseReflectionCoeffNIR;
            }
            set
            {
                _diffuseReflectionCoeffNIR = value;
                for (int i = 0; i < _nLayers; i++)
                {
                    diffuseReflectionCoeffsNIR[i] = value;
                }
            }
        }

        [ModelVar("ftev5", "Canopy-level reflection coefficient for diffuse PAR", "ρ", "cd", "", "l,t")]
        public double[] diffuseReflectionCoeffsNIR { get; set; }

        [ModelVar("fcTBH", "Proportion of intercepted radiation", "F(l)", "", "")]
        public double[] propnInterceptedRadns { get; set; }

        [ModelVar("gZLKz", "Proportion of intercepted radiation Accumulated", "F(c)", "", "")]
        public double[] propnInterceptedRadnsAccum { get; set; }

        [ModelVar("264dv", "Proportion of intercepted radiation Accumulated", "F(c)", "", "")]
        public double Fc
        {
            get
            {
                return propnInterceptedRadns.Sum();
            }
        }

        [ModelVar("pcSTq", "Total absorbed radiation for the canopy", "Iabs", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiation { get; set; }

        [ModelVar("pcSTq", "Total absorbed radiation for the canopy", "Iabs", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiationNIR { get; set; }

        [ModelVar("pcSTq", "Total absorbed radiation for the canopy", "Iabs", "", "μmol m-2 ground s-1")]
        public double[] absorbedRadiationPAR { get; set; }

        [ModelPar("", "", "'", "", "", "")]
        public double rcp { get; set; }

        [ModelPar("", "", "'", "", "", "")]
        public double sigma { get; set; }

        [ModelPar("", "", "'", "", "", "")]
        public double lambda { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double s { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double es { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double es1 { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double radi { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double rair { get; set; }

        [ModelVar("", "", "'", "", "", "")]
        public double directPAR;

        [ModelVar("", "", "'", "", "", "")]
        public double diffusePAR;

        [ModelVar("", "", "'", "", "", "")]
        public double directNIR;

        [ModelVar("", "", "'", "", "", "")]
        public double diffuseNIR;

        [ModelVar("", "", "'", "", "", "")]
        public double[] gbh;

        //-----------------------------------------------------------------------
        public LeafCanopy(int numlayers, double lai)
        {
            _nLayers = numlayers;
            initArrays();

            calcLAILayers(lai);
        }
        //-----------------------------------------------------------------------
        public LeafCanopy(int numlayers, double lai, double leafangle)
        {
            _nLayers = numlayers;
            initArrays();
            for (int i = 0; i < _nLayers; i++)
            {
                leafAngles[i] = new Angle(leafangle, AngleType.Deg);
            }

            calcLAILayers(lai);
        }
        //-----------------------------------------------------------------------
        public LeafCanopy(int numlayers, double lai, double[] leafangles)
        {
            _nLayers = numlayers;
            initArrays();
            for (int i = 0; i < _nLayers; i++)
            {
                leafAngles[i] = new Angle(leafangles[i], AngleType.Deg);
            }

            calcLAILayers(lai);
        }
        //-----------------------------------------------------------------------
        public LeafCanopy()
        {
            _nLayers = 5;
            initArrays();

            C3 = new PathwayParametersC3();
            C4 = new PathwayParametersC4();

            CPath = C3;
        }
        //-----------------------------------------------------------------------
        public void photoPathwayChanged(PhotosynthesisModel.PhotoPathway pathway)
        {
            if (pathway == PhotosynthesisModel.PhotoPathway.C3)
            {
                CPath = C3;
            }
            else
            {
                CPath = C4;
            }
        }
        //-----------------------------------------------------------------------

        protected void initArrays()
        {
            leafAngles = new Angle[_nLayers];
            LAIs = new double[_nLayers];
            LAIAccums = new double[_nLayers];
            beamPenetrations = new double[_nLayers];
            sunlitLAIs = new double[_nLayers];
            shadedLAIs = new double[_nLayers];
            shadowProjectionCoeffs = new double[_nLayers];
            beamExtCoeffs = new double[_nLayers];
            beamScatteredBeams = new double[_nLayers];
            diffuseExtCoeffs = new double[_nLayers];
            diffuseScatteredDiffuses = new double[_nLayers];
            leafReflectionCoeffs = new double[_nLayers];
            leafTransmissivitys = new double[_nLayers];
            reflectionCoefficientHorizontals = new double[_nLayers];
            beamReflectionCoeffs = new double[_nLayers];
            diffuseReflectionCoeffs = new double[_nLayers];
            leafScatteringCoeffs = new double[_nLayers];
            propnInterceptedRadns = new double[_nLayers];
            propnInterceptedRadnsAccum = new double[_nLayers];
            absorbedRadiation = new double[_nLayers];

            beamExtCoeffsNIR = new double[_nLayers];
            beamScatteredBeamsNIR = new double[_nLayers];
            diffuseExtCoeffsNIR = new double[_nLayers];
            diffuseScatteredDiffusesNIR = new double[_nLayers];
            reflectionCoefficientHorizontalsNIR = new double[_nLayers];
            beamReflectionCoeffsNIR = new double[_nLayers];
            diffuseReflectionCoeffsNIR = new double[_nLayers];
            leafScatteringCoeffsNIR = new double[_nLayers];
            absorbedRadiationNIR = new double[_nLayers];
            absorbedRadiationPAR = new double[_nLayers];

            us = new double[_nLayers];
            rb_Hs = new double[_nLayers];
            rb_H2Os = new double[_nLayers];
            rb_CO2s = new double[_nLayers];
            rts = new double[_nLayers];
            rb_H_LAIs = new double[_nLayers];
            rb_H2O_LAIs = new double[_nLayers];
            //boundryLayerConductance = new double[_nLayers];

            Ac = new double[_nLayers];
            Acgross = new double[_nLayers];

            biomassC = new double[_nLayers];

            //Nitrogen variables
            leafNs = new double[_nLayers];

            VcMax25 = new double[_nLayers];
            J2Max25 = new double[_nLayers];
            JMax25 = new double[_nLayers];
            Rd25 = new double[_nLayers];
            VpMax25 = new double[_nLayers];

            leafWidths = new double[_nLayers];

            gbh = new double[_nLayers];

            LAI = LAI;
            leafNTopCanopy = leafNTopCanopy;

            leafAngle = leafAngle;
            leafReflectionCoeff = leafReflectionCoeff;
            leafTransmissivity = leafTransmissivity;
            diffuseExtCoeff = diffuseExtCoeff;
            diffuseReflectionCoeff = diffuseReflectionCoeff;
            leafWidth = leafWidth;

            diffuseScatteredDiffuse = diffuseScatteredDiffuse;

        }
        //-----------------------------------------------------------------------
        void calcLAILayers(double lai)
        {
            LAI = LAI;
        }
        //-----------------------------------------------------------------------
        void calcLAILayers()
        {
            double LAITotal = 0;

            for (int i = 0; i < _nLayers; i++)
            {
                LAIs[i] = LAI / _nLayers;
                LAITotal += LAIs[i];

                LAIAccums[i] = LAITotal;
            }
        }
        //-----------------------------------------------------------------------
        public void calcCanopyStructure(double sunAngleRadians)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                //Shadow projection coefficient
                Angle θ0 = new Angle((leafAngles[i].rad > sunAngleRadians ? Math.Acos(1 / Math.Tan(leafAngles[i].rad) * Math.Tan(sunAngleRadians)) : -1), AngleType.Rad);
                if (θ0.rad == -1)
                {
                    shadowProjectionCoeffs[i] = Math.Cos(leafAngles[i].rad) * Math.Sin(sunAngleRadians);
                }
                else
                {
                    shadowProjectionCoeffs[i] = 2 / Math.PI * Math.Sin(leafAngles[i].rad) * Math.Cos(sunAngleRadians) *
                        Math.Sin(θ0.rad) + ((1 - θ0.deg / 90) * Math.Cos(leafAngles[i].rad) * Math.Sin(sunAngleRadians));
                }

                //leafScatteringCoeffs[i] = leafReflectionCoeffs[i] + leafTransmissivitys[i];
                if (sunAngleRadians > 0)
                {
                    beamExtCoeffs[i] = shadowProjectionCoeffs[i] / Math.Sin(sunAngleRadians);
                    beamExtCoeffsNIR[i] = shadowProjectionCoeffs[i] / Math.Sin(sunAngleRadians);

                }
                else
                {
                    beamExtCoeffs[i] = 0;
                    beamExtCoeffsNIR[i] = 0;

                }
                beamPenetrations[i] = Math.Exp(-1 * beamExtCoeffs[i] * LAIAccums[i]);

                beamScatteredBeams[i] = beamExtCoeffs[i] * Math.Pow(1 - leafScatteringCoeffs[i], 0.5);

                diffuseScatteredDiffuses[i] = diffuseExtCoeffs[i] * Math.Pow(1 - leafScatteringCoeffs[i], 0.5);

                reflectionCoefficientHorizontals[i] = (1 - Math.Pow(1 - leafScatteringCoeffs[i], 0.5)) / (1 + Math.Pow(1 - leafScatteringCoeffs[i], 0.5));

                beamReflectionCoeffs[i] = 1 - Math.Exp(-2 * reflectionCoefficientHorizontals[i] * beamExtCoeffs[i] / (1 + beamExtCoeffs[i]));

                //NIR
                beamScatteredBeamsNIR[i] = beamExtCoeffsNIR[i] * Math.Pow(1 - leafScatteringCoeffsNIR[i], 0.5);

                diffuseScatteredDiffusesNIR[i] = diffuseExtCoeffsNIR[i] * Math.Pow(1 - leafScatteringCoeffsNIR[i], 0.5);

                reflectionCoefficientHorizontalsNIR[i] = (1 - Math.Pow(1 - leafScatteringCoeffsNIR[i], 0.5)) / (1 + Math.Pow(1 - leafScatteringCoeffsNIR[i], 0.5));

                beamReflectionCoeffsNIR[i] = 1 - Math.Exp(-2 * reflectionCoefficientHorizontalsNIR[i] * beamExtCoeffsNIR[i] / (1 + beamExtCoeffsNIR[i]));

                propnInterceptedRadnsAccum[i] = 1 - Math.Exp(-beamExtCoeffs[i] * LAIAccums[i]);

                propnInterceptedRadns[i] = propnInterceptedRadnsAccum[i] - (i == 0 ? 0 : propnInterceptedRadnsAccum[i - 1]);
            }
        }
        //-----------------------------------------------------------------------
        void calcAbsorbedRadiation(EnvironmentModel em)
        {
            // double[] radiation = new double[_nLayers];

            for (int i = 0; i < _nLayers; i++)
            {
                //radiation[i] = (1 - beamReflectionCoeffs[i]) * beamScatteredBeams[i] * em.directRadiation * Math.Exp(-beamScatteredBeams[i] * LAIAccums[i]) +
                //(1 - diffuseReflectionCoeffs[i]) * diffuseScatteredDiffuses[i] * em.diffuseRadiation * Math.Exp(-diffuseScatteredDiffuses[i] * LAIAccums[i]);
                absorbedRadiation[i] = (1 - beamReflectionCoeffs[i]) * em.directRadiationPAR * ((i == 0 ? 1 : Math.Exp(-beamScatteredBeams[i] * LAIAccums[i - 1])) - Math.Exp(-beamScatteredBeams[i] * LAIAccums[i])) +
                    (1 - diffuseReflectionCoeffs[i]) * em.diffuseRadiationPAR * ((i == 0 ? 1 : Math.Exp(-diffuseScatteredDiffuses[i] * LAIAccums[i - 1])) - Math.Exp(-diffuseScatteredDiffuses[i] * LAIAccums[i]));

                directPAR = em.directRadiationPAR / 0.5 / 4.56 / 1000000 * 0.5 * 1000000;
                diffusePAR = em.diffuseRadiationPAR / 0.5 / 4.25 / 1000000 * 0.5 * 1000000;
                directNIR = em.directRadiationPAR / 0.5 / 4.56 / 1000000 * 0.5 * 1000000;
                diffuseNIR = em.diffuseRadiationPAR / 0.5 / 4.25 / 1000000 * 0.5 * 1000000;


                absorbedRadiationPAR[i] = (1 - beamReflectionCoeffs[i]) * directPAR * ((i == 0 ? 1 : Math.Exp(-beamScatteredBeams[i] * LAIAccums[i - 1])) - Math.Exp(-beamScatteredBeams[i] * LAIAccums[i])) +
                    (1 - diffuseReflectionCoeffs[i]) * diffusePAR * ((i == 0 ? 1 : Math.Exp(-diffuseScatteredDiffuses[i] * LAIAccums[i - 1])) - Math.Exp(-diffuseScatteredDiffuses[i] * LAIAccums[i]));

                absorbedRadiationNIR[i] = (1 - beamReflectionCoeffsNIR[i]) * directNIR * ((i == 0 ? 1 : Math.Exp(-beamScatteredBeamsNIR[i] * LAIAccums[i - 1])) - Math.Exp(-beamScatteredBeamsNIR[i] * LAIAccums[i])) +
                    (1 - diffuseReflectionCoeffsNIR[i]) * diffuseNIR * ((i == 0 ? 1 : Math.Exp(-diffuseScatteredDiffusesNIR[i] * LAIAccums[i - 1])) - Math.Exp(-diffuseScatteredDiffusesNIR[i] * LAIAccums[i]));
            }

            //TrapezoidLayer.integrate(_nLayers, absorbedRadiation, radiation, LAIs);
        }
        //-----------------------------------------------------------------------

        #region Nitrogen
        protected double _leafNTopCanopy = 137;
        [ModelPar("fgMsM", "Leaf N at canopy top", "N", "0", "mmol N/m2", "", "m2 leaf")]
        public double leafNTopCanopy
        {
            get { return _leafNTopCanopy; }
            set { _leafNTopCanopy = value; }
        }

        protected double _NAllocationCoeff = 0.713;
        [ModelPar("s8zoc", "Coefficient of leaf N allocation in canopy", "k", "n", "")]
        public double NAllocationCoeff
        {
            get { return _NAllocationCoeff; }
            set { _NAllocationCoeff = value; }
        }


        //protected double _leafNConc = 120;
        //[ModelPar("ljBSP", "Leaf N concentration (measured at a particular LAI)", "Nl", "mmol N m-2 leaf")]
        //public double leafNConc
        //{
        //    get { return _leafNConc; }
        //    set { _leafNConc = value; }
        //}

        protected double _Vpr_l = 80;
        [ModelPar("J2u5N", "PEP regeneration rate per unit leaf area at 25°C", "V", "pr_l", "μmol/m2/s", "", "m2 leaf", true)]
        public double Vpr_l
        {
            get { return _Vpr_l; }
            set { _Vpr_l = value; }
        }

        [ModelVar("xj9ot", "Total canopy N", "Nc", "", "mmol N m-2 ground")]
        public double Nc { get; set; }

        [ModelVar("HY9Qj", "Average canopy N", "Nc_av", "", "mmol N m-2 ground")]
        public double NcAv { get; set; }


        protected double _f = 0.15;
        [ModelPar("ZW890", "Empirical spectral correction factor", "f", "", "")]
        public double f
        {
            get { return _f; }
            set { _f = value; }
        }

        [ModelVar("nGIyH", "Leaf nitrogen distribution", "Nl", "", "g N m-2 leaf")]
        public double[] leafNs { get; set; }

        [ModelVar("cVhgB", "Maximum rate of Rubisco carboxylation @ 25", "V", "c_Max@25°", "μmol/m2/s")]
        public double[] VcMax25 { get; set; }

        [ModelVar("MdQHB", "Maximum rate of electron transport  @ 25", "J2Max", "", "μmol/m2/s")]
        public double[] J2Max25 { get; set; }

        [ModelVar("zp4O3", "Maximum rate of electron transport  @ 25", "J", "Max@25°", "μmol/m2/s")]
        public double[] JMax25 { get; set; }

        [ModelVar("pbYnG", "Leaf day respiration @ 25°", "R", "d@25°", "μmol/m2/s")]
        public double[] Rd25 { get; set; }

        [ModelVar("WfOpd", "Maximum rate of P activity-limited carboxylation for the canopy @ 25", "V", "p_Max@25°", "μmol/m2/s", "", "", true)]
        public double[] VpMax25 { get; set; }

        protected double _θ2 = 0.7;
        [ModelPar("rClzy", "Convexity factor for response of J2 to absorbed PAR", "θ", "2", "")]
        public double θ2
        {
            get { return _θ2; }
            set { _θ2 = value; }
        }




        [ModelVar("glKdy", "SLN at canopy top", "SLNo", "", "")]
        public double SLNTop { get; set; }


        protected double _fpseudo = 0.1;
        [ModelPar("uz8TM", "Fraction of electrons at PSI that follow pseudocyclic transport", "f", "pseudo", "")]
        public double fpseudo
        {
            get { return _fpseudo; }
            set { _fpseudo = value; }
        }

        [ModelVar("beFC7", "Quantum efficiency of PSII e- transport under strictly limiting light", "α2", "2", "", "LL")]
        public double a2 { get; set; }

        public double calcSLN(double LAIAc, double structuralN)
        {
            return (leafNTopCanopy - structuralN) * Math.Exp(-NAllocationCoeff *
                     LAIAc / LAIs.Sum()) + structuralN;
        }

        public double _B = Math.Round(30.0 / 44 * 0.6, 3);
        [ModelVar("Vm5Ix", "Biomass conversion efficiency ", "B", "", "", "")]
        public double B
        {
            get { return _B; }
            set { _B = value; }
        }

        //-----------------------------------------------------------------------
        void calcLeafNitrogenDistribution(PhotosynthesisModel PM)
        {
            //-------------This is only when coupled with Apsim----------------------------------------
            //-------------Otherwise use parameters----------------------------------------------------
            if (PM.nitrogenModel == PhotosynthesisModel.NitrogenModel.APSIM)
            {
                SLNTop = PM.canopy.CPath.SLNAv * PM.canopy.CPath.SLNRatioTop;

                leafNTopCanopy = SLNTop * 1000 / 14;

                NcAv = PM.canopy.CPath.SLNAv * 1000 / 14;

                NAllocationCoeff = -1 * Math.Log((NcAv - PM.canopy.CPath.structuralN) / (leafNTopCanopy - PM.canopy.CPath.structuralN)) * 2;
            }
            //-------------This is only when coupled with Apsim----------------------------------------
            else
            {
                SLNTop = leafNTopCanopy / 1000 * 14;

                NcAv = (leafNTopCanopy - PM.canopy.CPath.structuralN) * Math.Exp(-0.5 * NAllocationCoeff) + PM.canopy.CPath.structuralN;

                PM.canopy.CPath.SLNAv = NcAv / 1000 * 14;

                PM.canopy.CPath.SLNRatioTop = SLNTop / PM.canopy.CPath.SLNAv;
            }

            for (int i = 0; i < _nLayers; i++)
            {
                leafNs[i] = calcSLN(LAIAccums[i], PM.canopy.CPath.structuralN);

                VcMax25[i] = LAI * CPath.psiVc * (leafNTopCanopy - PM.canopy.CPath.structuralN) * (
                    (i == 0 ? 1 : Math.Exp(-NAllocationCoeff * LAIAccums[i - 1] / LAI)) -
                    Math.Exp(-NAllocationCoeff * LAIAccums[i] / LAI)) / NAllocationCoeff;

                J2Max25[i] = LAI * CPath.psiJ2 * (leafNTopCanopy - PM.canopy.CPath.structuralN) * (
                    (i == 0 ? 1 : Math.Exp(-NAllocationCoeff * LAIAccums[i - 1] / LAI)) -
                    Math.Exp(-NAllocationCoeff * LAIAccums[i] / LAI)) / NAllocationCoeff;

                JMax25[i] = LAI * CPath.psiJ * (leafNTopCanopy - PM.canopy.CPath.structuralN) * (
                   (i == 0 ? 1 : Math.Exp(-NAllocationCoeff * LAIAccums[i - 1] / LAI)) -
                   Math.Exp(-NAllocationCoeff * LAIAccums[i] / LAI)) / NAllocationCoeff;

                Rd25[i] = LAI * CPath.psiRd * (leafNTopCanopy - PM.canopy.CPath.structuralN) * (
                    (i == 0 ? 1 : Math.Exp(-NAllocationCoeff * LAIAccums[i - 1] / LAI)) -
                    Math.Exp(-NAllocationCoeff * LAIAccums[i] / LAI)) / NAllocationCoeff;

                VpMax25[i] = LAI * CPath.psiVp * (leafNTopCanopy - PM.canopy.CPath.structuralN) * (
                    (i == 0 ? 1 : Math.Exp(-NAllocationCoeff * LAIAccums[i - 1] / LAI)) -
                    Math.Exp(-NAllocationCoeff * LAIAccums[i] / LAI)) / NAllocationCoeff;
            }
        }

        #endregion


        #region InstantaneousPhotosynthesis
        protected double _k2 = 0.284;
        [ModelPar("AweVY", "", "k2(LL)", "", "")]
        public double k2
        {
            get { return _k2; }
            set { _k2 = value; }
        }

        protected double _θ = 0.7;
        [ModelPar("OlCWb", "Convexity factor for response of J to PAR", "θ", "", "")]
        public double θ
        {
            get { return _θ; }
            set { _θ = value; }
        }

        protected double _oxygenPartialPressure = 210000;
        [ModelPar("4N7O4", "Oxygen partial pressure inside leaves", "O", "l", "μbar")]
        public double oxygenPartialPressure
        {
            get { return _oxygenPartialPressure; }
            set { _oxygenPartialPressure = value; }
        }

        protected double _respirationRubiscoRatio = 0;
        [ModelPar("ojS8u", "Ratio of leaf respiration to PS Rubisco capacity", "Rd/Vcmax", "", "-")]
        public double respirationRubiscoRatio
        {
            get { return _respirationRubiscoRatio; }
            set { _respirationRubiscoRatio = value; }
        }

        protected double _Ca = 380;
        [ModelPar("aGpUj", "Ambient air CO2 partial pressure", "C", "a", "μbar")]
        public double Ca
        {
            get { return _Ca; }
            set { _Ca = value; }
        }

        public double _CcInit = 100;
        [ModelPar("wUyRh", "Chloroplast CO2 partial pressure initial guess", "CcInit", "", "μbar")]
        public double CcInit
        {
            get { return _CcInit; }
            set { _CcInit = value; }
        }

        [ModelVar("ATmtA", "Instantaneous net canopy Assimilation", "Ac (gross)", "", "μmol CO2 m-2 ground s-1")]
        public double[] instantaneousAssimilation { get; set; }

        #endregion

        #region Daily canopy biomass accumulation
        [ModelVar("XiJxc", "Ac", "Ac", "", "g CO2 m-2 ground s-1")]
        public double[] Ac { get; set; }
        [ModelVar("SA871", "Ac gross", "Ac", "", "g CO2 m-2 ground s-1")]
        public double[] Acgross { get; set; }

        protected double _hexoseToCO2 = 0.681818182;
        [ModelPar("lkgr9", "", "", "", "")]
        public double hexoseToCO2
        {
            get { return _hexoseToCO2; }
            set { _hexoseToCO2 = value; }
        }

        protected double _biomassToHexose = 0.75;
        [ModelPar("TCFyz", "Biomass to hexose ratio", "", "Biomass:hexose", "g biomass/g hexose")]
        public double biomassToHexose
        {
            get { return _biomassToHexose; }
            set
            {
                _biomassToHexose = value; //notifyChanged(); 
            }
        }

        protected double _maintenanceRespiration = 0.075;
        [ModelPar("ynLXn", "Maintenance and growth respiration to hexose ratio", "", "Respiration:hexose", "g hexose/g CO2")]
        public double maintenanceRespiration
        {
            get { return _maintenanceRespiration; }
            set
            {
                _maintenanceRespiration = value; //notifyChanged(); 
            }
        }
        [ModelVar("vUYQG", "Total biomass accumulation", "BiomassC", "", "g biomass m-2 ground hr-1")]
        public double totalBiomassC { get; set; }

        [ModelVar("ZKlcU", "Total biomass accumulation", "BiomassC", "", "g biomass m-2 ground hr-1")]
        public double[] biomassC { get; set; }

        [ModelVar("sAF5H", "", "", "", "")]
        public double Sco { get; set; }
        #endregion

        #region Conductance and Resistance Parameters
        [ModelVar("fJ3nt", "Leaf boundary layer resistance for heat", "rb_H", "", "s m-1")]
        public double[] rb_Hs { get; set; }

        [ModelVar("am5HZ", "Leaf boundary layer resistance for H2O", "rb_H2O", "", "s m-1")]
        public double[] rb_H2Os { get; set; }

        [ModelVar("zjhMW", "Leaf boundary layer resistance for CO2", "rb_CO2", "", "s m-1")]
        public double[] rb_CO2s { get; set; }


        protected double _gs0_CO2 = 0.01;
        [ModelPar("zpNXx", "Residual stomatal conductance of CO2", "g", "s_CO2", "mol/m2/s", "", "mol H2O, m2 leaf")]
        public double gs0_CO2
        {
            get { return _gs0_CO2; }
            set { _gs0_CO2 = value; }
        }


        protected double _gs_CO2 = 0.3;
        [ModelPar("jpiir", "Stomatal conductance of CO2", "g", "s_CO2", "mol/m2/s", "", "mol H2O, m2 leaf")]
        public double gs_CO2
        {
            get { return _gs_CO2; }
            set { _gs_CO2 = value; }
        }


        protected double _gm_0 = 0;
        [ModelPar("7J9FU", "", "gm_0", "", "")]
        public double gm_0
        {
            get { return _gm_0; }
            set { _gm_0 = value; }
        }

        protected double _gm_delta = 1.35;
        [ModelPar("WTRZb", "", "gm_delta", "", "")]
        public double gm_delta
        {
            get { return _gm_delta; }
            set { _gm_delta = value; }
        }

        protected double _a = 74.7;
        [ModelPar("LUm53", "Empirical coefficient of the impact function of VDPla", "a", "", "")]
        public double a
        {
            get { return _a; }
            set { _a = value; }
        }

        protected double _Do = 0.04;
        [ModelPar("n1lDz", "Emprical coefficient for fvpd", "D", "o", "kPa")]
        public double Do
        {
            get { return _Do; }
            set { _Do = value; }
        }

        [ModelVar("7uBqi", "Molar density of air", "ρa", "", "mol m-3")]
        public double ra { get; set; }
        #endregion

        #region Leaf temperature from Penman-Monteith combination equation (isothermal form)
        protected double _energyConvRatio = 0.208;
        [ModelPar("XVJhn", "Energy conversion ratio", "", "", "J s-1 m-2 : mmol m-2 s-1")]
        public double energyConvRatio
        {
            get { return _energyConvRatio; }
            set { _energyConvRatio = value; }
        }

        protected double _Bz = 5.67038E-08;
        [ModelPar("zJyMY", "Stefan-Boltzmann constant", "Bz", "", "J s-1 K-4")]
        public double Bz
        {
            get { return _Bz; }
            set { _Bz = value; }
        }
        protected double _l = 2.26;
        [ModelPar("baxMC", "Latent heat of vaporization of water vapour", "l", "", "MJ kg-1")]
        public double l
        {
            get { return _l; }
            set { _l = value; }
        }

        protected double _mwRatio = 0.622;
        [ModelPar("6mb4O", "Mwratio", "", "", "")]
        public double mwRatio
        {
            get { return _mwRatio; }
            set { _mwRatio = value; }
        }

        protected double _p = 101.325;
        [ModelPar("tpm6l", "Atmospheric pressure", "p", "", "kPa")]
        public double p
        {
            get { return _p; }
            set { _p = value; }
        }

        protected double _Height = 1.5;
        [ModelPar("sc9d8", "Crop height", "H", "", "m")]
        public double Height
        {
            get { return _Height; }
            set { _Height = value; }
        }

        protected double _u0 = 2;
        [ModelPar("iggg1", "Wind speed at canopy top", "u", "0", "m/s")]
        public double u0
        {
            get { return _u0; }
            set
            {
                _u0 = value;
                //notifyChanged();
            }
        }

        protected double _ku = 0.5;
        [ModelPar("Sj4Gm", "Extinction coefficient for wind speed", "ku", "", "")]
        public double ku
        {
            get { return _ku; }
            set { _ku = value; }
        }

        protected double _leafWidth = 0.1;
        [ModelPar("8cdc4", "Leaf width", "w", "l", "m")]
        public double leafWidth
        {
            get { return _leafWidth; }
            set
            {
                _leafWidth = value;
                for (int i = 0; i < nLayers; i++)
                {
                    leafWidths[i] = _leafWidth;
                }
            }
        }

        [ModelVar("DulsD", "Leaf width", "wl", "", "m", "l")]
        public double[] leafWidths { get; set; }

        protected double _airDensity;
        [ModelVar("D4mwj", "Air density	rair (weight)", "", "", "kg m-3")]
        public double airDensity { get; set; }
        //{
        //    get { return _airDensity; }
        //    set { _airDensity = value; }
        //}

        protected double _cp = 1000;
        [ModelPar("7i0In", "Specific heat of air", "cp", "", "J kg-1 K-1")]
        public double cp
        {
            get { return _cp; }
            set { _cp = value; }
        }

        protected double _Vair = 1.6;
        [ModelPar("H7wDs", "Vapour pressure of air", "Vair", "", "kPa")]
        public double Vair
        {
            get { return _Vair; }
            set { _Vair = value; }
        }

        [ModelVar("yp5fX", "", "fvap", "", "")]
        public double fvap { get; set; }
        [ModelVar("553hc", "", "fclear", "", "")]
        public double fclear { get; set; }
        [ModelVar("WRc27", "Saturated water vapour pressure @ Ta", "es(Ta)", "", "")]
        public double es_Ta { get; set; }
        [ModelVar("2wZdI", "Turbulence resistance (same for heat, CO2 and H2O)", "rt", "", "s m-1 (LAIsun,sh)")]
        public double[] rts { get; set; }
        [ModelVar("Qn31e", "Leaf boundary layer resistance for heat", "rb_H", "", "s m-1")]
        public double[] rb_H_LAIs { get; set; }
        [ModelVar("efoaj", "Leaf boundary layer resistance for H2O", "rb_H2O", "", "s m-1")]
        public double[] rb_H2O_LAIs { get; set; }
        [ModelVar("LpNUY", "Wind speed", "u", "", "m/s", "l")]
        public double[] us { get; set; }
        [ModelVar("c0pfL", "Leaf-to-air vapour pressure difference", "Da", "", "kPa")]
        public double Da { get; set; }
        [ModelVar("WYVVq", "Psychrometric constant", "g", "", "kPa K-1")]
        public double g { get; set; }
        [ModelVar("M0Rdv", "Half the reciprocal of Sc/o", "", "γ*", "")]
        public double g_ { get; set; }

        void calcConductanceResistance(PhotosynthesisModel PM)
        {
            for (int i = 0; i < _nLayers; i++)
            {
                //Intercepted radiation

                //Saturated vapour pressure
                es = PM.envModel.calcSVP(PM.envModel.getTemp(PM.time));
                es1 = PM.envModel.calcSVP(PM.envModel.getTemp(PM.time) + 1);
                s = (es1 - es) / ((PM.envModel.getTemp(PM.time) + 1) - PM.envModel.getTemp(PM.time));


                //Wind speed
                //us[i] = u0 * Math.Exp(-ku * (i + 1));
                us[i] = u0;

                rair = PM.envModel.ATM * 100000 / (287 * (PM.envModel.getTemp(PM.time) + 273)) * 1000 / 28.966;

                gbh[i] = 0.01 * Math.Pow((us[i] / leafWidth), 0.5) *
                    (1 - Math.Exp(-0.5 * ku * LAI)) / (0.5 * ku);

                //Boundary layer
                //rb_Hs[i] = 100 * Math.Pow((leafWidths[i] / us[i]), 0.5);
                //rb_H2Os[i] = 0.93 * rb_Hs[i];

                //rb_CO2s[i] = 1.37 * rb_H2Os[i];

                //rts[i] = 0.74 * Math.Pow(Math.Log(2 - 0.7 * Height) / (0.1 * Height), 2) / (0.16 * us[i]) / LAIs[i];
                //rb_H_LAIs[i] = rb_Hs[i] / LAIs[i];
                //rb_H2O_LAIs[i] = rb_H2Os[i] / LAIs[i];

            }
        }

        public void calcLeafTemperature(PhotosynthesisModel PM, EnvironmentModel EM)
        {
            //double airTemp = EM.getTemp(PM.time);

            //fvap = 0.56 - 0.079 * Math.Pow(10 * Vair, 0.5);

            //fclear = 0.1 + 0.9 * Math.Max(0, Math.Min(1, (EM.atmTransmissionRatio - 0.2) / 0.5));

            //g = (cp * Math.Pow(10, -6)) * p / (l * mwRatio);

            //es_Ta = 5.637E-7 * Math.Pow(airTemp, 4) + 1.728E-5 * Math.Pow(airTemp, 3) + 1.534E-3 *
            //    Math.Pow(airTemp, 2) + 4.424E-2 * airTemp + 6.095E-1;

            //a2 = CPath.F2 * (1 - CPath.fcyc) / (CPath.F2 / CPath.F1 + (1 - CPath.fcyc));

        }

        #endregion


        public void calcCanopyBiomassAccumulation(PhotosynthesisModel PM)
        {
            //for (int i = 0; i < nLayers; i++)
            //{
            //    //TODO -- Rename / refactor variables to reflect units and time
            //    //TODO -- calculate biomass using B after daily A has ben summed (ie 1 calculation per day)
            //    //TODO -- check that we are only calculating between dawn and dusk
            //    //TODO -- use floor and cieling on dusk and dawn to calculate assimilation times

            //    Ac[i] = (PM.sunlit.A[i] + PM.shaded.A[i]) * 3600; // Rename (Acan, hour)  (umolCo2/m2/s)

            //    // Acgross[i] = Ac[i] * Math.Pow(10, -6) * 44; // (gCo2/m2/s)

            //    biomassC[i] = Ac[i] * 44 * B * Math.Pow(10, -6); // Hourly Biomass
            //}
            //totalBiomassC = biomassC.Sum();
        }



        public void run(PhotosynthesisModel PM, EnvironmentModel EM)
        {
            //calcCanopyStructure(EM.sunAngle.rad);

            calcAbsorbedRadiation(EM);
            calcLeafNitrogenDistribution(PM);
            calcConductanceResistance(PM);
            calcLeafTemperature(PM, EM);
            calcTotalLeafNitrogen(PM);
        }


        public void calcTotalLeafNitrogen(PhotosynthesisModel PM)
        {
            totalLeafNitrogen = LAI * ((leafNTopCanopy - PM.canopy.CPath.structuralN) * (1 - Math.Exp(-NAllocationCoeff)) / NAllocationCoeff + PM.canopy.CPath.structuralN);
        }
        //////////////////////////////////////////////////////////////////////////////////
        //-----------C4 Section ----------------------------------------------------------
        //////////////////////////////////////////////////////////////////////////////////

        protected double _alpha = 0.1;
        [ModelPar("L8PzL", "Fraction of O2 evolution occurring in the bundle sheath", "α", "", "")]
        public double alpha
        {
            get
            {
                return _alpha;
            }
            set
            {
                _alpha = value;
            }
        }

        protected double _gbs_CO2 = 0.003;
        [ModelPar("pE0qz", "Conductance to CO2 leakage from the bundle sheath to mesophyll", "g", "bs_CO2", "mol/m2/s", "", "mol of H20, m2 leaf", true)]
        public double gbs_CO2
        {
            get
            {
                return _gbs_CO2;
            }
            set
            {
                _gbs_CO2 = value;
            }
        }

        protected double _fQ = 1;
        [ModelPar("iJHqO", "Fraction of electron transport operating in the Q-cycle", "f", "q", "")]
        public double fQ
        {
            get
            {
                return _fQ;
            }
            set
            {
                _fQ = value;
            }
        }

        protected double _h = 3;
        [ModelPar("EdOLY", "Number of protons, generated by the electron transport chain, required to produce one ATP", "h", "", "")]
        public double h
        {
            get
            {
                return _h;
            }
            set
            {
                _h = value;
            }
        }

        protected double _x = 0.4;
        [ModelPar("fChzp", "Fraction of electrons partitioned to the C4 cycle", "x", "", "")]
        public double x
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        //[ModelVar("3sYmP", "Mesophyll oxygen partial pressure", "Om", "", "μbar")]  //Om==Oi
        //public double Om { get; set; }

        [ModelVar("ngpOW", "", "", "", "")]
        public double z { get; set; }
    }
}
