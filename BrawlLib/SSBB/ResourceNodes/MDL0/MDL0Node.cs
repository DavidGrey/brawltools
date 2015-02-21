﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using BrawlLib.SSBBTypes;
using BrawlLib.OpenGL;
using System.IO;
using BrawlLib.IO;
using BrawlLib.Imaging;
using BrawlLib.Modeling;
using BrawlLib.Wii.Models;
using BrawlLib.Wii.Animations;
using System.Windows.Forms;
using BrawlLib.Wii.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class MDL0Node : BRESEntryNode, IModel
    {
        internal MDL0Header* Header { get { return (MDL0Header*)WorkingUncompressed.Address; } }
        public override ResourceType ResourceType { get { return ResourceType.MDL0; } }
        public override int DataAlign { get { return 0x20; } }
        public override int[] SupportedVersions { get { return new int[] { 8, 9, 10, 11 }; } }

        public MDL0Node() { _version = 9; _linker = ModelLinker.Prepare(this); }

        #region Variables and Attributes

        internal int _scalingRule, _texMtxMode;
        public byte _envMtxMode;
        public bool _needsNrmMtxArray, _needsTexMtxArray, _enableExtents;
        public int _numFacepoints, _numTriangles, _numNodes;
        public Box _extents;
        
        public ModelLinker _linker;
        public AssetStorage _assets;
        public bool _hasTree, _hasMix, _hasOpa, _hasXlu, _isImport, _autoMetal;

        public List<MDL0BoneNode> _billboardBones = new List<MDL0BoneNode>();
        public InfluenceManager _influences = new InfluenceManager();
        public List<string> _errors = new List<string>();

        internal const string _textureMatrixModeDescription = @"";

        [Browsable(false)]
        public InfluenceManager Influences { get { return _influences; } }

        [Browsable(true), Description(
@"This feature is for Super Smash Bros Brawl models specifically.
When true, metal materials and shaders will be added and modulated as you edit your own custom materials and shaders.")]
        public bool AutoMetalMaterials { get { return _autoMetal; } set { _autoMetal = value; CheckMetals(); } }

        [Category("G3D Model")]
        public MDLScalingRule ScalingRule { get { return (MDLScalingRule)_scalingRule; } set { _scalingRule = (int)value; SignalPropertyChange(); } }
        [Category("G3D Model"), Description(_textureMatrixModeDescription)]
        public TexMatrixMode TextureMatrixMode { get { return (TexMatrixMode)_texMtxMode; } set { _texMtxMode = (int)value; SignalPropertyChange(); } }
        [Category("G3D Model"), Description("How many points are stored in the model file and sent to the GPU every frame. A lower value is better.")]
        public int NumFacepoints { get { return _numFacepoints; } }
        [Category("G3D Model"), Description("How many individual vertices this model has. A vertex in this case is only a point in space with its associated influence.")]
        public int NumVertices
        {
            get
            {
                if (_objList == null)
                    return 0;

                int i = 0;
                foreach (MDL0ObjectNode n in _objList)
                    i += n.VertexCount;
                return i;
            }
        }

        [Category("G3D Model"), Description("The number of individual triangle faces this model has.")]
        public int NumTriangles { get { return _numTriangles; } }
        [Category("G3D Model"), Description("The number of matrices used in this model (bones + weighted influences).")]
        public int NumNodes { get { return _numNodes; } }
        
        protected override void OnVersionChanged(int previousVersion)
        {
            bool
                convertingDown = previousVersion > 9 && _version <= 9,
                convertingUp = previousVersion <= 9 && _version > 9;

            if (convertingDown)
            {
                //TODO: alert user to information that will be lost after converting down to v8 or v9,  saving and closing.
                //No need to allow cancelling the version change here, as the user can simply change the version back before saving and closing.
            }

            //Be sure the model is populated so that the object list is filled
            if (_children == null)
                Populate(0);

            //Version 10 and 11 objects are slighly different from 8 and 9
            if (_objList != null && (convertingDown || convertingUp))
                foreach (MDL0ObjectNode o in _objList)
                    o._rebuild = true;
        }

        [Category("G3D Model"), Description("True when one or more objects has normals and is rigged to more than one influence (the object's single bind property says '(none)').")]
        public bool NeedsNormalMtxArray { get { return _needsNrmMtxArray; } }
        [Category("G3D Model"), Description("True when one or more objects has a texture matrix turned on and is rigged to more than one influence (the object's single bind property says '(none)').")]
        public bool NeedsTextureMtxArray { get { return _needsTexMtxArray; } }
        [Category("G3D Model"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 BoxMin { get { return _extents.Min; } set { _extents.Min = value; SignalPropertyChange(); } }
        [Category("G3D Model"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 BoxMax { get { return _extents.Max; } set { _extents.Max = value; SignalPropertyChange(); } }
        [Category("G3D Model")]
        public bool EnableBoundingBox { get { return _enableExtents; } set { _enableExtents = value; SignalPropertyChange(); } }
        [Category("G3D Model")]
        public MDLEnvelopeMatrixMode EnvelopeMatrixMode { get { return (MDLEnvelopeMatrixMode)_envMtxMode; } set { _envMtxMode = (byte)value; SignalPropertyChange(); } }

        #endregion

        #region Immediate accessors

        public MDL0GroupNode _boneGroup, _matGroup, _shadGroup, _objGroup, _texGroup, _pltGroup, _vertGroup, _normGroup, _uvGroup, _defGroup, _colorGroup, _furPosGroup, _furVecGroup;
        public List<ResourceNode> _boneList, _matList, _shadList, _objList, _texList, _pltList, _vertList, _normList, _uvList, _defList, _colorList, _furPosList, _furVecList;
        
        [Browsable(false)]
        public ResourceNode DefinitionsGroup { get { return _defGroup; } }
        [Browsable(false)]
        public ResourceNode BoneGroup { get { return _boneGroup; } }
        [Browsable(false)]
        public ResourceNode MaterialGroup { get { return _matGroup; } }
        [Browsable(false)]
        public ResourceNode ShaderGroup { get { return _shadGroup; } }
        [Browsable(false)]
        public ResourceNode VertexGroup { get { return _vertGroup; } }
        [Browsable(false)]
        public ResourceNode NormalGroup { get { return _normGroup; } }
        [Browsable(false)]
        public ResourceNode UVGroup { get { return _uvGroup; } }
        [Browsable(false)]
        public ResourceNode ColorGroup { get { return _colorGroup; } }
        [Browsable(false)]
        public ResourceNode PolygonGroup { get { return _objGroup; } }
        [Browsable(false)]
        public ResourceNode TextureGroup { get { return _texGroup; } }
        [Browsable(false)]
        public ResourceNode PaletteGroup { get { return _pltGroup; } }
        [Browsable(false)]
        public ResourceNode FurVecGroup { get { return _furVecGroup; } }
        [Browsable(false)]
        public ResourceNode FurPosGroup { get { return _furPosGroup; } }

        [Browsable(false)]
        public List<ResourceNode> DefinitionsList { get { return _defList; } }
        [Browsable(false)]
        public List<ResourceNode> BoneList { get { return _boneList; } }
        [Browsable(false)]
        public List<ResourceNode> MaterialList { get { return _matList; } }
        [Browsable(false)]
        public List<ResourceNode> ShaderList { get { return _shadList; } }
        [Browsable(false)]
        public List<ResourceNode> VertexList { get { return _vertList; } }
        [Browsable(false)]
        public List<ResourceNode> NormalList { get { return _normList; } }
        [Browsable(false)]
        public List<ResourceNode> UVList { get { return _uvList; } }
        [Browsable(false)]
        public List<ResourceNode> ColorList { get { return _colorList; } }
        [Browsable(false)]
        public List<ResourceNode> PolygonList { get { return _objList; } }
        [Browsable(false)]
        public List<ResourceNode> TextureList { get { return _texList; } }
        [Browsable(false)]
        public List<ResourceNode> PaletteList { get { return _pltList; } }
        [Browsable(false)]
        public List<ResourceNode> FurVecList { get { return _colorList; } }
        [Browsable(false)]
        public List<ResourceNode> FurPosList { get { return _colorList; } }
        #endregion

        #region Functions

        /// <summary>
        /// Call ApplyCHR0 before calling this
        /// </summary>
        public Box GetBox()
        {
            if (_objList == null)
                return new Box();

            Box box = Box.ExpandableVolume;
            foreach (MDL0ObjectNode o in _objList)
                if (o._manager != null && o._manager._vertices != null)
                    foreach (Vertex3 vertex in o._manager._vertices)
                        box.ExpandVolume(vertex.WeightedPosition);

            return box;
        }

        /// <summary>
        /// Does not signal a property change!
        /// </summary>
        public void CalculateBoundingBoxes()
        {
            ApplyCHR(null, 0);
            _extents = GetBox();
            if (_boneList != null)
                foreach (MDL0BoneNode b in _boneList)
                    b.SetBox();
        }

        public void RemoveBone(MDL0BoneNode bone)
        {
            foreach (MDL0BoneNode b in bone.Children)
                RemoveBone(b);

        //    _influences.RemoveBone(bone);
        //    foreach (MDL0ObjectNode o in _polyList)
        //        if (o.MatrixNode == bone)
        //            o.MatrixNode = bone.Parent as MDL0BoneNode;

        //Top:
        //    if (bone.References.Count != 0)
        //    {
        //        bone.References[bone.References.Count - 1].MatrixNode = bone.Parent as MDL0BoneNode;
        //        goto Top;
        //    }
        }

        public void CheckTextures()
        {
            if (_texList != null)
                foreach (MDL0TextureNode t in _texList)
                {
                    for (int i = 0; i < t._references.Count; i++)
                        if (t._references[i].Parent == null)
                            t._references.RemoveAt(i--);
                    if (t._references.Count == 0)
                        t.Remove();
                }
        }

        public List<ResourceNode> GetUsedShaders()
        {
            List<ResourceNode> shaders = new List<ResourceNode>();
            if (_shadList != null)
            foreach (MDL0ShaderNode s in _shadList)
                if (s._materials.Count > 0)
                    shaders.Add(s);
            return shaders;
        }

        public void CheckMetals()
        {
            if (_autoMetal)
            {
                if (MessageBox.Show(null, "Are you sure you want to turn this on?\nAny existing metal materials will be modified.", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (_children == null) Populate();
                    for (int x = 0; x < _matList.Count; x++)
                    {
                        MDL0MaterialNode n = (MDL0MaterialNode)_matList[x];
                        if (!n.isMetal)
                        {
                            if (n.MetalMaterial == null)
                            {
                                MDL0MaterialNode node = new MDL0MaterialNode();
                                _matGroup.AddChild(node);
                                node._updating = true;
                                node.Name = n.Name + "_ExtMtl";
                                node.SetImportValues();
                                node._activeStages = 4;

                                for (int i = 0; i <= n.Children.Count; i++)
                                {
                                    MDL0MaterialRefNode mr = new MDL0MaterialRefNode();
                                    node.AddChild(mr);
                                    mr.Texture = "metal00";
                                    mr._index1 = mr._index2 = i;
                                    mr.SignalPropertyChange();
                                    if (i == n.Children.Count || ((MDL0MaterialRefNode)n.Children[i]).HasTextureMatrix)
                                    {
                                        mr._minFltr = 5;
                                        mr._magFltr = 1;
                                        mr._lodBias = -2;

                                        mr.HasTextureMatrix = true;
                                        node.Rebuild(true);

                                        mr._projection = (int)TexProjection.STQ;
                                        mr._inputForm = (int)TexInputForm.ABC1;
                                        mr._texGenType = (int)TexTexgenType.Regular;
                                        mr._sourceRow = (int)TexSourceRow.Normals;
                                        mr._embossSource = 4;
                                        mr._embossLight = 2;
                                        mr.Normalize = true;

                                        mr.MapMode = MappingMethod.EnvCamera;

                                        mr.SetTextMtxData();

                                        break;
                                    }
                                }

                                node._chan1 = new LightChannel(true, 63, new RGBAPixel(128, 128, 128, 255), new RGBAPixel(255, 255, 255, 255), 0, 0, node);
                                node.C1ColorEnabled = true;
                                node.C1ColorDiffuseFunction = GXDiffuseFn.Clamped;
                                node.C1ColorAttenuation = GXAttnFn.Spotlight;
                                node.C1AlphaEnabled = true;
                                node.C1AlphaDiffuseFunction = GXDiffuseFn.Clamped;
                                node.C1AlphaAttenuation = GXAttnFn.Spotlight;

                                node._chan2 = new LightChannel(true, 63, new RGBAPixel(255, 255, 255, 255), new RGBAPixel(), 0, 0, node);
                                node.C2ColorEnabled = true;
                                node.C2ColorDiffuseFunction = GXDiffuseFn.Disabled;
                                node.C2ColorAttenuation = GXAttnFn.Specular;
                                node.C2AlphaDiffuseFunction = GXDiffuseFn.Disabled;
                                node.C2AlphaAttenuation = GXAttnFn.Specular;

                                node._lightSetIndex = n._lightSetIndex;
                                node._fogIndex = n._fogIndex;

                                node._cull = n._cull;
                                //node._numLights = 2;
                                node.CompareBeforeTexture = true;
                                node._normMapRefLight1 =
                                node._normMapRefLight2 =
                                node._normMapRefLight3 =
                                node._normMapRefLight4 = -1;

                                node.SignalPropertyChange();
                            }
                        }
                    }
                    foreach (MDL0MaterialNode node in _matList)
                    {
                        if (!node.isMetal)
                            continue;

                        if (node.ShaderNode != null)
                        {
                            if (node.ShaderNode._autoMetal && node.ShaderNode._texCount == node.Children.Count)
                            {
                                node._updating = false;
                                continue;
                            }
                            else
                            {
                                if (node.ShaderNode.Stages == 4)
                                {
                                    foreach (MDL0MaterialNode y in node.ShaderNode._materials)
                                        if (!y.isMetal || y.Children.Count != node.Children.Count)
                                            goto Next;
                                    node.ShaderNode.DefaultAsMetal(node.Children.Count);
                                    continue;
                                }
                            }
                        }
                    Next:
                        bool found = false;
                        foreach (MDL0ShaderNode s in _shadGroup.Children)
                        {
                            if (s._autoMetal && s._texCount == node.Children.Count)
                            {
                                node.ShaderNode = s;
                                found = true;
                            }
                            else
                            {
                                if (s.Stages == 4)
                                {
                                    foreach (MDL0MaterialNode y in s._materials)
                                        if (!y.isMetal || y.Children.Count != node.Children.Count)
                                            goto NotFound;
                                    node.ShaderNode = s;
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
                            _shadGroup.AddChild(shader);
                            shader.DefaultAsMetal(node.Children.Count);
                            node.ShaderNode = shader;
                        }
                    }
                    foreach (MDL0MaterialNode m in _matList)
                        m._updating = false;
                }
                else
                    _autoMetal = false;
            }
        }

        public void CleanTextures()
        {
            if (_texList != null)
            {
                int i = 0;
                while (i < _texList.Count)
                {
                    MDL0TextureNode texture = (MDL0TextureNode)_texList[i];

                at1:
                    foreach (MDL0MaterialRefNode r in texture._references)
                        if (_matList.IndexOf(r.Parent) == -1)
                        {
                            texture._references.Remove(r);
                            goto at1;
                        }

                    if (texture._references.Count == 0)
                        _texList.RemoveAt(i);
                    else
                        i++;
                }
            }

            if (_pltList != null)
            {
                int i = 0;
                while (i < _pltList.Count)
                {
                    MDL0TextureNode palette = (MDL0TextureNode)_pltList[i];

                bt1:
                    foreach (MDL0MaterialRefNode r in palette._references)
                        if (_matList.IndexOf(r.Parent) == -1)
                        {
                            palette._references.Remove(r);
                            goto bt1;
                        }

                    if (palette._references.Count == 0)
                        _pltList.RemoveAt(i);
                    else
                        i++;
                }
            }
        }

        public MDL0TextureNode FindOrCreateTexture(string name)
        {
            if (_texGroup == null)
                AddChild(_texGroup = new MDL0GroupNode(MDLResourceType.Textures), false);
            else
                foreach (MDL0TextureNode n in _texGroup.Children)
                    if (n._name == name)
                        return n;

            MDL0TextureNode node = new MDL0TextureNode(name);
            _texGroup.AddChild(node, false);

            return node;
        }
        public MDL0TextureNode FindOrCreatePalette(string name)
        {
            if (_pltGroup == null)
                AddChild(_pltGroup = new MDL0GroupNode(MDLResourceType.Palettes), false);
            else
                foreach (MDL0TextureNode n in _pltGroup.Children)
                    if (n._name == name)
                        return n;

            MDL0TextureNode node = new MDL0TextureNode(name);
            _pltGroup.AddChild(node, false);

            return node;
        }
        public MDL0BoneNode FindBone(string name)
        {
            foreach (MDL0BoneNode b in _linker.BoneCache)
                if (b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return b;

            return null;
        }
        public MDL0MaterialNode FindOrCreateOpaMaterial(string name)
        {
            foreach (MDL0MaterialNode m in _matList)
                if (m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !m.XLUMaterial)
                    return m;

            MDL0MaterialNode node = new MDL0MaterialNode() { _name = _matGroup.FindName(name) };
            _matGroup.AddChild(node, false);

            SignalPropertyChange();

            return node;
        }
        public MDL0MaterialNode FindOrCreateXluMaterial(string name)
        {
            foreach (MDL0MaterialNode m in _matList)
                if (m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && m.XLUMaterial)
                    return m;

            MDL0MaterialNode node = new MDL0MaterialNode() { _name = _matGroup.FindName(name), XLUMaterial = true };
            _matGroup.AddChild(node, false);

            SignalPropertyChange();

            return node;
        }
        public override void AddChild(ResourceNode child, bool change)
        {
            if (child is MDL0GroupNode)
                LinkGroup(child as MDL0GroupNode);
            base.AddChild(child, change);
        }

        public override void RemoveChild(ResourceNode child)
        {
            if (child is MDL0GroupNode)
                UnlinkGroup(child as MDL0GroupNode);
            base.RemoveChild(child);
        }

        #endregion

        #region Linking
        public void LinkGroup(MDL0GroupNode group)
        {
            switch (group._type)
            {
                case MDLResourceType.Definitions: { _defGroup = group; _defList = group._children; break; }
                case MDLResourceType.Bones: { _boneGroup = group; _boneList = group._children; break; }
                case MDLResourceType.Materials: { _matGroup = group; _matList = group._children; break; }
                case MDLResourceType.Shaders: { _shadGroup = group; _shadList = group._children; break; }
                case MDLResourceType.Vertices: { _vertGroup = group; _vertList = group._children; break; }
                case MDLResourceType.Normals: { _normGroup = group; _normList = group._children; break; }
                case MDLResourceType.UVs: { _uvGroup = group; _uvList = group._children; break; }
                case MDLResourceType.Colors: { _colorGroup = group; _colorList = group._children; break; }
                case MDLResourceType.Objects: { _objGroup = group; _objList = group._children; break; }
                case MDLResourceType.Textures: { _texGroup = group; _texList = group._children; break; }
                case MDLResourceType.Palettes: { _pltGroup = group; _pltList = group._children; break; }
                case MDLResourceType.FurLayerCoords: { _furPosGroup = group; _furPosList = group._children; break; }
                case MDLResourceType.FurVectors: { _furVecGroup = group; _furVecList = group._children; break; }
            }
        }
        public void UnlinkGroup(MDL0GroupNode group)
        {
            if (group != null)
            switch (group._type)
            {
                case MDLResourceType.Definitions: { _defGroup = null; _defList = null; break; }
                case MDLResourceType.Bones: { _boneGroup = null; _boneList = null; break; }
                case MDLResourceType.Materials: { _matGroup = null; _matList = null; break; }
                case MDLResourceType.Shaders: { _shadGroup = null; _shadList = null; break; }
                case MDLResourceType.Vertices: { _vertGroup = null; _vertList = null; break; }
                case MDLResourceType.Normals: { _normGroup = null; _normList = null; break; }
                case MDLResourceType.UVs: { _uvGroup = null; _uvList = null; break; }
                case MDLResourceType.Colors: { _colorGroup = null; _colorList = null; break; }
                case MDLResourceType.Objects: { _objGroup = null; _objList = null; break; }
                case MDLResourceType.Textures: { _texGroup = null; _texList = null; break; }
                case MDLResourceType.Palettes: { _pltGroup = null; _pltList = null; break; }
                case MDLResourceType.FurLayerCoords: { _furPosGroup = null; _furPosList = null; break; }
                case MDLResourceType.FurVectors: { _furVecGroup = null; _furVecList = null; break; }
            }
        }
        internal void InitGroups()
        {
            LinkGroup(new MDL0GroupNode(MDLResourceType.Definitions));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Bones));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Materials));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Shaders));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Vertices));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Normals));
            LinkGroup(new MDL0GroupNode(MDLResourceType.UVs));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Colors));
            LinkGroup(new MDL0GroupNode(MDLResourceType.FurVectors));
            LinkGroup(new MDL0GroupNode(MDLResourceType.FurLayerCoords));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Objects));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Textures));
            LinkGroup(new MDL0GroupNode(MDLResourceType.Palettes));

            _defGroup._parent = this;
            _boneGroup._parent = this;
            _matGroup._parent = this;
            _shadGroup._parent = this;
            _vertGroup._parent = this;
            _normGroup._parent = this;
            _uvGroup._parent = this;
            _colorGroup._parent = this;
            _furPosGroup._parent = this;
            _furVecGroup._parent = this;
            _objGroup._parent = this;
            _texGroup._parent = this;
            _pltGroup._parent = this;
        }
        internal void CleanGroups()
        {
            if (_defList.Count > 0)
                _children.Add(_defGroup);
            else
                UnlinkGroup(_defGroup);

            if (_boneList.Count > 0)
                _children.Add(_boneGroup);
            else
                UnlinkGroup(_boneGroup);

            if (_matList.Count > 0)
                _children.Add(_matGroup);
            else
                UnlinkGroup(_matGroup);

            if (_shadList.Count > 0)
                _children.Add(_shadGroup);
            else
                UnlinkGroup(_shadGroup);

            if (_vertList.Count > 0)
                _children.Add(_vertGroup);
            else
                UnlinkGroup(_vertGroup);

            if (_normList.Count > 0)
                _children.Add(_normGroup);
            else
                UnlinkGroup(_normGroup);

            if (_uvList.Count > 0)
                _children.Add(_uvGroup);
            else
                UnlinkGroup(_uvGroup);

            if (_colorList.Count > 0)
                _children.Add(_colorGroup);
            else
                UnlinkGroup(_colorGroup);

            if (_furPosList.Count > 0)
                _children.Add(_furPosGroup);
            else
                UnlinkGroup(_furPosGroup);

            if (_furVecList.Count > 0)
                _children.Add(_furVecGroup);
            else
                UnlinkGroup(_furVecGroup);

            if (_objList.Count > 0)
                _children.Add(_objGroup);
            else
                UnlinkGroup(_objGroup);

            if (_texList.Count > 0)
                _children.Add(_texGroup);
            else
                UnlinkGroup(_texGroup);

            if (_pltList.Count > 0)
                _children.Add(_pltGroup);
            else
                UnlinkGroup(_pltGroup);
        }
        #endregion

        #region Parsing

        public override bool OnInitialize()
        {
            base.OnInitialize();

            _billboardBones = new List<MDL0BoneNode>();
            _errors = new List<string>();
            _influences = new InfluenceManager();

            MDL0Header* header = Header;

            if (_name == null && header->StringOffset != 0)
                _name = header->ResourceString;

            MDL0Props* props = header->Properties;

            _scalingRule = props->_scalingRule;
            _texMtxMode = props->_texMatrixMode;
            _numFacepoints = props->_numVertices;
            _numTriangles = props->_numTriangles;
            _numNodes = props->_numNodes;
            _needsNrmMtxArray = props->_needNrmMtxArray != 0;
            _needsTexMtxArray = props->_needTexMtxArray != 0;
            _extents = props->_extents;
            _enableExtents = props->_enableExtents != 0;
            _envMtxMode = props->_envMtxMode;

            if (props->_origPathOffset > 0 && props->_origPathOffset < header->_header._size)
                _originalPath = props->OrigPath;

            (_userEntries = new UserDataCollection()).Read(header->UserData);

            return true;
        }

        public override void OnPopulate()
        {
            InitGroups();
            _linker = new ModelLinker(Header);
            _assets = new AssetStorage(_linker);
            try
            {
                //Set def flags
                _hasMix = _hasOpa = _hasTree = _hasXlu = false;
                if (_linker.Defs != null)
                    foreach (ResourcePair p in *_linker.Defs)
                        if (p.Name == "NodeTree") _hasTree = true;
                        else if (p.Name == "NodeMix") _hasMix = true;
                        else if (p.Name == "DrawOpa") _hasOpa = true;
                        else if (p.Name == "DrawXlu") _hasXlu = true;

                //These cause some complications if not parsed...
                _texGroup.Parse(this);
                _pltGroup.Parse(this);

                _defGroup.Parse(this);
                _boneGroup.Parse(this);
                _matGroup.Parse(this);
                _shadGroup.Parse(this);
                _vertGroup.Parse(this);
                _normGroup.Parse(this);
                _uvGroup.Parse(this);
                _colorGroup.Parse(this);

                if (Version >= 10)
                {
                    _furVecGroup.Parse(this);
                    _furPosGroup.Parse(this);
                }

                _objGroup.Parse(this); //Parse objects last!

                _texList.Sort();
                _pltList.Sort();
            }
            finally //Clean up!
            {
                //We'll use the linker to access the bone cache
                //_linker = null;

                //Don't dispose assets, in case an object is replaced
                //_assets.Dispose();
                //_assets = null;

                CleanGroups();

                //Check for model errors
                if (_errors.Count > 0)
                {
                    string message = _errors.Count + (_errors.Count > 1 ? " errors have" : " error has") + " been found in the model " + _name + ".\n" + (_errors.Count > 1 ? "These errors" : "This error") + " will be fixed when you save:";
                    foreach (string s in _errors)
                        message += "\n - " + s;
					if (!Properties.Settings.Default.HideMDL0Errors) MessageBox.Show(message);
                }
            }
        }

        public void BeginImport()
        {
            _isImport = true;
            InitGroups();
        }

        public void FinishImport(Collada form = null)
        {
            //Prepare for rebuild
            CleanTextures();
            CleanGroups();
            _influences.Clean();
            _influences.Sort();
            _linker = ModelLinker.Prepare(this);

            //Calculate size and align for string table later
            int size = (_calcSize = ModelEncoder.CalcSize(form, _linker)).Align(4);

            //Rebuild and write the model to a temp buffer with no string table
            UnsafeBuffer buffer = new UnsafeBuffer(size);
            ModelEncoder.Build(form, _linker, (MDL0Header*)buffer.Address, _calcSize, true);

            //Get strings AFTER calculating the size and rebuilding
            StringTable table = new StringTable();
            GetStrings(table);

            //Create temporary file map with the string table included
            FileMap uncompMap = FileMap.FromTempFile(size + table.GetTotalSize());

            //Set the sources
            _origSource = _uncompSource = new DataSource(uncompMap);

            //move the temp buffer to the end of the file map
            Memory.Move(uncompMap.Address, buffer.Address, (uint)buffer.Length);

            //Write the string table and do final calculations
            table.WriteTable(uncompMap.Address + size);
            PostProcess(null, uncompMap.Address, _calcSize, table);
            
            //Clear table and reset import bool
            table.Clear();
            _isImport = false;

            //Set replacement maps with the uncompressed map to force a reparse
            ReplaceRaw(uncompMap);
        }

        public static MDL0Node FromFile(string path)
        {
            //string ext = Path.GetExtension(path);
            if (path.EndsWith(".mdl0", StringComparison.OrdinalIgnoreCase))
                return NodeFactory.FromFile(null, path) as MDL0Node;
            else if (path.EndsWith(".dae", StringComparison.OrdinalIgnoreCase))
                return new Collada().ShowDialog(path, Collada.ImportType.MDL0) as MDL0Node;
            else if (path.EndsWith(".pmd", StringComparison.OrdinalIgnoreCase))
                return PMDModel.ImportModel(path);
            //else if (string.Equals(ext, "fbx", StringComparison.OrdinalIgnoreCase))
            //{
            //}
            //else if (string.Equals(ext, "blend", StringComparison.OrdinalIgnoreCase))
            //{
            //}

            throw new NotSupportedException("The file extension specified is not of a supported model type.");
        }

        #endregion

        #region Saving
        internal override void GetStrings(StringTable table)
        {
            table.Add(Name);
            foreach (MDL0GroupNode n in Children)
                n.GetStrings(table);

            _hasOpa = _hasXlu = false;

            //Can't use XLUMaterial bool in materials
            //because it's not guaranteed on when an object uses the material as XLU
            if (_objList != null)
                foreach (MDL0ObjectNode n in _objList)
                {
                    if (n.XluMaterialNode != null)
                        _hasXlu = true;
                    if (n.OpaMaterialNode != null)
                        _hasOpa = true;
                }

            //Add def names
            if (_hasTree) table.Add("NodeTree");
            if (_hasMix) table.Add("NodeMix");
            if (_hasOpa) table.Add("DrawOpa");
            if (_hasXlu) table.Add("DrawXlu");

            if (_version > 9)
                _userEntries.GetStrings(table);

            if (!String.IsNullOrEmpty(_originalPath))
                table.Add(_originalPath);
        }
        public override unsafe void Replace(string fileName, FileMapProtect prot, FileOptions options)
        {
            MDL0Node node = FromFile(fileName);
            if (node == null)
                return;

            int i = Index;
            Parent.InsertChild(node, true, i);
            Parent.SelectChildAtIndex(i);
            Remove();
            Dispose();
        }
        public override unsafe void Export(string outPath)
        {
            if (outPath.ToUpper().EndsWith(".DAE"))
                Collada.Serialize(this, outPath);
            //else if (outPath.ToUpper().EndsWith(".PMD"))
            //    PMDModel.Export(this, outPath);
            //else if (outPath.ToUpper().EndsWith(".RMDL"))
            //    XMLExporter.ExportRMDL(this, outPath);
            else
                base.Export(outPath);
        }
        public override int OnCalculateSize(bool force)
        {
            //Clean and sort influence list
            _influences.Clean();
            //_influences.Sort();

            //Clean texture list
            CleanTextures();

            _linker = ModelLinker.Prepare(this);
            return ModelEncoder.CalcSize(_linker);
        }
        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            ModelEncoder.Build(_linker, (MDL0Header*)address, length, force);
        }
        protected internal override void PostProcess(VoidPtr bresAddress, VoidPtr dataAddress, int dataLength, StringTable stringTable)
        {
            base.PostProcess(bresAddress, dataAddress, dataLength, stringTable);

            MDL0Header* header = (MDL0Header*)dataAddress;
            ResourceGroup* pGroup, sGroup;
            ResourceEntry* pEntry, sEntry;
            bint* offsets = header->Offsets;
            int index, sIndex;

            //Model name
            header->ResourceStringAddress = stringTable[Name] + 4;

            if (!String.IsNullOrEmpty(_originalPath))
                header->Properties->OrigPathAddress = stringTable[_originalPath] + 4;

            //Post-process groups, using linker lists
            List<MDLResourceType> gList = ModelLinker.IndexBank[_version];
            foreach (MDL0GroupNode node in Children)
            {
                MDLResourceType type = (MDLResourceType)Enum.Parse(typeof(MDLResourceType), node.Name);
                if ((index = gList.IndexOf(type)) >= 0 && type != MDLResourceType.Shaders)
                {
                    int offset = offsets[index];
                    if (offset > 0)
                        node.PostProcess(dataAddress, dataAddress + offset, stringTable);
                }
            }

            //Post-process definitions
            index = gList.IndexOf(MDLResourceType.Definitions);
            pGroup = (ResourceGroup*)(dataAddress + offsets[index]);
            pGroup->_first = new ResourceEntry(0xFFFF, 0, 0, 0);
            pEntry = &pGroup->_first + 1;
            index = 1;
            if (_hasTree)
                ResourceEntry.Build(pGroup, index++, (byte*)pGroup + (pEntry++)->_dataOffset, (BRESString*)stringTable["NodeTree"]);
            if (_hasMix)
                ResourceEntry.Build(pGroup, index++, (byte*)pGroup + (pEntry++)->_dataOffset, (BRESString*)stringTable["NodeMix"]);
            if (_hasOpa)
                ResourceEntry.Build(pGroup, index++, (byte*)pGroup + (pEntry++)->_dataOffset, (BRESString*)stringTable["DrawOpa"]);
            if (_hasXlu)
                ResourceEntry.Build(pGroup, index++, (byte*)pGroup + (pEntry++)->_dataOffset, (BRESString*)stringTable["DrawXlu"]);

            //Link shader names using material list
            index = offsets[gList.IndexOf(MDLResourceType.Materials)];
            sIndex = offsets[gList.IndexOf(MDLResourceType.Shaders)];
            if ((index > 0) && (sIndex > 0))
            {
                pGroup = (ResourceGroup*)(dataAddress + index);
                sGroup = (ResourceGroup*)(dataAddress + sIndex);
                pEntry = &pGroup->_first + 1;
                sEntry = &sGroup->_first + 1;

                sGroup->_first = new ResourceEntry(0xFFFF, 0, 0, 0);
                index = pGroup->_numEntries;
                for (int i = 1; i <= index; i++)
                {
                    VoidPtr dataAddr = (VoidPtr)sGroup + (sEntry++)->_dataOffset;
                    ResourceEntry.Build(sGroup, i, dataAddr, (BRESString*)((byte*)pGroup + (pEntry++)->_stringOffset - 4));
                    ((MDL0Shader*)dataAddr)->_mdl0Offset = (int)dataAddress - (int)dataAddr;
                }
            }
            
            //Write part2 entries
            if (Version > 9)
                _userEntries.PostProcess((VoidPtr)header + header->UserDataOffset, stringTable);
        }
        #endregion

        #region Rendering

        [Browsable(false)]
        public IBoneNode[] BoneCache
        {
            get
            {
                if (_linker != null && _linker.BoneCache != null)
                    return _linker.BoneCache.Select(x => x as IBoneNode).ToArray();
                return new IBoneNode[0];
            }
        }

        [Browsable(false)]
        public IBoneNode[] RootBones
        {
            get { return _boneList == null ? new IBoneNode[0] : _boneList.Select(x => x as IBoneNode).ToArray(); }
        }

        [Browsable(false)]
        public bool IsRendering { get { return _render; } set { _render = value; } }
        bool _render = true;

        [Browsable(false)]
        public bool IsTargetModel { get { return _isTargetModel; } set { _isTargetModel = value; } }
        bool _isTargetModel = false;

        public ModelRenderAttributes _renderAttribs = new ModelRenderAttributes();
        public bool _ignoreModelViewerAttribs = false;

        public int _selectedObjectIndex = -1;

        [Browsable(false)]
        public bool Attached { get { return _attached; } }
        private bool _attached = false;

        SHP0Node _currentSHP = null;
        float _currentSHPIndex = 0;

        public Dictionary<string, List<int>> VIS0Indices;

        public void Attach()
        {
            _attached = true;
            ResetToBindState();
            foreach (MDL0GroupNode g in Children)
                g.Bind();

            RegenerateVIS0Indices();
        }

        /// <summary>
        /// This only needs to be called when the model is
        /// currently attached to a model renderer and
        /// the amount of objects change or an object's visibility bone changes.
        /// </summary>
        public void RegenerateVIS0Indices()
        {
            int i = 0;
            VIS0Indices = new Dictionary<string, List<int>>();
            if (_objList != null)
                foreach (MDL0ObjectNode p in _objList)
                {
                    if (p._visBoneNode != null && p._visBoneNode.BoneIndex != 0)
                        if (!VIS0Indices.ContainsKey(p._visBoneNode.Name))
                            VIS0Indices.Add(p._visBoneNode.Name, new List<int> { i });
                        else if (!VIS0Indices[p._visBoneNode.Name].Contains(i))
                            VIS0Indices[p._visBoneNode.Name].Add(i);
                    i++;
                }
        }

        public void Detach()
        {
            _attached = false;
            _isTargetModel = false;
            ResetToBindState();
            foreach (MDL0GroupNode g in Children)
                g.Unbind();
        }

        public void Refresh()
        {
            if (_texList != null)
                foreach (MDL0TextureNode t in _texList)
                    t.Reload();
        }

        public static void RenderObject(
            MDL0ObjectNode p, 
            float maxDrawPriority,
            bool dontRenderOffscreen,
            bool renderPolygons,
            bool renderWireframe)
        {
            if (p._render)
            {
                //if (dontRenderOffscreen)
                //{
                //    Vector3 min = new Vector3(float.MaxValue);
                //    Vector3 max = new Vector3(float.MinValue);

                //    if (p._manager != null)
                //        foreach (Vertex3 vertex in p._manager._vertices)
                //        {
                //            Vector3 v = GLPanel.Current.Project(vertex.WeightedPosition);

                //            min.Min(v);
                //            max.Max(v);
                //        }

                //    if (max._x < 0 || min._x > GLPanel.Current.Size.Width ||
                //        max._y < 0 || min._y > GLPanel.Current.Size.Height)
                //        return;
                //}

                if (renderPolygons)
                {
                    float polyOffset = 0.0f;
                    //polyOffset -= p.DrawPriority;
                    //polyOffset += maxDrawPriority;
                    if (renderWireframe)
                        polyOffset += 1.0f;
                    if (polyOffset != 0)
                    {
                        GL.Enable(EnableCap.PolygonOffsetFill);
                        GL.PolygonOffset(1.0f, polyOffset);
                    }
                    else
                        GL.Disable(EnableCap.PolygonOffsetFill);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    p.Render(false);
                }
                if (renderWireframe)
                {
                    GL.Disable(EnableCap.PolygonOffsetFill);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.LineWidth(0.5f);
                    p.Render(true);
                }
            }
        }

        public Matrix _matrixOffset = Matrix.Identity;
        public void Render(params object[] args)
        {
            if (!_render || TKContext.CurrentContext == null)
                return;
            
            ModelRenderAttributes attrib;
            ModelPanelViewport v = null;

            if (args.Length > 0 && args[0] is ModelPanelViewport)
                v = args[0] as ModelPanelViewport;

            if (!_ignoreModelViewerAttribs && v != null && v._renderAttrib != null)
                attrib = v._renderAttrib;
            else
                attrib = _renderAttribs;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            if (_matrixOffset != Matrix.Identity && _matrixOffset != new Matrix())
            {
                GL.PushMatrix();
                Matrix m = _matrixOffset;
                GL.MultMatrix((float*)&m);
            }

            //Apply billboard bones before rendering meshes
            if (attrib._applyBillboardBones && _billboardBones.Count > 0)
            {
                WeightModel(v);
                ApplySHP(_currentSHP, _currentSHPIndex);
            }

            if (attrib._renderPolygons || attrib._renderWireframe)
            {
                GL.PushAttrib(AttribMask.AllAttribBits);

                GL.Enable(EnableCap.Lighting);
                GL.Enable(EnableCap.DepthTest);

                float maxDrawPriority = 0.0f;
                if (_objList != null)
                    foreach (MDL0ObjectNode p in _objList)
                        maxDrawPriority = Math.Max(maxDrawPriority, p.DrawPriority);

                //Draw objects in the prioritized order of materials.
                List<MDL0ObjectNode> rendered = new List<MDL0ObjectNode>();
                if (_matList != null)
                    foreach (MDL0MaterialNode m in _matList)
                        foreach (MDL0ObjectNode p in m._objects)
                        {
                            RenderObject(p, maxDrawPriority, attrib._dontRenderOffscreen, attrib._renderPolygons, attrib._renderWireframe);
                            rendered.Add(p);
                        }

                //Render any remaining objects
                if (_objList != null)
                    foreach (MDL0ObjectNode p in _objList)
                        if (!rendered.Contains(p))
                            RenderObject(p, maxDrawPriority, attrib._dontRenderOffscreen, attrib._renderPolygons, attrib._renderWireframe);

                //Turn off the last bound shader program.
                if (TKContext.CurrentContext._shadersEnabled)
                {
                    GL.UseProgram(0);
                    GL.ClientActiveTexture(TextureUnit.Texture0);
                }

                GL.PopAttrib();
            }

            if (attrib._renderModelBox || attrib._renderObjectBoxes || attrib._renderBoneBoxes)
            {
                GL.PushAttrib(AttribMask.AllAttribBits);

                GL.Disable(EnableCap.Lighting);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                bool bindState = _bindFrame && attrib._useBindStateBoxes;

                if (attrib._renderModelBox)
                {
                    GL.Color4(Color.Gray);
                    DrawBox(bindState);
                }

                if (attrib._renderObjectBoxes && _objList != null)
                {
                    GL.Color4(Color.Purple);
                    if (_selectedObjectIndex != -1 && ((MDL0ObjectNode)_objList[_selectedObjectIndex])._render)
                        ((MDL0ObjectNode)_objList[_selectedObjectIndex]).DrawBox();
                    else
                        foreach (MDL0ObjectNode p in _objList)
                            if (p._render)
                                p.DrawBox();
                }

                if (attrib._renderBoneBoxes)
                {
                    GL.Color4(Color.Orange);
                    foreach (MDL0BoneNode bone in _boneList)
                        bone.DrawBox(true, bindState);
                }

                GL.PopAttrib();
            }

            if (attrib._renderBones)
            {
                GL.PushAttrib(AttribMask.AllAttribBits);

                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.Lighting);
                GL.Disable(EnableCap.DepthTest);
                //GL.Enable(EnableCap.LineSmooth);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.LineWidth(1.5f);

                if (_boneList != null)
                    foreach (MDL0BoneNode bone in _boneList)
                        bone.Render(_isTargetModel, v);

                GL.PopAttrib();
            }

            if (_matrixOffset != Matrix.Identity && _matrixOffset != new Matrix())
                GL.PopMatrix();
        }

        public void RenderVertices(bool depthPass, IBoneNode weightTarget, GLCamera camera)
        {
            if (_objList != null)
                if (_selectedObjectIndex != -1)
                {
                    MDL0ObjectNode o = (MDL0ObjectNode)_objList[_selectedObjectIndex];
                    if (o._render)
                    {
                        o._manager.RenderVertices(o._matrixNode, weightTarget, depthPass, camera);
                        return;
                    }
                }
                else
                    foreach (MDL0ObjectNode p in _objList)
                        if (p._render)
                            p._manager.RenderVertices(p._matrixNode, weightTarget, depthPass, camera);
        }

        public void RenderNormals()
        {
            if (_objList != null)
                if (_selectedObjectIndex != -1)
                {
                    MDL0ObjectNode o = (MDL0ObjectNode)_objList[_selectedObjectIndex];
                    if (o._render)
                        o._manager.RenderNormals();
                }
                else 
                    foreach (MDL0ObjectNode p in _objList)
                        if (p._render)
                            p._manager.RenderNormals();
            
        }

        [Browsable(false)]
        public int SelectedObjectIndex { get { return _selectedObjectIndex; } set { _selectedObjectIndex = value; } }
        [Browsable(false)]
        public IObject[] Objects { get { return _objList == null ? new IObject[0] : _objList.Select(x => x as IObject).ToArray(); } }

        public void ResetToBindState()
        {
            ApplyCHR(null, 0);
            ApplySRT(null, 0);
            ApplyVIS(null, 0);
            ApplyPAT(null, 0);
            ApplyCLR(null, 0);
        }

        public void DrawBox(bool bindState)
        {
            Box box = bindState ? _extents : GetBox();
            //if (box.IsValid)
                TKContext.DrawWireframeBox(box);
        }

        bool _bindFrame = true;
        public void ApplyCHR(CHR0Node node, float index)
        {
            _bindFrame = node == null || index == 0;

            //Transform bones
            if (_boneList != null)
                foreach (MDL0BoneNode b in _boneList)
                    b.ApplyCHR0(node, index);

            WeightModel();
        }

        public void WeightModel(ModelPanelViewport v = null)
        {
            //Multiply matrices
            if (_boneList != null)
                foreach (MDL0BoneNode b in _boneList)
                    b.RecalcFrameState(v);
            foreach (Influence inf in _influences._influences)
                inf.CalcMatrix();

            //Weight vertices
            if (_objList != null)
                foreach (MDL0ObjectNode poly in _objList)
                    poly.WeightVertices();
        }

        public void ApplySRT(SRT0Node node, float index)
        {
            //Transform textures
            if (_matList != null)
                foreach (MDL0MaterialNode m in _matList)
                    m.ApplySRT0(node, index);
        }

        public void ApplyCLR(CLR0Node node, float index)
        {
            //Apply color changes
            if (_matList != null)
                foreach (MDL0MaterialNode m in _matList)
                    m.ApplyCLR0(node, index);
        }

        public void ApplyPAT(PAT0Node node, float index)
        {
            //Change textures
            if (_matList != null)
                foreach (MDL0MaterialNode m in _matList)
                    m.ApplyPAT0(node, index);
        }

        public void ApplyVIS(VIS0Node node, float index)
        {
            if (node == null || index < 1)
            {
                //if (_objList != null)
                //    foreach (MDL0ObjectNode o in _objList)
                //        if (o._visBoneNode != null)
                //            o._render = o._visBoneNode._boneFlags.HasFlag(BoneFlags.Visible);
                return;
            }

            if (VIS0Indices == null)
                RegenerateVIS0Indices();

            foreach (string n in VIS0Indices.Keys)
            {
                VIS0EntryNode entry = null;
                List<int> indices = VIS0Indices[n];
                for (int i = 0; i < indices.Count; i++)
                    if ((entry = (VIS0EntryNode)node.FindChild(((MDL0ObjectNode)_objList[indices[i]])._visBoneNode.Name, true)) != null)
                        if (entry._entryCount != 0 && index >= 1)
                            ((MDL0ObjectNode)_objList[indices[i]])._render = entry.GetEntry((int)index - 1);
                        else
                            ((MDL0ObjectNode)_objList[indices[i]])._render = entry._flags.HasFlag(VIS0Flags.Enabled);
            }
        }

        public void ApplySCN(SCN0Node node, float index)
        {
            if (_matList != null)
                foreach (MDL0MaterialNode mat in _matList)
                    mat.ApplySCN(node, index);
        }

        //This only modifies vertices after ApplyCHR0 has weighted them.
        //It cannot be used without calling ApplyCHR0 first.
        //TODO: Find a more efficient way to store this data
        public void ApplySHP(SHP0Node node, float index)
        {
            _currentSHP = node;
            _currentSHPIndex = index;

            if (node == null || index == 0)
                return;

            SHP0EntryNode entry;

            if (_objList != null)
                foreach (MDL0ObjectNode poly in _objList)
                    if (poly._manager != null && 
                        (entry = node.FindChild(poly.VertexNode, true) as SHP0EntryNode) != null && 
                        entry.Enabled)
                    {
                        if (entry.UpdateVertices)
                        {
                            //Max amount of morphs allowed is technically 32
                            float[] weights = new float[entry.Children.Count];
                            MDL0VertexNode[] nodes = new MDL0VertexNode[entry.Children.Count];

                            foreach (SHP0VertexSetNode shpSet in entry.Children)
                            {
                                MDL0VertexNode vNode = _vertList.Find(x => x.Name == shpSet.Name) as MDL0VertexNode;

                                weights[shpSet.Index] = vNode != null ? shpSet.Keyframes.GetFrameValue(index - 1) : 0;
                                nodes[shpSet.Index] = vNode;
                            }

                            float totalWeight = 0;
                            foreach (float f in weights)
                                totalWeight += f;

                            float baseWeight = 1.0f - totalWeight;

                            //Calculate barycenter per vertex and set as weighted pos
                            for (int i = 0; i < poly._manager._vertices.Count; i++)
                            {
                                int x = 0;
                                Vertex3 v3 = poly._manager._vertices[i];
                                v3._weightedPosition *= baseWeight;

                                foreach (MDL0VertexNode vNode in nodes)
                                    if (vNode != null && v3._facepoints[0]._vertexIndex < vNode.Vertices.Length)
                                        v3._weightedPosition += (v3.GetMatrix() * vNode.Vertices[v3._facepoints[0]._vertexIndex]) * weights[x++];

                                v3._weightedPosition /= (totalWeight + baseWeight);

                                v3._weights = weights;
                                v3._nodes = nodes;
                                v3._baseWeight = baseWeight;
                                v3._bCenter = v3._weightedPosition;
                            }
                        }

                        //TODO: update normals and colors
                        //This will be a bit trickier since they're not stored in the Vertex3 class
                    }
        }
        #endregion

        internal static ResourceNode TryParse(DataSource source) { return ((MDL0Header*)source.Address)->_header._tag == MDL0Header.Tag ? new MDL0Node() : null; }
    }
}
