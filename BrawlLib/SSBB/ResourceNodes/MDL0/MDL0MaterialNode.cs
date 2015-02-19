﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrawlLib.SSBBTypes;
using System.ComponentModel;
using BrawlLib.OpenGL;
using System.Drawing;
using System.IO;
using BrawlLib.Imaging;
using BrawlLib.Wii.Graphics;
using BrawlLib.Wii.Models;
using System.Windows.Forms;
using BrawlLib.IO;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using BrawlLib.Modeling;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe partial class MDL0MaterialNode : MDL0EntryNode
    {
        internal MDL0Material* Header { get { return (MDL0Material*)WorkingUncompressed.Address; } }
        public override ResourceType ResourceType { get { return ResourceType.MDL0Material; } }
        public override bool AllowDuplicateNames { get { return true; } }

        public bool _updating = false;

        public MDL0MaterialNode()
        {
            _normMapRefLight1 =
            _normMapRefLight2 =
            _normMapRefLight3 =
            _normMapRefLight4 = -1;
            //_numLights = 1;

            _chan1 = new LightChannel(true, 63, new RGBAPixel(255, 255, 255, 255), new RGBAPixel(255, 255, 255, 255), 0, 0, this);
            _chan2 = new LightChannel(false, 15, new RGBAPixel(0, 0, 0, 255), new RGBAPixel(), 0, 0, this);
        }

        //Just settings some extra, more brawl-specific defaults here
        public void SetImportValues()
        {
            _lightSetIndex = 20;
            _fogIndex = 4;
            _activeStages = 3;

            C1ColorEnabled = true;
            C1AlphaMaterialSource = GXColorSrc.Vertex;
            C1ColorMaterialSource = GXColorSrc.Vertex;
            C1ColorDiffuseFunction = GXDiffuseFn.Clamped;
            C1ColorAttenuation = GXAttnFn.Spotlight;
            C1AlphaEnabled = true;
            C1AlphaDiffuseFunction = GXDiffuseFn.Clamped;
            C1AlphaAttenuation = GXAttnFn.Spotlight;

            C2ColorDiffuseFunction = GXDiffuseFn.Disabled;
            C2ColorAttenuation = GXAttnFn.None;
            C2AlphaDiffuseFunction = GXDiffuseFn.Disabled;
            C2AlphaAttenuation = GXAttnFn.None;
        }

        #region Variables

        [Category("User Data"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public UserDataCollection UserEntries { get { return _userEntries; } set { _userEntries = value; SignalPropertyChange(); } }
        internal UserDataCollection _userEntries = new UserDataCollection();

        public byte
            //_numLights,
            _indirectMethod1,
            _indirectMethod2,
            _indirectMethod3,
            _indirectMethod4,
            _activeStages,
            _activeIndStages,
            _zCompLoc;
        public sbyte
            _normMapRefLight1,
            _normMapRefLight2,
            _normMapRefLight3,
            _normMapRefLight4,
            _lightSetIndex = -1,
            _fogIndex = -1;
        private Bin32 _usageFlags = new Bin32();
        public CullMode _cull = CullMode.Cull_None;
        public uint _texMtxFlags;

        public LightChannel 
            _chan1 = new LightChannel(true, 63, new RGBAPixel(255, 255, 255, 255), new RGBAPixel(255, 255, 255, 255), 0, 0),
            _chan2 = new LightChannel(false, 15, new RGBAPixel(0, 0, 0, 255), new RGBAPixel(), 0, 0);

        internal List<MDL0ObjectNode> _objects = new List<MDL0ObjectNode>();
        internal List<XFData> XFCmds = new List<XFData>();

        //In order of appearance in display list:
        //Mode block
        internal GXAlphaFunction _alphaFunc = GXAlphaFunction.Default;
        internal ZMode _zMode = ZMode.Default;
        //Mask, does not allow changing the dither/update bits
        internal Wii.Graphics.BlendMode _blendMode = Wii.Graphics.BlendMode.Default;
        internal ConstantAlpha _constantAlpha = ConstantAlpha.Default;
        //Tev Color Block
        internal MatTevColorBlock _tevColorBlock = MatTevColorBlock.Default;
        //Pad 4
        //Tev Konstant Block
        internal MatTevKonstBlock _tevKonstBlock = MatTevKonstBlock.Default;
        //Pad 24
        //Indirect texture scale for CMD stages
        internal MatIndMtxBlock _indMtx = MatIndMtxBlock.Default;
        //XF Texture matrix info

        #endregion

        #region Attributes

        public MDL0ObjectNode[] Objects { get { if (!isMetal) return _objects.ToArray(); else return MetalMaterial == null ? null : MetalMaterial._objects.ToArray(); } }
        
        #region Konstant Block

        const string KonstDesc = @"
This color is used by the linked shader. 
In each shader stage, there are properties called KonstantColorSelection and KonstantAlphaSelection.
Those properties can use this color as an argument. This color is referred to as ";

        [Category("TEV Konstant Block"), 
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(KonstDesc + "KSel_0.")]
        public GXColorS10 KReg0Color
        { 
            get { return new GXColorS10() { R = _tevKonstBlock.TevReg0Lo.RB, A = _tevKonstBlock.TevReg0Lo.AG, B = _tevKonstBlock.TevReg0Hi.RB, G = _tevKonstBlock.TevReg0Hi.AG }; }
            set { if (!CheckIfMetal()) { _tevKonstBlock.TevReg0Lo.RB = value.R; _tevKonstBlock.TevReg0Lo.AG = value.A; _tevKonstBlock.TevReg0Hi.RB = value.B; _tevKonstBlock.TevReg0Hi.AG = value.G; k1 = value; } } 
        }
        [Category("TEV Konstant Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(KonstDesc + "KSel_1.")]
        public GXColorS10 KReg1Color 
        { 
            get { return new GXColorS10() { R = _tevKonstBlock.TevReg1Lo.RB, A = _tevKonstBlock.TevReg1Lo.AG, B = _tevKonstBlock.TevReg1Hi.RB, G = _tevKonstBlock.TevReg1Hi.AG }; }
            set { if (!CheckIfMetal()) { _tevKonstBlock.TevReg1Lo.RB = value.R; _tevKonstBlock.TevReg1Lo.AG = value.A; _tevKonstBlock.TevReg1Hi.RB = value.B; _tevKonstBlock.TevReg1Hi.AG = value.G; k2 = value; } } 
        }
        [Category("TEV Konstant Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(KonstDesc + "KSel_2.")]
        public GXColorS10 KReg2Color 
        { 
            get { return new GXColorS10() { R = _tevKonstBlock.TevReg2Lo.RB, A = _tevKonstBlock.TevReg2Lo.AG, B = _tevKonstBlock.TevReg2Hi.RB, G = _tevKonstBlock.TevReg2Hi.AG }; }
            set { if (!CheckIfMetal()) { _tevKonstBlock.TevReg2Lo.RB = value.R; _tevKonstBlock.TevReg2Lo.AG = value.A; _tevKonstBlock.TevReg2Hi.RB = value.B; _tevKonstBlock.TevReg2Hi.AG = value.G; k3 = value; } } 
        }
        [Category("TEV Konstant Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(KonstDesc + "KSel_3.")]
        public GXColorS10 KReg3Color 
        { 
            get { return new GXColorS10() { R = _tevKonstBlock.TevReg3Lo.RB, A = _tevKonstBlock.TevReg3Lo.AG, B = _tevKonstBlock.TevReg3Hi.RB, G = _tevKonstBlock.TevReg3Hi.AG }; }
            set { if (!CheckIfMetal()) { _tevKonstBlock.TevReg3Lo.RB = value.R; _tevKonstBlock.TevReg3Lo.AG = value.A; _tevKonstBlock.TevReg3Hi.RB = value.B; _tevKonstBlock.TevReg3Hi.AG = value.G; k4 = value; } } 
        }

        #endregion

        #region Color Block

        const string ColorDesc = @"
This color is used by the linked shader. 
In each shader stage, there are properties called Color/Alpha Selection A, B, C and D.
Those properties can use this color as an argument. This color is referred to as ";

        [Category("TEV Color Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(ColorDesc + "Color0 and Alpha0.")]
        public GXColorS10 CReg0Color 
        {
            get { return new GXColorS10() { R = _tevColorBlock.TevReg1Lo.RB, A = _tevColorBlock.TevReg1Lo.AG, B = _tevColorBlock.TevReg1Hi0.RB, G = _tevColorBlock.TevReg1Hi0.AG }; }
            set
            {
                if (!CheckIfMetal())
                {
                    _tevColorBlock.TevReg1Lo.RB = value.R;
                    _tevColorBlock.TevReg1Lo.AG = value.A;

                    //High values are always the same...
                    _tevColorBlock.TevReg1Hi0.RB =
                    _tevColorBlock.TevReg1Hi1.RB =
                    _tevColorBlock.TevReg1Hi2.RB = value.B;
                    _tevColorBlock.TevReg1Hi0.AG =
                    _tevColorBlock.TevReg1Hi1.AG =
                    _tevColorBlock.TevReg1Hi2.AG = value.G;

                    c1 = value;
                }
            }
        }
        [Category("TEV Color Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(ColorDesc + "Color1 and Alpha1.")]
        public GXColorS10 CReg1Color 
        {
            get { return new GXColorS10() { R = _tevColorBlock.TevReg2Lo.RB, A = _tevColorBlock.TevReg2Lo.AG, B = _tevColorBlock.TevReg2Hi0.RB, G = _tevColorBlock.TevReg2Hi0.AG }; }
            set
            {
                if (!CheckIfMetal())
                {
                    _tevColorBlock.TevReg2Lo.RB = value.R;
                    _tevColorBlock.TevReg2Lo.AG = value.A;

                    //High values are always the same...
                    _tevColorBlock.TevReg2Hi0.RB =
                    _tevColorBlock.TevReg2Hi1.RB =
                    _tevColorBlock.TevReg2Hi2.RB = value.B;
                    _tevColorBlock.TevReg2Hi0.AG =
                    _tevColorBlock.TevReg2Hi1.AG =
                    _tevColorBlock.TevReg2Hi2.AG = value.G;

                    c2 = value;
                }
            }
        }
        [Category("TEV Color Block"),
        TypeConverter(typeof(GXColorS10StringConverter)),
        Description(ColorDesc + "Color2 and Alpha2.")]
        public GXColorS10 CReg2Color 
        {
            get { return new GXColorS10() { R = _tevColorBlock.TevReg3Lo.RB, A = _tevColorBlock.TevReg3Lo.AG, B = _tevColorBlock.TevReg3Hi0.RB, G = _tevColorBlock.TevReg3Hi0.AG }; } 
            set
            {
                if (!CheckIfMetal()) 
                {
                    _tevColorBlock.TevReg3Lo.RB = value.R; 
                    _tevColorBlock.TevReg3Lo.AG = value.A;

                    //High values are always the same...
                    _tevColorBlock.TevReg3Hi0.RB =
                    _tevColorBlock.TevReg3Hi1.RB =
                    _tevColorBlock.TevReg3Hi2.RB = value.B; 
                    _tevColorBlock.TevReg3Hi0.AG =
                    _tevColorBlock.TevReg3Hi1.AG =
                    _tevColorBlock.TevReg3Hi2.AG = value.G;

                    c3 = value;
                }
            }
        }

        #endregion

        #region Shader linkage
        internal MDL0ShaderNode _shader;
        [Browsable(false)]
        public MDL0ShaderNode ShaderNode
        {
            get { return _shader; }
            set
            {
                if (_shader == value)
                    return;
                if (_shader != null)
                    _shader._materials.Remove(this);
                if ((_shader = value) != null)
                {
                    _shader._materials.Add(this);
                    ActiveShaderStages = _shader.Stages;
                }
            }
        }
        [Browsable(true), TypeConverter(typeof(DropDownListShaders))]
        public string Shader
        {
            get { return _shader == null ? null : _shader.Name; }
            set
            {
                if (CheckIfMetal())
                    return;

                if (String.IsNullOrEmpty(value))
                    ShaderNode = null;
                else
                {
                    MDL0ShaderNode node = Model.FindChild(String.Format("Shaders/{0}", value), false) as MDL0ShaderNode;
                    if (node != null)
                        ShaderNode = node;
                }
            }
        }
        #endregion

        #region Alpha Func

        [Category("Alpha Function")]
        public byte Ref0 { get { return _alphaFunc._ref0; } set { if (!CheckIfMetal()) _alphaFunc._ref0 = value; } }
        [Category("Alpha Function")]
        public AlphaCompare Comp0 { get { return _alphaFunc.Comp0; } set { if (!CheckIfMetal()) _alphaFunc.Comp0 = value;  } }
        [Category("Alpha Function")]
        public AlphaOp Logic { get { return _alphaFunc.Logic; } set { if (!CheckIfMetal()) _alphaFunc.Logic = value;  } }
        [Category("Alpha Function")]
        public byte Ref1 { get { return _alphaFunc._ref1; } set { if (!CheckIfMetal()) _alphaFunc._ref1 = value;  } }
        [Category("Alpha Function")]
        public AlphaCompare Comp1 { get { return _alphaFunc.Comp1; } set { if (!CheckIfMetal()) _alphaFunc.Comp1 = value;  } }

        #endregion

        #region Depth Func

        [Category("Z Mode"), Description("Generally this should be false if using alpha function (transparency), as transparent pixels will change the depth.")]
        public bool CompareBeforeTexture { get { return _zCompLoc != 0; } set { if (!CheckIfMetal()) _zCompLoc = (byte)(value ? 1 : 0); } }
        [Category("Z Mode"), Description("Determines if this material's pixels should be compared to other pixels in order to obscure or be obscured.")]
        public bool EnableDepthTest { get { return _zMode.EnableDepthTest; } set { if (!CheckIfMetal()) _zMode.EnableDepthTest = value;  } }
        [Category("Z Mode")]
        public bool EnableDepthUpdate { get { return _zMode.EnableDepthUpdate; } set { if (!CheckIfMetal()) _zMode.EnableDepthUpdate = value;  } }
        [Category("Z Mode"), Description("How this material should be compared to other materials.")]
        public GXCompare DepthFunction { get { return _zMode.DepthFunction; } set { if (!CheckIfMetal()) _zMode.DepthFunction = value;  } }

        #endregion

        #region Blend Mode

        [Category("Blend Mode"), Description("This allows the textures to be semi-transparent. Cannot be used with alpha function.")]
        public bool EnableBlend { get { return _blendMode.EnableBlend; } set { if (!CheckIfMetal()) { _blendMode.EnableBlend = value; } } }
        [Category("Blend Mode")]
        public bool EnableBlendLogic { get { return _blendMode.EnableLogicOp; } set { if (!CheckIfMetal()) _blendMode.EnableLogicOp = value;  } }
        
        //These are disabled via mask
        //[Category("Blend Mode")]
        //public bool EnableDither { get { return _blendMode.EnableDither; } }
        //[Category("Blend Mode")]
        //public bool EnableColorUpdate { get { return _blendMode.EnableColorUpdate; } }
        //[Category("Blend Mode")]
        //public bool EnableAlphaUpdate { get { return _blendMode.EnableAlphaUpdate; } }

        [Category("Blend Mode")]
        public BlendFactor SrcFactor { get { return _blendMode.SrcFactor; } set { if (!CheckIfMetal()) _blendMode.SrcFactor = value;  } }
        [Category("Blend Mode")]
        public GXLogicOp BlendLogicOp { get { return _blendMode.LogicOp; } set { if (!CheckIfMetal()) _blendMode.LogicOp = value;  } }
        [Category("Blend Mode")]
        public BlendFactor DstFactor { get { return _blendMode.DstFactor; } set { if (!CheckIfMetal()) _blendMode.DstFactor = value;  } }

        [Category("Blend Mode")]
        public bool Subtract { get { return _blendMode.Subtract; } set { if (!CheckIfMetal()) _blendMode.Subtract = value;  } }

        #endregion

        #region Constant Alpha

        [Category("Constant Alpha")]
        public bool Enabled { get { return _constantAlpha.Enable != 0; } set { if (!CheckIfMetal()) _constantAlpha.Enable = (byte)(value ? 1 : 0); } }
        [Category("Constant Alpha")]
        public byte Value { get { return _constantAlpha.Value; } set { if (!CheckIfMetal()) _constantAlpha.Value = value; } }

        #endregion

        #region Indirect Texturing

        [Category("Indirect Texturing")]
        public IndirectMethod IndirectMethodTex1 { get { return (IndirectMethod)_indirectMethod1; } set { if (!CheckIfMetal()) _indirectMethod1 = (byte)value; } }
        [Category("Indirect Texturing")]
        public IndirectMethod IndirectMethodTex2 { get { return (IndirectMethod)_indirectMethod2; } set { if (!CheckIfMetal()) _indirectMethod2 = (byte)value; } }
        [Category("Indirect Texturing")]
        public IndirectMethod IndirectMethodTex3 { get { return (IndirectMethod)_indirectMethod3; } set { if (!CheckIfMetal()) _indirectMethod3 = (byte)value; } }
        [Category("Indirect Texturing")]
        public IndirectMethod IndirectMethodTex4 { get { return (IndirectMethod)_indirectMethod4; } set { if (!CheckIfMetal()) _indirectMethod4 = (byte)value; } }
        
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex1ScaleS { get { return (IndTexScale)_indMtx.SS0val.S_Scale0; } set { if (!CheckIfMetal()) _indMtx.SS0val.S_Scale0 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex1ScaleT { get { return (IndTexScale)_indMtx.SS0val.T_Scale0; } set { if (!CheckIfMetal()) _indMtx.SS0val.T_Scale0 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex2ScaleS { get { return (IndTexScale)_indMtx.SS0val.S_Scale1; } set { if (!CheckIfMetal()) _indMtx.SS0val.S_Scale1 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex2ScaleT { get { return (IndTexScale)_indMtx.SS0val.T_Scale1; } set { if (!CheckIfMetal()) _indMtx.SS0val.T_Scale1 = value; } }
        
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex3ScaleS { get { return (IndTexScale)_indMtx.SS1val.S_Scale0; } set { if (!CheckIfMetal()) _indMtx.SS1val.S_Scale0 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex3ScaleT { get { return (IndTexScale)_indMtx.SS1val.T_Scale0; } set { if (!CheckIfMetal()) _indMtx.SS1val.T_Scale0 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex4ScaleS { get { return (IndTexScale)_indMtx.SS1val.S_Scale1; } set { if (!CheckIfMetal()) _indMtx.SS1val.S_Scale1 = value; } }
        [Category("Indirect Texturing")]
        public IndTexScale IndirectTex4ScaleT { get { return (IndTexScale)_indMtx.SS1val.T_Scale1; } set { if (!CheckIfMetal()) _indMtx.SS1val.T_Scale1 = value; } }
        
        public enum IndirectMethod
        {
            Warp = 0,
            NormalMap,
            NormalMapSpecular,
            Fur,
            Reserved0,
            Reserved1,
            User0,
            User1
        }

        #endregion

        #region Lighting Channels

//        [Category("Lighting Channels"), Browsable(false), Description(@"
//This is how many light channels this material uses. Minimum of 0, maximum of 2.
//If this number is 0, all light channels in this material are ignored.
//If this number is 1, only Light Channel 1 is applied. 
//If this number is 2, both channels are applied.")]
//        public int ActiveLightChannels { get { return _numLights; } set { if (!CheckIfMetal()) _numLights = (byte)value.Clamp(0, 2); } }
        [Category("Lighting Channels"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public LightChannel LightChannel1 { get { return _chan1; } set { if (!CheckIfMetal()) _chan1 = value; } }
        [Category("Lighting Channels"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public LightChannel LightChannel2 { get { return _chan2; } set { if (!CheckIfMetal()) _chan2 = value; } }

        [Category("Lighting Channel 1"), Browsable(false)]
        public LightingChannelFlags C1Flags { get { return _chan1.Flags; } set { if (!CheckIfMetal()) _chan1.Flags = value; } }
        [Category("Lighting Channel 1"), Browsable(false), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel C1MaterialColor { get { return _chan1.MaterialColor; } set { if (!CheckIfMetal()) clr1 = _chan1.MaterialColor = value; } }
        [Category("Lighting Channel 1"), Browsable(false), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel C1AmbientColor { get { return _chan1.AmbientColor; } set { if (!CheckIfMetal()) amb1 = _chan1.AmbientColor = value; } }

        [Category("Lighting Channel 1"), Browsable(false)]
        public GXColorSrc C1ColorMaterialSource
        {
            get { return _chan1.ColorMaterialSource; }
            set { if (!CheckIfMetal()) _chan1.ColorMaterialSource = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public bool C1ColorEnabled
        {
            get { return _chan1.ColorEnabled; }
            set { if (!CheckIfMetal()) _chan1.ColorEnabled = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXColorSrc C1ColorAmbientSource
        {
            get { return _chan1.ColorAmbientSource; }
            set { if (!CheckIfMetal()) _chan1.ColorAmbientSource = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXDiffuseFn C1ColorDiffuseFunction
        {
            get { return _chan1.ColorDiffuseFunction; }
            set { if (!CheckIfMetal()) _chan1.ColorDiffuseFunction = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXAttnFn C1ColorAttenuation
        {
            get { return _chan1.ColorAttenuation; }
            set { if (!CheckIfMetal()) _chan1.ColorAttenuation = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public MatChanLights C1ColorLights
        {
            get { return _chan1.ColorLights; }
            set { if (!CheckIfMetal()) _chan1.ColorLights = value; }
        }

        [Category("Lighting Channel 1"), Browsable(false)]
        public GXColorSrc C1AlphaMaterialSource
        {
            get { return _chan1.AlphaMaterialSource; }
            set { if (!CheckIfMetal()) _chan1.AlphaMaterialSource = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public bool C1AlphaEnabled
        {
            get { return _chan1.AlphaEnabled; }
            set { if (!CheckIfMetal()) _chan1.AlphaEnabled = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXColorSrc C1AlphaAmbientSource
        {
            get { return _chan1.AlphaAmbientSource; }
            set { if (!CheckIfMetal()) _chan1.AlphaAmbientSource = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXDiffuseFn C1AlphaDiffuseFunction
        {
            get { return _chan1.AlphaDiffuseFunction; }
            set { if (!CheckIfMetal()) _chan1.AlphaDiffuseFunction = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public GXAttnFn C1AlphaAttenuation
        {
            get { return _chan1.AlphaAttenuation; }
            set { if (!CheckIfMetal()) _chan1.AlphaAttenuation = value; }
        }
        [Category("Lighting Channel 1"), Browsable(false)]
        public MatChanLights C1AlphaLights
        {
            get { return _chan1.AlphaLights; }
            set { if (!CheckIfMetal()) _chan1.AlphaLights = value; }
        }

        [Category("Lighting Channel 2"), Browsable(false)]
        public LightingChannelFlags C2Flags { get { return _chan2.Flags; } set { if (!CheckIfMetal()) _chan2.Flags = value; } }
        [Category("Lighting Channel 2"), Browsable(false), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel C2MaterialColor { get { return _chan2.MaterialColor; } set { if (!CheckIfMetal()) clr2 = _chan2.MaterialColor = value; } }
        [Category("Lighting Channel 2"), Browsable(false), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel C2AmbientColor { get { return _chan2.AmbientColor; } set { if (!CheckIfMetal()) amb2 = _chan2.AmbientColor = value; } }

        [Category("Lighting Channel 2"), Browsable(false)]
        public GXColorSrc C2ColorMaterialSource
        {
            get { return _chan2.ColorMaterialSource; }
            set { if (!CheckIfMetal()) _chan2.ColorMaterialSource = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public bool C2ColorEnabled
        {
            get { return _chan2.ColorEnabled; }
            set { if (!CheckIfMetal()) _chan2.ColorEnabled = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXColorSrc C2ColorAmbientSource
        {
            get { return _chan2.ColorAmbientSource; }
            set { if (!CheckIfMetal()) _chan2.ColorAmbientSource = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXDiffuseFn C2ColorDiffuseFunction
        {
            get { return _chan2.ColorDiffuseFunction; }
            set { if (!CheckIfMetal()) _chan2.ColorDiffuseFunction = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXAttnFn C2ColorAttenuation
        {
            get { return _chan2.ColorAttenuation; }
            set { if (!CheckIfMetal()) _chan2.ColorAttenuation = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public MatChanLights C2ColorLights
        {
            get { return _chan2.ColorLights; }
            set { if (!CheckIfMetal()) _chan2.ColorLights = value; }
        }

        [Category("Lighting Channel 2"), Browsable(false)]
        public GXColorSrc C2AlphaMaterialSource
        {
            get { return _chan2.AlphaMaterialSource; }
            set { if (!CheckIfMetal()) _chan2.AlphaMaterialSource = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public bool C2AlphaEnabled
        {
            get { return _chan2.AlphaEnabled; }
            set { if (!CheckIfMetal()) _chan2.AlphaEnabled = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXColorSrc C2AlphaAmbientSource
        {
            get { return _chan2.AlphaAmbientSource; }
            set { if (!CheckIfMetal()) _chan2.AlphaAmbientSource = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXDiffuseFn C2AlphaDiffuseFunction
        {
            get { return _chan2.AlphaDiffuseFunction; }
            set { if (!CheckIfMetal()) _chan2.AlphaDiffuseFunction = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public GXAttnFn C2AlphaAttenuation
        {
            get { return _chan2.AlphaAttenuation; }
            set { if (!CheckIfMetal()) _chan2.AlphaAttenuation = value; }
        }
        [Category("Lighting Channel 2"), Browsable(false)]
        public MatChanLights C2AlphaLights
        {
            get { return _chan2.AlphaLights; }
            set { if (!CheckIfMetal()) _chan2.AlphaLights = value; }
        }

        #endregion

        #region General Material

        [Category("Material"), Description(MDL0Node._textureMatrixModeDescription)]
        public TexMatrixMode TextureMatrixMode
        {
            get { return (TexMatrixMode)_texMtxFlags; }
            set
            {
                if (!CheckIfMetal())
                {
                    _texMtxFlags = (uint)value;
                    foreach (MDL0MaterialRefNode r in Children)
                        r._bindState.MatrixMode = r._frameState.MatrixMode = value;
                }
            }
        }
        
        [Category("Material"), Description("True if this material has transparency (alpha function) enabled.")]
        public bool XLUMaterial
        {
            get { return _usageFlags[31]; }
            set
            {
                if (!CheckIfMetal())
                {
                    //Forget all these checks.
                    //We'll let the user have complete freedom, as I've seen objects use materials
                    //as XLU when this bool wasn't on anyway.

                    //bool prev = _usageFlags[31];
                    _usageFlags[31] = value;

                    //string message = "";
                    //for (int i = 0; i < _objects.Count; i++)
                    //    _objects[i].EvalMaterials(ref message);

                    //if (message.Length != 0)
                    //    if (MessageBox.Show(null, "Are you sure you want to continue?\nThe following objects will no longer use this material:\n" + message, "Continue?", MessageBoxButtons.YesNo) == DialogResult.No)
                    //    {
                    //        _changed = false;
                    //        _usageFlags[31] = prev;
                    //        return;
                    //    }

                    //message = "";
                    //for (int i = 0; i < _objects.Count; i++)
                    //    _objects[i].FixMaterials(ref message);

                    //if (message.Length != 0)
                    //    MessageBox.Show("The following objects no longer use this material:\n" + message);
                }
            }
        }

        [Category("Material")]
        public int ID { get { return Index; } }
        [Category("Material"), Description(@"
This dictates how many consecutive stages in the attached shader should be applied to produce the final output color.
For example, if the shader has two stages but this number is 1, the second stage in the shader will be ignored.")]
        public int ActiveShaderStages { get { return _activeStages; } set { if (!CheckIfMetal()) _activeStages = (byte)(value > ShaderNode.Stages ? ShaderNode.Stages : value < 1 ? (byte)1 : value); } }
        [Category("Material"), Description("The number of active indirect textures in the shader.")]
        public int IndirectShaderStages { get { return _activeIndStages; } set { if (!CheckIfMetal()) _activeIndStages = (byte)(value > 4 ? (byte)4 : value < 0 ? (byte)0 : value); } }
        [Category("Material"), Description("This will make one, neither or both sides of the linked objects' mesh invisible.")]
        public CullMode CullMode { get { return _cull; } set { if (!CheckIfMetal()) _cull = value;  } }

        #endregion

        #region SCN0 References

        [Category("SCN0 References"), 
        Description("This is the index of the SCN0 LightSet that should be applied to this model. Set to -1 if unused.")]
        public sbyte LightSetIndex { get { return _lightSetIndex; } set { if (!CheckIfMetal()) { _lightSetIndex = value; if (MetalMaterial != null) MetalMaterial.UpdateAsMetal(); } } }
        [Category("SCN0 References"), 
        Description("This is the index of the SCN0 Fog that should be applied to this model. Set to -1 if unused.")]
        public sbyte FogIndex { get { return _fogIndex; } set { if (!CheckIfMetal()) { _fogIndex = value; if (MetalMaterial != null) MetalMaterial.UpdateAsMetal(); } } }
        [Category("SCN0 References"), 
        Description("This is the index of the SCN0 Light that should be used for indirect texture 1 if it is a normal map. Set to -1 if unused.")]
        public sbyte NormMapRefLight1 { get { return _normMapRefLight1; } set { if (!CheckIfMetal()) _normMapRefLight1 = value; } }
        [Category("SCN0 References"),
        Description("This is the index of the SCN0 Light that should be used for indirect texture 2 if it is a normal map. Set to -1 if unused.")]
        public sbyte NormMapRefLight2 { get { return _normMapRefLight2; } set { if (!CheckIfMetal()) _normMapRefLight1 = value; } }
        [Category("SCN0 References"),
        Description("This is the index of the SCN0 Light that should be used for indirect texture 3 if it is a normal map. Set to -1 if unused.")]
        public sbyte NormMapRefLight3 { get { return _normMapRefLight3; } set { if (!CheckIfMetal()) _normMapRefLight1 = value; } }
        [Category("SCN0 References"),
        Description("This is the index of the SCN0 Light that should be used for indirect texture 4 if it is a normal map. Set to -1 if unused.")]
        public sbyte NormMapRefLight4 { get { return _normMapRefLight4; } set { if (!CheckIfMetal()) _normMapRefLight1 = value; } }

        #endregion

        #endregion

        #region Metal

        public void UpdateAsMetal()
        {
            if (!isMetal)
                return;

            _updating = true;
            if (ShaderNode != null && ShaderNode._autoMetal && ShaderNode._texCount == Children.Count)
            {
                //ShaderNode.DefaultAsMetal(Children.Count); 
            }
            else
            {
                bool found = false;
                foreach (MDL0ShaderNode s in Model._shadList)
                {
                    if (s._autoMetal && s._texCount == Children.Count)
                    {
                        ShaderNode = s;
                        found = true;
                    }
                    else
                    {
                        if (s.Stages == 4)
                        {
                            foreach (MDL0MaterialNode y in s._materials)
                                if (!y.isMetal || y.Children.Count != Children.Count)
                                    goto NotFound;
                            ShaderNode = s;
                            found = true;
                            goto End;
                        NotFound:
                            continue;
                        }
                    }
                }
            End:
                if (!found)
                {
                    MDL0ShaderNode shader = new MDL0ShaderNode();
                    Model._shadGroup.AddChild(shader);
                    shader.DefaultAsMetal(Children.Count);
                    ShaderNode = shader;
                }
            }

            if (MetalMaterial != null)
            {
                Name = MetalMaterial.Name + "_ExtMtl";
                _activeStages = 4;

                if (Children.Count - 1 != MetalMaterial.Children.Count)
                {
                    //Remove all children
                    for (int i = 0; i < Children.Count; i++)
                    {
                        ((MDL0MaterialRefNode)Children[i]).TextureNode = null;
                        ((MDL0MaterialRefNode)Children[i]).PaletteNode = null;
                        RemoveChild(Children[i--]);
                    }

                    //Start over
                    for (int i = 0; i <= MetalMaterial.Children.Count; i++)
                    {
                        MDL0MaterialRefNode mr = new MDL0MaterialRefNode();

                        AddChild(mr);
                        mr.Texture = "metal00";
                        mr._index1 = mr._index2 = i;

                        mr._bindState = TextureFrameState.Neutral;
                        mr._texMatrixEffect.TextureMatrix = Matrix43.Identity;
                        mr._texMatrixEffect.SCNCamera = -1;
                        mr._texMatrixEffect.SCNLight = -1;

                        if (i == MetalMaterial.Children.Count)
                        {
                            mr._minFltr = 5;
                            mr._magFltr = 1;
                            mr._lodBias = -2;
                            mr.HasTextureMatrix = true;
                            mr._projection = (int)TexProjection.STQ;
                            mr._inputForm = (int)TexInputForm.ABC1;
                            mr._sourceRow = (int)TexSourceRow.Normals;
                            mr.Normalize = true;
                            mr.MapMode = MappingMethod.EnvCamera;
                        }
                        else
                        {
                            mr._projection = (int)TexProjection.ST;
                            mr._inputForm = (int)TexInputForm.AB11;
                            mr._sourceRow = (int)TexSourceRow.TexCoord0 + i;
                            mr.Normalize = false;
                            mr.MapMode = MappingMethod.TexCoord;
                        }

                        mr._texGenType = (int)TexTexgenType.Regular;
                        mr._embossSource = 4;
                        mr._embossLight = 2;

                        mr.SetTextMtxData();
                    }

                    _chan1 = new LightChannel(true, 63, new RGBAPixel(128, 128, 128, 255), new RGBAPixel(255, 255, 255, 255), 0, 0, this);
                    C1ColorEnabled = true;
                    C1ColorDiffuseFunction = GXDiffuseFn.Clamped;
                    C1ColorAttenuation = GXAttnFn.Spotlight;
                    C1AlphaEnabled = true;
                    C1AlphaDiffuseFunction = GXDiffuseFn.Clamped;
                    C1AlphaAttenuation = GXAttnFn.Spotlight;

                    _chan2 = new LightChannel(true, 63, new RGBAPixel(255, 255, 255, 255), new RGBAPixel(), 0, 0, this);
                    C2ColorEnabled = true;
                    C2ColorDiffuseFunction = GXDiffuseFn.Disabled;
                    C2ColorAttenuation = GXAttnFn.Specular;
                    C2AlphaDiffuseFunction = GXDiffuseFn.Disabled;
                    C2AlphaAttenuation = GXAttnFn.Specular;

                    _lightSetIndex = MetalMaterial._lightSetIndex;
                    _fogIndex = MetalMaterial._fogIndex;

                    _cull = MetalMaterial._cull;
                    //_numLights = 2;
                    CompareBeforeTexture = true;
                    _normMapRefLight1 =
                    _normMapRefLight2 =
                    _normMapRefLight3 =
                    _normMapRefLight4 = -1;

                    SignalPropertyChange();
                }
            }
            _updating = false;
        }

        public bool CheckIfMetal()
        {
            if (Model != null && Model._autoMetal)
            {
                if (!_updating)
                {
                    if (isMetal)
                        if (MessageBox.Show(null, "This model is currently set to automatically modify metal materials.\nYou cannot make changes unless you turn it off.\nDo you want to turn it off?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            Model._autoMetal = false;
                        else
                            return true;
                }
            }

            SignalPropertyChange();
            return false;
        }

        [Browsable(false)]
        public bool isMetal { get { return Name.EndsWith("_ExtMtl"); } }

        [Browsable(false)]
        public MDL0MaterialNode MetalMaterial
        {
            get
            {
                foreach (MDL0MaterialNode t in Model._matList)
                {
                    if (!isMetal)
                    {
                        if (t.Name.StartsWith(Name) && t.isMetal && (Name.Length + 7 == t.Name.Length))
                            return t;
                    }
                    else if (Name.StartsWith(t.Name) && !t.isMetal && (t.Name.Length + 7 == Name.Length)) return t;
                }
                return null;
            }
        }

        #endregion

        #region Reading & Writing
        internal int _initVersion;
        public override bool OnInitialize()
        {
            MDL0Material* header = Header;

            _initVersion = header->_pad != 0 && _replaced ? header->_pad : Model._version;

            if ((_name == null) && (header->_stringOffset != 0))
                _name = header->ResourceString;

            //Get XF Commands
            XFCmds = XFData.Parse((byte*)header->DisplayLists(_initVersion) + 0xE0);

            _usageFlags = header->_usageFlags;

            _indirectMethod1 = header->_indirectMethod1;
            _indirectMethod2 = header->_indirectMethod2;
            _indirectMethod3 = header->_indirectMethod3;
            _indirectMethod4 = header->_indirectMethod4;
            
            _normMapRefLight1 = header->_normMapRefLight1;
            _normMapRefLight2 = header->_normMapRefLight2;
            _normMapRefLight3 = header->_normMapRefLight3;
            _normMapRefLight4 = header->_normMapRefLight4;

            _activeStages = header->_activeTEVStages;
            _activeIndStages = header->_numIndTexStages;
            _zCompLoc = header->_enableAlphaTest;

            _lightSetIndex = header->_lightSet;
            _fogIndex = header->_fogSet;

            _cull = (CullMode)(int)header->_cull;

            if (!_replaced && (-header->_mdl0Offset + (int)header->DisplayListOffset(_initVersion)) % 0x20 != 0)
            {
                Model._errors.Add("Material " + Index + " has an improper align offset.");
                SignalPropertyChange();
            }

            MatModeBlock* mode = header->DisplayLists(_initVersion);
            _alphaFunc = mode->AlphaFunction;
            _zMode = mode->ZMode;
            _blendMode = mode->BlendMode;
            _constantAlpha = mode->ConstantAlpha;

            _tevColorBlock = *header->TevColorBlock(_initVersion);
            _tevKonstBlock = *header->TevKonstBlock(_initVersion);
            _indMtx = *header->IndMtxBlock(_initVersion);

            MDL0TexSRTData* TexMatrices = header->TexMatrices(_initVersion);
            _texMtxFlags = TexMatrices->_mtxFlags;

            MDL0MaterialLighting* Light = header->Light(_initVersion);

            (_chan1 = Light->Channel1)._parent = this;
            (_chan2 = Light->Channel2)._parent = this;

            int lightCount = header->_numLightChans;
            _chan1._enabled = lightCount != 0;
            _chan2._enabled = lightCount == 2;

            (_userEntries = new UserDataCollection()).Read(header->UserData(_initVersion));
            
            return true;
        }

        public override void OnPopulate()
        {
            MDL0TextureRef* first = Header->First;
            for (int i = 0; i < Header->_numTextures; i++)
                new MDL0MaterialRefNode().Initialize(this, first++, MDL0TextureRef.Size);
        }

        internal override void GetStrings(StringTable table)
        {
            table.Add(Name);

            foreach (UserDataClass s in _userEntries)
            {
                table.Add(s._name);
                if (s._type == UserValueType.String && s._entries.Count > 0)
                    table.Add(s._entries[0]);
            }

            foreach (MDL0MaterialRefNode n in Children)
                n.GetStrings(table);
        }

        internal int _dataAlign = 0, _mdlOffset = 0;
        public override int OnCalculateSize(bool force)
        {
            int temp, size;

            //Add header and tex matrices size at start
            size = 0x414 + (Model._version >= 10 ? 4 : 0);

            //Add children size
            size += Children.Count * MDL0TextureRef.Size;

            //Add user entries, if there are any
            size += _userEntries.GetSize();

            //Set temp align offset
            temp = size;

            //Align data to an offset divisible by 0x20 using data length.
            size = size.Align(0x10) + _dataAlign;
            if ((size + _mdlOffset) % 0x20 != 0)
                size += ((size - 0x10 >= temp) ? -0x10 : 0x10);

            //Reset data alignment
            _dataAlign = 0;

            //Add display list and XF flags
            size += 0x180;

            return size;
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            MDL0Node model = Model;
            MDL0Material* header = (MDL0Material*)address;
            
            //Set offsets
            header->_dataLen = length;

            int addr = 0x414, displayListOffset = length - 0x180;
            if (model._version >= 10)
            {
                addr += 4;

                header->_dataOffset2 = 0; //Fur Data not supported
                header->_dataOffset3 = displayListOffset;
            }
            else
                header->_dataOffset2 = displayListOffset;

            header->_matRefOffset = Children.Count > 0 ? addr : 0;

            //Check for user entries
            if (_userEntries.Count > 0)
            {
                addr += Children.Count * 0x34;
                if (model._version >= 10)
                    header->_dataOffset2 = addr;
                else
                    header->_dataOffset1 = addr;
                
                _userEntries.Write(header->UserData(model._version));
            }
            else
                header->_dataOffset1 = 0;

            ushort i1 = 0x1040, i2 = 0x1050, mtx = 0;
            if (Model._isImport)
            {
                //Set default texgen flags
                for (int i = 0; i < Children.Count; i++)
                {
                    MDL0MaterialRefNode node = ((MDL0MaterialRefNode)Children[i]);

                    //Tex Mtx
                    XFData dat = new XFData();
                    dat._addr = (XFMemoryAddr)i1++;
                    XFTexMtxInfo tex = new XFTexMtxInfo();
                    tex._data = (uint)(0 |
                        ((int)TexProjection.ST << 1) |
                        ((int)TexInputForm.AB11 << 2) |
                        ((int)TexTexgenType.Regular << 4) |
                        ((int)(0x5) << 7) |
                        (4 << 10) |
                        (2 << 13));
                    dat._values.Add(tex._data);
                    XFCmds.Add(dat);
                    node._texMtxFlags = tex;

                    //Dual Tex
                    dat = new XFData();
                    dat._addr = (XFMemoryAddr)i2++;
                    XFDualTex dtex = new XFDualTex(mtx, 0); mtx += 3;
                    dat._values.Add(dtex.Value);
                    XFCmds.Add(dat);
                    node._dualTexFlags = dtex;
                    node.GetTexMtxValues();
                    node._bindState = TextureFrameState.Neutral;
                    node._texMatrixEffect.TextureMatrix = Matrix43.Identity;
                    node._texMatrixEffect.SCNCamera = -1;
                    node._texMatrixEffect.SCNLight = -1;
                    node._texMatrixEffect.MapMode = MappingMethod.TexCoord;
                }
            }

            //Set header values
            header->_numTextures = Children.Count;
            header->_numTexGens = (byte)Children.Count;
            header->_index = Index;
            header->_activeTEVStages = (byte)_activeStages;
            header->_numIndTexStages = _activeIndStages;
            header->_enableAlphaTest = _zCompLoc;

            byte lights = 0;
            bool c1 = _chan1._enabled, c2 = _chan2._enabled;
            if (c2 && !c1)
            {
                //Swap channels. First must be enabled before second
                LightChannel temp = _chan1;
                _chan1 = _chan2;
                _chan2 = temp;
            }
            if (c1) lights++;
            if (c2) lights++;
            header->_numLightChans = lights;

            header->_lightSet = _lightSetIndex;
            header->_fogSet = _fogIndex;
            header->_pad = 0;

            header->_cull = (int)_cull;
            header->_usageFlags = _usageFlags;

            header->_indirectMethod1 = _indirectMethod1;
            header->_indirectMethod2 = _indirectMethod2;
            header->_indirectMethod3 = _indirectMethod3;
            header->_indirectMethod4 = _indirectMethod4;

            header->_normMapRefLight1 = _normMapRefLight1;
            header->_normMapRefLight2 = _normMapRefLight2;
            header->_normMapRefLight3 = _normMapRefLight3;
            header->_normMapRefLight4 = _normMapRefLight4;

            //Generate layer flags and write texture matrices
            MDL0TexSRTData* TexSettings = header->TexMatrices(model._version);
            *TexSettings = MDL0TexSRTData.Default;

            //Usage flags. Each set of 4 bits represents one texture layer.
            uint layerFlags = 0; 

            //Loop through references in reverse so that layerflags is set in the right order
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                MDL0MaterialRefNode node = (MDL0MaterialRefNode)Children[i];

                TexFlags flags = TexFlags.Enabled;

                //Check for non-default values
                if (node._bindState.Scale == new Vector2(1))
                    flags |= TexFlags.FixedScale;
                if (node._bindState.Rotate == 0)
                    flags |= TexFlags.FixedRot;
                if (node._bindState.Translate == new Vector2(0))
                    flags |= TexFlags.FixedTrans;

                TexSettings->SetTexSRT(node._bindState, node.Index);
                TexSettings->SetTexMatrices(node._texMatrixEffect, node.Index);

                layerFlags = ((layerFlags << 4) | (byte)flags);
            }

            TexSettings->_layerFlags = layerFlags;
            TexSettings->_mtxFlags = _texMtxFlags;

            //Write lighting flags
            MDL0MaterialLighting* Light = header->Light(model._version);

            Light->Channel1 = _chan1;
            Light->Channel2 = _chan2;

            //The shader offset will be written later

            //Rebuild references
            MDL0TextureRef* mRefs = header->First;
            foreach (MDL0MaterialRefNode n in Children)
                n.Rebuild(mRefs++, MDL0TextureRef.Size, true);
            
            //Set Display Lists
            *header->TevKonstBlock(model._version) = _tevKonstBlock;
            *header->TevColorBlock(model._version) = _tevColorBlock;
            *header->IndMtxBlock(model._version) = _indMtx;

            MatModeBlock* mode = header->DisplayLists(model._version);
            *mode = MatModeBlock.Default;
            if (model._isImport)
            {
                _alphaFunc = mode->AlphaFunction;
                _zMode = mode->ZMode;
                _blendMode = mode->BlendMode;
                _constantAlpha = mode->ConstantAlpha;
            }
            else
            {
                mode->AlphaFunction = _alphaFunc;
                mode->ZMode = _zMode;
                mode->BlendMode = _blendMode;
                mode->ConstantAlpha = _constantAlpha;
            }

            //Write XF flags
            i1 = 0x1040; i2 = 0x1050;  mtx = 0;
            byte* xfData = (byte*)header->DisplayLists(model._version) + 0xE0;
            foreach (MDL0MaterialRefNode mr in Children)
            {
                //Tex Mtx
                *xfData++ = 0x10;
                *(bushort*)xfData = 0; xfData += 2;
                *(bushort*)xfData = (ushort)i1++;  xfData += 2;
                *(buint*)xfData = mr._texMtxFlags._data; xfData += 4;

                //Dual Tex
                *xfData++ = 0x10;
                *(bushort*)xfData = 0; xfData += 2;
                *(bushort*)xfData = (ushort)i2++; xfData += 2;
                *(buint*)xfData = new XFDualTex(mtx, mr._dualTexFlags._normalEnable).Value; xfData += 4;
                
                mtx += 3;
            }
        }
        protected internal override void PostProcess(VoidPtr mdlAddress, VoidPtr dataAddress, StringTable stringTable)
        {
            MDL0Material* header = (MDL0Material*)dataAddress;
            header->_mdl0Offset = (int)mdlAddress - (int)dataAddress;
            header->_stringOffset = (int)stringTable[Name] + 4 - (int)dataAddress;
            header->_index = Index;

            _userEntries.PostProcess(header->UserData(Model._version), stringTable);

            MDL0TextureRef* first = header->First;
            foreach (MDL0MaterialRefNode n in Children)
                n.PostProcess(mdlAddress, first++, stringTable);
        }
        public override unsafe void Export(string outPath)
        {
            StringTable table = new StringTable();
            GetStrings(table);
            int dataLen = CalculateSize(false);
            int totalLen = dataLen + table.GetTotalSize();

            using (FileStream stream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 8, FileOptions.RandomAccess))
            {
                stream.SetLength(totalLen);
                using (FileMap map = FileMap.FromStream(stream))
                {
                    Rebuild(map.Address, dataLen, false);
                    table.WriteTable(map.Address + dataLen);
                    PostProcess(map.Address, map.Address, table);
                    ((MDL0Material*)map.Address)->_pad = (byte)Model._version;
                }
            }
        }
        #endregion

        #region Rendering

        public bool _renderUpdate = false;
        public new void SignalPropertyChange()
        {
            _renderUpdate = true;
            base.SignalPropertyChange();
        }

        public bool written = false;

        public SCN0FogNode _fog;
        public SCN0LightSetNode _lightSet;
        public int _animFrame;

        public void Render()
        {
            foreach (MDL0ObjectNode p in _objects)
                p.Render(false);

            #region Old

            //if (Model._mainWindow != null && Model._mainWindow._scn0 != null && (LightSet >= 0 || FogSet >= 0))
            //{
            //    ModelEditControl m = Model._mainWindow;
            //    SCN0Node scn = m._scn0;
            //    int animFrame = m._animFrame;
            //    SCN0GroupNode fog;
            //    if (FogSet >= 0 && (fog = scn.GetFolder<SCN0FogNode>()) != null && fog.Children.Count > FogSet)
            //    {
            //        SCN0FogNode f = fog.Children[FogSet] as SCN0FogNode;
            //        GL.Enable(EnableCap.Fog);
            //        uint mode = 0;
            //        switch (f.Type)
            //        {
            //            case FogType.OrthographicExp:
            //            case FogType.PerspectiveExp:
            //                mode = (uint)OpenTK.Graphics.OpenGL.FogMode.Exp;
            //                break;
            //            case FogType.OrthographicExp2:
            //            case FogType.PerspectiveExp2:
            //                mode = (uint)OpenTK.Graphics.OpenGL.FogMode.Exp2;
            //                break;
            //            case FogType.OrthographicLinear:
            //            case FogType.PerspectiveLinear:
            //                mode = (uint)OpenTK.Graphics.OpenGL.FogMode.Linear;
            //                break;
            //            case FogType.OrthographicRevExp:
            //            case FogType.PerspectiveRevExp:
            //                mode = (uint)OpenTK.Graphics.OpenGL.FogMode.Linear;
            //                break;
            //            case FogType.OrthographicRevExp2:
            //            case FogType.PerspectiveRevExp2:
            //                mode = (uint)OpenTK.Graphics.OpenGL.FogMode.Linear;
            //                break;
            //        }
            //        GL.Fog(OpenTK.Graphics.OpenGL.FogParameter.FogMode, mode);
            //        float* l = stackalloc float[4];
            //        if (f.Colors.Count == 1)
            //        {
            //            l[0] = (float)f.Colors[0].R / 255f;
            //            l[1] = (float)f.Colors[0].G / 255f;
            //            l[2] = (float)f.Colors[0].B / 255f;
            //            l[3] = (float)f.Colors[0].A / 255f;
            //        }
            //        else if (animFrame - 1 < f.Colors.Count)
            //        {
            //            l[0] = (float)f.Colors[animFrame - 1].R / 255f;
            //            l[1] = (float)f.Colors[animFrame - 1].G / 255f;
            //            l[2] = (float)f.Colors[animFrame - 1].B / 255f;
            //            l[3] = (float)f.Colors[animFrame - 1].A / 255f;
            //        }
            //        GL.Fog(OpenTK.Graphics.OpenGL.FogParameter.FogColor, l);
            //        //ctx.glFog(FogParameter.FogDensity, 0.05f);
            //        GL.Hint(HintTarget.FogHint, HintMode.Nicest);
            //        GL.Fog(OpenTK.Graphics.OpenGL.FogParameter.FogStart, f._startKeys.GetFrameValue(animFrame - 1));
            //        GL.Fog(OpenTK.Graphics.OpenGL.FogParameter.FogEnd, f._endKeys.GetFrameValue(animFrame - 1));
            //    }
            //    else
            //        GL.Disable(EnableCap.Fog);
            //}
            //else
            //    GL.Disable(EnableCap.Fog);

            #endregion
        }

        internal override void Bind()
        {
            _renderUpdate = true;
        }

        internal override void Unbind() 
        {
            foreach (MDL0MaterialRefNode m in Children) 
                m.Unbind();
        }

        public TextureFrameState[] _indirectFrameStates = new TextureFrameState[3];

        internal void ApplySRT0(SRT0Node node, float index)
        {
            SRT0EntryNode e;

            if (node == null || index < 1)
                foreach (MDL0MaterialRefNode r in Children)
                    r.ApplySRT0Texture(null);
            else if ((e = node.FindChild(Name, false) as SRT0EntryNode) != null)
                foreach (SRT0TextureNode t in e.Children)
                {
                    if (!t.Indirect)
                    {
                        if (t._textureIndex < Children.Count)
                            ((MDL0MaterialRefNode)Children[t._textureIndex]).ApplySRT0Texture(t, index, node.MatrixMode);
                    }
                    else if (t._textureIndex < _indirectFrameStates.Length)
                    {
                        fixed (TextureFrameState* state = &_indirectFrameStates[t._textureIndex])
                            if (node != null && index >= 1)
                            {
                                float* f = (float*)state;
                                for (int i = 0; i < 5; i++)
                                    if (t.Keyframes[i]._keyCount > 0)
                                        f[i] = t.GetFrameValue(i, index - 1);
                                state->MatrixMode = node.MatrixMode;
                                state->CalcTransforms();
                            }
                            else
                                *state = TextureFrameState.Neutral;
                    }
                }
            else
                foreach (MDL0MaterialRefNode r in Children)
                    r.ApplySRT0Texture(null);
        }

        //Use these for streaming values into the shader
        public RGBAPixel amb1, amb2, clr1, clr2;
        public GXColorS10 k1, k2, k3, k4, c1, c2, c3;

        internal void ApplyCLR0(CLR0Node node, float index)
        {
            if (node == null || index < 1)
            {
                clr1 = C1MaterialColor;
                clr2 = C2MaterialColor;
                amb1 = C1AmbientColor;
                amb2 = C2AmbientColor;
                c1 = CReg0Color;
                c2 = CReg1Color;
                c3 = CReg2Color;
                k1 = KReg0Color;
                k2 = KReg1Color;
                k3 = KReg2Color;
                k4 = KReg3Color;
                return;
            }

            CLR0MaterialNode mat = node.FindChild(Name, false) as CLR0MaterialNode;
            if (mat != null)
                foreach (CLR0MaterialEntryNode e in mat.Children)
                {
                    ARGBPixel p = e.Colors.Count > index ? e.Colors[(int)index] : new ARGBPixel();
                    switch (e.Target)
                    {
                        case EntryTarget.Ambient0: amb1 = (RGBAPixel)p; break;
                        case EntryTarget.Ambient1: amb2 = (RGBAPixel)p; break;
                        case EntryTarget.Color0: clr1 = (RGBAPixel)p; break;
                        case EntryTarget.Color1: clr2 = (RGBAPixel)p; break;
                        case EntryTarget.TevColorReg0: c1 = (GXColorS10)p; break;
                        case EntryTarget.TevColorReg1: c2 = (GXColorS10)p; break;
                        case EntryTarget.TevColorReg2: c3 = (GXColorS10)p; break;
                        case EntryTarget.TevKonstReg0: k1 = (GXColorS10)p; break;
                        case EntryTarget.TevKonstReg1: k2 = (GXColorS10)p; break;
                        case EntryTarget.TevKonstReg2: k3 = (GXColorS10)p; break;
                        case EntryTarget.TevKonstReg3: k4 = (GXColorS10)p; break;
                    }
                }
        }

        internal unsafe void ApplyPAT0(PAT0Node node, float index)
        {
            PAT0EntryNode e;

            if (node == null || index < 1)
                foreach (MDL0MaterialRefNode r in Children)
                    r.ApplyPAT0Texture(null, 0);
            else if ((e = node.FindChild(Name, false) as PAT0EntryNode) != null)
            {
                foreach (PAT0TextureNode t in e.Children)
                    if (t._textureIndex < Children.Count)
                        ((MDL0MaterialRefNode)Children[t._textureIndex]).ApplyPAT0Texture(t, index);
            }
            else
                foreach (MDL0MaterialRefNode r in Children)
                    r.ApplyPAT0Texture(null, 0);
        }
        public float _renderFrame = 0;
        internal unsafe void ApplySCN(SCN0Node node, float index)
        {
            _renderFrame = index;
            if (node == null)
            {
                _lightSet = null;
                _fog = null;
            }
            else
            {
                SCN0GroupNode g = node.GetFolder<SCN0LightSetNode>();
                _lightSet = LightSetIndex < g.Children.Count && LightSetIndex >= 0 ? g.Children[LightSetIndex] as SCN0LightSetNode : null;
                g = node.GetFolder<SCN0FogNode>();
                _fog = FogIndex < g.Children.Count && FogIndex >= 0 ? g.Children[FogIndex] as SCN0FogNode : null;
            }
        }

        #endregion

        public override unsafe void Replace(string fileName)
        {
            base.Replace(fileName);

            Model.CheckTextures();
        }

        public override void Remove()
        {
            if (Parent != null)
            {
                ShaderNode = null;

                foreach (MDL0MaterialRefNode r in Children)
                {
                    r.TextureNode = null;
                    r.PaletteNode = null;
                }

                Model.CheckTextures();
            }
            base.Remove();
        }

        public override void RemoveChild(ResourceNode child)
        {
            base.RemoveChild(child);

            if (!_updating && Model._autoMetal && MetalMaterial != null && !this.isMetal)
                MetalMaterial.UpdateAsMetal();
        }
    }

    #region Light Channel Info

    public class LightChannel
    {
        public MDL0MaterialNode _parent = null;

        public LightChannel(bool enabled, uint flags, RGBAPixel mat, RGBAPixel amb, uint color, uint alpha, MDL0MaterialNode material) : this(enabled, flags, mat, amb, color, alpha) { _parent = material; }
        public LightChannel(bool enabled, uint flags, RGBAPixel mat, RGBAPixel amb, uint color, uint alpha)
        {
            _enabled = enabled;
            _flags = flags;
            _matColor = mat;
            _ambColor = amb;
            _color = new LightChannelControl(color) { _parent = this };
            _alpha = new LightChannelControl(alpha) { _parent = this };
        }

        [Category("Lighting Channel")]
        public bool Enabled { get { return _enabled; } set { _enabled = value; _parent.SignalPropertyChange(); } }

        [Category("Lighting Channel")]
        public LightingChannelFlags Flags { get { return (LightingChannelFlags)_flags; } set { _flags = (uint)value; _parent.SignalPropertyChange(); } }

        [Category("Lighting Channel"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel MaterialColor { get { return _matColor; } set { _matColor = value; _parent.SignalPropertyChange(); } }

        [Category("Lighting Channel"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel AmbientColor { get { return _ambColor; } set { _ambColor = value; _parent.SignalPropertyChange(); } }

        [Category("Lighting Channel"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public LightChannelControl Color
        {
            get { return _color; }
            set { _color = value; _color._parent = this; _parent.SignalPropertyChange(); }
        }

        [Category("Lighting Channel"), TypeConverter(typeof(ExpandableObjectCustomConverter))]
        public LightChannelControl Alpha
        {
            get { return _alpha; }
            set { _alpha = value; _alpha._parent = this; _parent.SignalPropertyChange(); }
        }

        [Category("Lighting Channel"), Browsable(false)]
        public GXColorSrc ColorMaterialSource
        {
            get { return _color.MaterialSource; }
            set { _color.MaterialSource = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public bool ColorEnabled
        {
            get { return _color.Enabled; }
            set { _color.Enabled = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXColorSrc ColorAmbientSource
        {
            get { return _color.AmbientSource; }
            set { _color.AmbientSource = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXDiffuseFn ColorDiffuseFunction
        {
            get { return _color.DiffuseFunction; }
            set { _color.DiffuseFunction = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXAttnFn ColorAttenuation
        {
            get { return _color.Attenuation; }
            set { _color.Attenuation = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public MatChanLights ColorLights
        {
            get { return _color.Lights; }
            set { _color.Lights = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXColorSrc AlphaMaterialSource
        {
            get { return _alpha.MaterialSource; }
            set { _alpha.MaterialSource = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public bool AlphaEnabled
        {
            get { return _alpha.Enabled; }
            set { _alpha.Enabled = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXColorSrc AlphaAmbientSource
        {
            get { return _alpha.AmbientSource; }
            set { _alpha.AmbientSource = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXDiffuseFn AlphaDiffuseFunction
        {
            get { return _alpha.DiffuseFunction; }
            set { _alpha.DiffuseFunction = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public GXAttnFn AlphaAttenuation
        {
            get { return _alpha.Attenuation; }
            set { _alpha.Attenuation = value; _parent.SignalPropertyChange(); }
        }
        [Category("Lighting Channel"), Browsable(false)]
        public MatChanLights AlphaLights
        {
            get { return _alpha.Lights; }
            set { _alpha.Lights = value; _parent.SignalPropertyChange(); }
        }

        public bool _enabled;
        public uint _flags;
        public RGBAPixel _matColor, _ambColor;
        public LightChannelControl _color, _alpha;
    }

    public enum GXColorSrc
    {
        Register,
        Vertex
    }

    [Flags]
    public enum MatChanLights
    {
        None = 0x0,
        Light0 = 0x1,
        Light1 = 0x2,
        Light2 = 0x4,
        Light3 = 0x8,
        Light4 = 0x10,
        Light5 = 0x20,
        Light6 = 0x40,
        Light7 = 0x80,
    }

    public enum GXDiffuseFn
    {
        Disabled,
        Enabled,
        Clamped
    }

    public enum GXAttnFn
    {
        Specular,
        Spotlight,
        None
    }

    [Flags]
    public enum LightingChannelFlags
    {
        UseMatColor = 0x1,
        UseMatAlpha = 0x2,
        UseAmbColor = 0x4,
        UseAmbAlpha = 0x8,
        UseChanColor = 0x10,
        UseChanAlpha = 0x20
    }

    public class LightChannelControl
    {
        public LightChannel _parent;

        public LightChannelControl(uint ctrl) { _binary = new Bin32(ctrl); }

        public Bin32 _binary;

        //0000 0000 0000 0000 0000 0000 0000 0001   Material Source (GXColorSrc)
        //0000 0000 0000 0000 0000 0000 0000 0010   Light Enabled
        //0000 0000 0000 0000 0000 0000 0011 1100   Light 0123
        //0000 0000 0000 0000 0000 0000 0100 0000   Ambient Source (GXColorSrc)
        //0000 0000 0000 0000 0000 0001 1000 0000   Diffuse Func
        //0000 0000 0000 0000 0000 0010 0000 0000   Attenuation Enable
        //0000 0000 0000 0000 0000 0100 0000 0000   Attenuation Function (0 = Specular)
        //0000 0000 0000 0000 0111 1000 0000 0000   Light 4567

        [Category("Lighting Control")]
        public bool Enabled { get { return _binary[1]; } set { _binary[1] = value; if (_parent != null) _parent._parent.SignalPropertyChange(); } }
        [Category("Lighting Control")]
        public GXColorSrc MaterialSource { get { return (GXColorSrc)(_binary[0] ? 1 : 0); } set { _binary[0] = ((int)value != 0); if (_parent != null) _parent._parent.SignalPropertyChange(); } }
        [Category("Lighting Control")]
        public GXColorSrc AmbientSource { get { return (GXColorSrc)(_binary[6] ? 1 : 0); } set { _binary[6] = ((int)value != 0); if (_parent != null) _parent._parent.SignalPropertyChange(); } }
        [Category("Lighting Control")]
        public GXDiffuseFn DiffuseFunction { get { return (GXDiffuseFn)(_binary[7, 2]); } set { _binary[7, 2] = ((uint)value); if (_parent != null) _parent._parent.SignalPropertyChange(); } }
        [Category("Lighting Control")]
        public GXAttnFn Attenuation
        {
            get
            {
                if (!_binary[9])
                    return GXAttnFn.None;
                else
                    return (GXAttnFn)(_binary[10] ? 1 : 0);
            }
            set
            {
                if (value != GXAttnFn.None)
                {
                    _binary[9] = true;
                    _binary[10] = ((int)value) != 0;
                }
                else
                {
                    _binary[9] = false;
                    _binary[10] = false;
                }

                if (_parent != null)
                    _parent._parent.SignalPropertyChange();
            }
        }
        [Category("Lighting Control")]
        public MatChanLights Lights
        {
            get { return (MatChanLights)(_binary[11, 4] | (_binary[2, 4] << 4)); }
            set
            {
                uint val = (uint)value;
                _binary[11, 4] = (val & 0xF);
                _binary[2, 4] = ((val >> 4) & 0xF);

                if (_parent != null)
                    _parent._parent.SignalPropertyChange();
            }
        }
    }
    #endregion
}
