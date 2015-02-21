﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrawlLib.SSBB.ResourceNodes;
using System.IO;
using BrawlLib.IO;
using System.Windows.Forms;
using BrawlLib.Wii.Models;
using BrawlLib.Imaging;
using BrawlLib.SSBBTypes;
using BrawlLib.Wii.Graphics;
using System.Globalization;
using System.Threading;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;

namespace BrawlLib.Modeling
{
    public unsafe partial class Collada : Form
    {
        public static string Error;
        public static IModel CurrentModel;
        public static ImportType ModelType;
        private static Type BoneType;
        private static ResourceNode TempRootBone;

        public enum ImportType
        {
            MDL0,
            BMD,
            //LM
            //FMDL
            //NP3D
        }

        public IModel ImportModel(string filePath, ImportType type)
        {
            IModel model = null;
            ModelType = type;

            BoneType = ModelType == ImportType.MDL0 ? typeof(MDL0BoneNode) : null;

            switch (type)
            {
                case ImportType.MDL0:
                    MDL0Node m = new MDL0Node()
                    {
                        _name = Path.GetFileNameWithoutExtension(filePath),
                        _version = _importOptions._modelVersion.Clamp(8, 11)
                    };
                    if (_importOptions._setOrigPath)
                        m._originalPath = filePath;
                    m.BeginImport();
                    model = m;
                    break;

                case ImportType.BMD:
                    break;
            }

            CurrentModel = model;

            Error = "There was a problem reading the model.";
            using (DecoderShell shell = DecoderShell.Import(filePath))
            try
            {
                Error = "There was a problem reading texture entries.";

                //Extract images, removing duplicates
                foreach (ImageEntry img in shell._images)
                {
                    string name;
                    if (img._path != null)
                        name = Path.GetFileNameWithoutExtension(img._path);
                    else
                        name = img._name != null ? img._name : img._id;

                    switch (type)
                    {
                        case ImportType.MDL0:
                            img._node = ((MDL0Node)model).FindOrCreateTexture(name);
                            break;
                    }
                }

                Error = "There was a problem creating a default shader.";

                //Create a shader
                ResourceNode shader = null;
                switch (type)
                {
                    case ImportType.MDL0:
                        MDL0Node m = (MDL0Node)model;
                        MDL0ShaderNode shadNode = new MDL0ShaderNode()
                        {
                            _ref0 = 0,
                            _ref1 = -1,
                            _ref2 = -1,
                            _ref3 = -1,
                            _ref4 = -1,
                            _ref5 = -1,
                            _ref6 = -1,
                            _ref7 = -1,
                        };

                        shadNode._parent = m._shadGroup;
                        m._shadList.Add(shadNode);

                        switch (_importOptions._mdlType)
                        {
                            case ImportOptions.MDLType.Character:
                                for (int i = 0; i < 3; i++)
                                {
                                    switch (i)
                                    {
                                        case 0:
                                            shadNode.AddChild(new TEVStageNode(0x28F8AF, 0x08F2F0, 0, TevKColorSel.KSel_0_Value, TevKAlphaSel.KSel_0_Alpha, TexMapID.TexMap0, TexCoordID.TexCoord0, ColorSelChan.ColorChannel0, true));
                                            break;
                                        case 1:
                                            shadNode.AddChild(new TEVStageNode(0x08FEB0, 0x081FF0, 0, TevKColorSel.KSel_1_Value, TevKAlphaSel.KSel_0_Alpha, TexMapID.TexMap7, TexCoordID.TexCoord7, ColorSelChan.ColorChannel0, false));
                                            break;
                                        case 2:
                                            shadNode.AddChild(new TEVStageNode(0x0806EF, 0x081FF0, 0, TevKColorSel.KSel_0_Value, TevKAlphaSel.KSel_0_Alpha, TexMapID.TexMap7, TexCoordID.TexCoord7, ColorSelChan.Zero, false));
                                            break;
                                    }
                                }
                                break;
                            case ImportOptions.MDLType.Stage:
                                shadNode.AddChild(new TEVStageNode(0x28F8AF, 0x08F2F0, 0, TevKColorSel.KSel_0_Value, TevKAlphaSel.KSel_0_Alpha, TexMapID.TexMap0, TexCoordID.TexCoord0, ColorSelChan.ColorChannel0, true));
                                break;
                        }

                        shader = shadNode;

                        break;
                }

                Error = "There was a problem extracting materials.";

                //Extract materials
                foreach (MaterialEntry mat in shell._materials)
                {
                    List<ImageEntry> imgEntries = new List<ImageEntry>();

                    //Find effect
                    if (mat._effect != null)
                        foreach (EffectEntry eff in shell._effects)
                            if (eff._id == mat._effect) //Attach textures and effects to material
                                if (eff._shader != null)
                                    foreach (LightEffectEntry l in eff._shader._effects)
                                        if (l._type == LightEffectType.diffuse && l._texture != null)
                                        {
                                            string path = l._texture;
                                            foreach (EffectNewParam p in eff._newParams)
                                                if (p._sid == l._texture)
                                                {
                                                    path = p._sampler2D._url;
                                                    if (!String.IsNullOrEmpty(p._sampler2D._source))
                                                        foreach (EffectNewParam p2 in eff._newParams)
                                                            if (p2._sid == p._sampler2D._source)
                                                                path = p2._path;
                                                }

                                            foreach (ImageEntry img in shell._images)
                                                if (img._id == path)
                                                {
                                                    imgEntries.Add(img);
                                                    break;
                                                }
                                        }
                    switch (type)
                    {
                        case ImportType.MDL0:
                            MDL0MaterialNode matNode = new MDL0MaterialNode();

                            MDL0Node m = (MDL0Node)model;
                            matNode._parent = m._matGroup;
                            m._matList.Add(matNode);

                            matNode._name = mat._name != null ? mat._name : mat._id;
                            matNode.ShaderNode = shader as MDL0ShaderNode;

                            mat._node = matNode;
                            matNode._cull = _importOptions._culling;

                            foreach (ImageEntry img in imgEntries)
                            {
                                MDL0MaterialRefNode mr = new MDL0MaterialRefNode();
                                (mr._texture = img._node as MDL0TextureNode)._references.Add(mr);
                                mr._name = mr._texture.Name;
                                matNode._children.Add(mr);
                                mr._parent = matNode;
                                mr._minFltr = mr._magFltr = 1;
                                mr._index1 = mr._index2 = mr.Index;
                                mr._uWrap = mr._vWrap = (int)_importOptions._wrap;
                            }
                            break;
                    }
                }

                Say("Extracting scenes...");

                List<ObjectInfo> _objects = new List<ObjectInfo>();
                ResourceNode boneGroup = null;
                switch (type)
                {
                    case ImportType.MDL0:
                        boneGroup = ((MDL0Node)model)._boneGroup;
                        break;
                }

                //Extract bones and objects and create bone tree
                foreach (SceneEntry scene in shell._scenes)
                    foreach (NodeEntry node in scene._nodes)
                        EnumNode(node, boneGroup, scene, model, shell, _objects, Matrix.Identity);

                //Add root bone if there are no bones
                if (boneGroup.Children.Count == 0)
                    switch (type)
                    {
                        case ImportType.MDL0:
                            MDL0BoneNode bone = new MDL0BoneNode();
                            bone.Scale = new Vector3(1);
                            bone.RecalcBindState();
                            bone._name = "TopN";
                            TempRootBone = bone;
                            break;
                    }

                //Create objects
                foreach (ObjectInfo obj in _objects)
                {
                    NodeEntry node = obj._node;
                    string w = obj._weighted ? "" : "un";
                    string w2 = obj._weighted ? "\nOne or more vertices may not be weighted correctly." : "";
                    string n = node._name != null ? node._name : node._id;

                    Error = String.Format("There was a problem decoding {0}weighted primitives for the object {1}.{2}", w, n, w2);

                    Say(String.Format("Decoding {0}weighted primitives for {1}...", w, n));

                    obj.Initialize(model, shell);
                }

                //Finish
                switch (type)
                {
                    case ImportType.MDL0:
                        MDL0Node mdl0 = (MDL0Node)model;
                        if (TempRootBone != null)
                        {
                            mdl0._boneGroup._children.Add(TempRootBone);
                            TempRootBone._parent = mdl0._boneGroup;
                        }
                        FinishMDL0(mdl0);
                        break;
                }
            }
#if !DEBUG
            catch (Exception x)
            {
                MessageBox.Show("Cannot continue importing this model.\n" + Error + "\n\nException:\n" + x.ToString());
                model = null;
                Close();
            }
#endif
            finally
            {
                //Clean up the mess we've made
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }

            CurrentModel = null;
            Error = null;

            return model;
        }

        private void EnumNode(
            NodeEntry node, 
            ResourceNode parent,
            SceneEntry scene,
            IModel model,
            DecoderShell shell,
            List<ObjectInfo> objects,
            Matrix bindMatrix)
        {
            bindMatrix *= node._matrix;

            if (node._type == NodeType.JOINT)
            {
                Error = "There was a problem creating a new bone.";

                Influence inf = null;
                ResourceNode newParent = null;

                switch (ModelType)
                {
                    case ImportType.MDL0:
                        MDL0BoneNode bone = new MDL0BoneNode();
                        bone._name = node._name != null ? node._name : node._id;

                        bone._bindState = node._transform;
                        node._node = bone;

                        parent._children.Add(bone);
                        bone._parent = parent;

                        bone.RecalcBindState();
                        bone.CalcFlags();

                        newParent = bone;

                        inf = new Influence(bone);
                        break;
                }

                if (newParent != null)
                    foreach (NodeEntry e in node._children)
                        EnumNode(e, newParent, scene, model, shell, objects, bindMatrix);

                if (inf != null)
                    model.Influences._influences.Add(inf);
            }
            else
                foreach (NodeEntry e in node._children)
                    EnumNode(e, parent, scene, model, shell, objects, bindMatrix);

            foreach (InstanceEntry inst in node._instances)
            {
                if (inst._type == InstanceType.Controller)
                {
                    foreach (SkinEntry skin in shell._skins)
                        if (skin._id == inst._url)
                        {
                            foreach (GeometryEntry g in shell._geometry)
                                if (g._id == skin._skinSource)
                                {
                                    objects.Add(new ObjectInfo(true, g, bindMatrix, skin, scene, inst, parent, node));
                                    break;
                                }
                            break;
                        }
                }
                else if (inst._type == InstanceType.Geometry)
                {
                    foreach (GeometryEntry g in shell._geometry)
                        if (g._id == inst._url)
                        {
                            objects.Add(new ObjectInfo(false, g, bindMatrix, null, null, inst, parent, node));
                            break;
                        }
                }
                else
                    foreach (NodeEntry e in shell._nodes)
                        if (e._id == inst._url)
                            EnumNode(e, parent, scene, model, shell, objects, bindMatrix);
            }
        }

        private class ObjectInfo
        {
            public bool _weighted;
            public GeometryEntry _g;
            public Matrix _bindMatrix;
            public SkinEntry _skin;
            public InstanceEntry _inst;
            public SceneEntry _scene;
            public ResourceNode _parent;
            public NodeEntry _node;

            public ObjectInfo(
                bool weighted,
                GeometryEntry g,
                Matrix bindMatrix,
                SkinEntry skin,
                SceneEntry scene,
                InstanceEntry inst,
                ResourceNode parent,
                NodeEntry node)
            {
                _weighted = weighted;
                _g = g;
                _bindMatrix = bindMatrix;
                _skin = skin;
                _scene = scene;
                _parent = parent;
                _node = node;
                _inst = inst;
            }

            public void Initialize(IModel model, DecoderShell shell)
            {
                PrimitiveManager m;
                if (_weighted)
                    m = DecodePrimitivesWeighted(_bindMatrix, _g, _skin, _scene, model.Influences, BoneType);
                else
                    m = DecodePrimitivesUnweighted(_bindMatrix, _g);

                switch (ModelType)
                {
                    case ImportType.MDL0:
                        CreateMDL0Object(_inst, _node, _parent, m, (MDL0Node)model, shell);
                        break;
                }
            }
        }

        private static void CreateMDL0Object(
            InstanceEntry inst,
            NodeEntry node,
            ResourceNode parent,
            PrimitiveManager manager,
            MDL0Node model,
            DecoderShell shell)
        {
            if (manager != null)
            {
                Error = "There was a problem creating a new object for " + (node._name != null ? node._name : node._id);

                MDL0ObjectNode poly = new MDL0ObjectNode() { _manager = manager };
                poly._name = node._name != null ? node._name : node._id;

                //Attach material
                if (inst._material != null)
                    foreach (MaterialEntry mat in shell._materials)
                        if (mat._id == inst._material._target)
                        {
                            (poly._opaMaterial = (mat._node as MDL0MaterialNode))._objects.Add(poly);
                            break;
                        }

                model._numTriangles += poly._numFaces = manager._faceCount = manager._pointCount / 3;
                model._numFacepoints += poly._numFacepoints = manager._pointCount;

                poly._parent = model._objGroup;
                model._objList.Add(poly);

                model.ApplyCHR(null, 0);

                //Attach single-bind
                if (parent != null && parent is MDL0BoneNode)
                {
                    MDL0BoneNode bone = (MDL0BoneNode)parent;
                    foreach (Vertex3 v in poly.Vertices)
                        v.Position *= bone.InverseBindMatrix;

                    poly.MatrixNode = bone;
                }
                else if (model._boneList.Count == 0)
                {
                    Error = String.Format("There was a problem rigging {0} to a single bone.", poly._name);

                    Box box = poly.GetBox();

                    MDL0BoneNode bone = new MDL0BoneNode();
                    bone.Scale = new Vector3(1);
                    bone.Translation = (box.Max + box.Min) / 2;
                    bone.RecalcBindState();

                    bone._name = "TransN_" + poly.Name;

                    foreach (Vertex3 v in poly.Vertices)
                        v.Position *= bone.InverseBindMatrix;

                    poly.MatrixNode = bone;
                    bone.ReferenceCount++;

                    TempRootBone._children.Add(bone);
                    bone._parent = TempRootBone;
                }
                else
                {
                    Error = String.Format("There was a problem checking if {0} is rigged to a single bone.", poly._name);

                    IMatrixNode mtxNode = null;
                    bool singlebind = true;

                    foreach (Vertex3 v in poly._manager._vertices)
                        if (v.MatrixNode != null)
                        {
                            if (mtxNode == null)
                                mtxNode = v.MatrixNode;

                            if (v.MatrixNode != mtxNode)
                            {
                                singlebind = false;
                                break;
                            }
                        }

                    if (singlebind && poly._matrixNode == null)
                    {
                        //Increase reference count ahead of time for rebuild
                        if (poly._manager._vertices[0].MatrixNode != null)
                            poly._manager._vertices[0].MatrixNode.ReferenceCount++;

                        foreach (Vertex3 v in poly._manager._vertices)
                            if (v.MatrixNode != null)
                                v.MatrixNode.ReferenceCount--;

                        poly._nodeId = -2; //Continued on polygon rebuild
                    }
                }
            }
        }

        private void FinishMDL0(MDL0Node model)
        {
            if (model._matList.Count == 0 && model._objList.Count != 0)
            {
                //TODO: Add a default material
            }

            Error = "There was a problem removing original color buffers.";

            //Remove original color buffers if option set
            if (_importOptions._ignoreColors)
            {
                if (model._objList != null && model._objList.Count != 0)
                    foreach (MDL0ObjectNode p in model._objList)
                        for (int x = 2; x < 4; x++)
                            if (p._manager._faceData[x] != null)
                            {
                                p._manager._faceData[x].Dispose();
                                p._manager._faceData[x] = null;
                            }
            }

            Error = "There was a problem adding default color values.";

            //Add color buffers if option set
            if (_importOptions._addClrs)
            {
                RGBAPixel pixel = _importOptions._dfltClr;

                //Add a color buffer to objects that don't have one
                if (model._objList != null && model._objList.Count != 0)
                    foreach (MDL0ObjectNode p in model._objList)
                        if (p._manager._faceData[2] == null)
                        {
                            RGBAPixel* pIn = (RGBAPixel*)(p._manager._faceData[2] = new UnsafeBuffer(4 * p._manager._pointCount)).Address;
                            for (int i = 0; i < p._manager._pointCount; i++)
                                *pIn++ = pixel;
                        }
            }

            Error = "There was a problem initializing materials.";

            //Apply defaults to materials
            if (model._matList != null)
                foreach (MDL0MaterialNode p in model._matList)
                {
                    if (_importOptions._mdlType == 0)
                    {
                        p._lightSetIndex = 20;
                        p._fogIndex = 4;
                        p._activeStages = 3;

                        p.C1ColorEnabled = true;
                        p.C1AlphaMaterialSource = GXColorSrc.Vertex;
                        p.C1ColorMaterialSource = GXColorSrc.Vertex;
                        p.C1ColorDiffuseFunction = GXDiffuseFn.Clamped;
                        p.C1ColorAttenuation = GXAttnFn.Spotlight;
                        p.C1AlphaEnabled = true;
                        p.C1AlphaDiffuseFunction = GXDiffuseFn.Clamped;
                        p.C1AlphaAttenuation = GXAttnFn.Spotlight;

                        p.C2ColorDiffuseFunction = GXDiffuseFn.Disabled;
                        p.C2ColorAttenuation = GXAttnFn.None;
                        p.C2AlphaDiffuseFunction = GXDiffuseFn.Disabled;
                        p.C2AlphaAttenuation = GXAttnFn.None;
                    }
                    else
                    {
                        p._lightSetIndex = 0;
                        p._fogIndex = 0;
                        p._activeStages = 1;

                        p._chan1.Color = new LightChannelControl(1795);
                        p._chan1.Alpha = new LightChannelControl(1795);
                        p._chan2.Color = new LightChannelControl(1795);
                        p._chan2.Alpha = new LightChannelControl(1795);
                    }
                }

            //Set materials to use register color if option set
            if (_importOptions._useReg && model._objList != null)
                foreach (MDL0ObjectNode p in model._objList)
                {
                    MDL0MaterialNode m = p.OpaMaterialNode;
                    if (m != null && p._manager._faceData[2] == null && p._manager._faceData[3] == null)
                    {
                        m.C1MaterialColor = _importOptions._dfltClr;
                        m.C1ColorMaterialSource = GXColorSrc.Register;
                        m.C1AlphaMaterialSource = GXColorSrc.Register;
                    }
                }

            Error = "There was a problem remapping materials.";

            //Remap materials if option set
            if (_importOptions._rmpMats && model._matList != null && model._objList != null)
            {
                foreach (MDL0ObjectNode p in model._objList)
                    foreach (MDL0MaterialNode m in model._matList)
                        if (m.Children.Count > 0 &&
                            m.Children[0] != null &&
                            p.OpaMaterialNode != null &&
                            p.OpaMaterialNode.Children.Count > 0 &&
                            p.OpaMaterialNode.Children[0] != null &&
                            m.Children[0].Name == p.OpaMaterialNode.Children[0].Name &&
                            m.C1ColorMaterialSource == p.OpaMaterialNode.C1ColorMaterialSource)
                        {
                            p.OpaMaterialNode = m;
                            break;
                        }

                //Remove unused materials
                for (int i = 0; i < model._matList.Count; i++)
                    if (((MDL0MaterialNode)model._matList[i])._objects.Count == 0)
                        model._matList.RemoveAt(i--);
            }

            Error = "There was a problem writing the model.";

            //Clean the model and then build it!
            if (model != null)
                model.FinishImport();
        }

        public static ImportOptions _importOptions = new ImportOptions();
        public class ImportOptions
        {
            public enum MDLType
            {
                Character,
                Stage
            }

            [Category("Model"), Description("Determines the default settings for materials and shaders.")]
            public MDLType ModelType { get { return _mdlType; } set { _mdlType = value; } }
            [Category("Model"), Description("If true, object primitives will be culled in reverse. This means the outside of the object will be the inside, and the inside will be the outside. It is not recommended to change this to true as you can change the culling later using the object's material.")]
            public bool ForceCounterClockwisePrimitives { get { return _forceCCW; } set { _forceCCW = value; } }
            [Category("Model"), Description("If true, the file path of the imported model will be written to the model's header.")]
            public bool SetOriginalPath { get { return _setOrigPath; } set { _setOrigPath = value; } }
            [Category("Model"), Description("Determines how precise weights will be compared. A smaller value means more accuracy but also more influences, resulting in a larger file size. A larger value means the weighting will be less accurate but there will be less influences.")]
            public float WeightPrecision { get { return _weightPrecision; } set { _weightPrecision = value.Clamp(0.0000001f, 0.999999f); } }
            [Category("Model"), Description("Sets the model version number, which affects how some parts of the model are written. Only versions 8, 9, 10 and 11 are supported.")]
            public int ModelVersion { get { return _modelVersion; } set { _modelVersion = value.Clamp(8, 11); } }

            [Category("Materials"), Description("The default texture wrap for material texture references.")]
            public MatWrapMode TextureWrap { get { return _wrap; } set { _wrap = value; } }
            [Category("Materials"), Description("If true, materials will be remapped. This means there will be no redundant materials with the same settings, saving file space.")]
            public bool RemapMaterials { get { return _rmpMats; } set { _rmpMats = value; } }
            [Category("Materials"), Description("The default setting to use for material culling. Culling determines what side of the mesh is invisible.")]
            public CullMode MaterialCulling { get { return _culling; } set { _culling = value; } }

            [Category("Assets"), Description("If true, vertex arrays will be written in float format. This means that the data size will be larger, but more precise. Float arrays for vertices must be used if the model uses texture matrices, tristripped primitives or SHP0 morph animations; otherwise the model will explode in-game.")]
            public bool ForceFloatVertices { get { return _fltVerts; } set { _fltVerts = value; } }
            [Category("Assets"), Description("If true, normal arrays will be written in float format. This means that the data size will be larger, but more precise.")]
            public bool ForceFloatNormals { get { return _fltNrms; } set { _fltNrms = value; } }
            [Category("Assets"), Description("If true, texture coordinate arrays will be written in float format. This means that the data size will be larger, but more precise.")]
            public bool ForceFloatUVs { get { return _fltUVs; } set { _fltUVs = value; } }

            [Category("Color Nodes"), Description("If true, color arrays read from the file will be ignored.")]
            public bool IgnoreOriginalColors { get { return _ignoreColors; } set { _ignoreColors = value; } }
            [Category("Color Nodes"), Description("If true, color arrays will be added to objects that do not have any. The array will be filled with only the default color.")]
            public bool AddColors { get { return _addClrs; } set { _addClrs = value; } }
            [Category("Color Nodes"), Description("If true, color arrays will be remapped. This means there will not be any color nodes that have the same entries as another, saving file space.")]
            public bool RemapColors { get { return _rmpClrs; } set { _rmpClrs = value; } }
            [Category("Color Nodes"), Description("If true, objects without color arrays will use a constant color (set to the default color) in its material for the whole mesh instead of a color node that specifies a color for every vertex. This saves a lot of file space.")]
            public bool UseRegisterColor { get { return _useReg; } set { _useReg = value; } }
            [Category("Color Nodes"), TypeConverter(typeof(RGBAStringConverter)), Description("The default color to use for generated color arrays.")]
            public RGBAPixel DefaultColor { get { return _dfltClr; } set { _dfltClr = value; } }
            [Category("Color Nodes"), Description("This will make all colors be written in one color node. This will save file space for models with lots of different colors.")]
            public bool UseOneNode { get { return _useOneNode; } set { _useOneNode = value; } }

            [Category("Tristripper"), Description("Determines whether the model will be optimized to use tristrips along with triangles or not. Tristrips can greatly reduce in-game lag, so it is highly recommended that you leave this as true.")]
            public bool UseTristrips { get { return _useTristrips; } set { _useTristrips = value; } }
            [Category("Tristripper"), Description("The size of the cache optimizer which affects the final amount of face points. Set to 0 to disable.")]
            public uint CacheSize { get { return _cacheSize; } set { _cacheSize = value; } }
            [Category("Tristripper"), Description("The minimum amount of triangles that must be included in a strip. This cannot be lower than two triangles and should not be a large number. Two is highly recommended.")]
            public uint MinimumStripLength { get { return _minStripLen; } set { _minStripLen = value < 2 ? 2 : value; } }
            [Category("Tristripper"), Description("When enabled, pushes cache hits into a simple FIFO structure to simulate GPUs that don't duplicate cache entries, affecting the final face point count. Does nothing if the cache is disabled.")]
            public bool PushCacheHits { get { return _pushCacheHits; } set { _pushCacheHits = value; } }
            //[Category("Tristripper"), Description("If true, the tristripper will search for strips backwards as well as forwards.")]
            //public bool BackwardSearch { get { return _backwardSearch; } set { _backwardSearch = value; } }

            public MDLType _mdlType = MDLType.Character;
            public bool _fltVerts = true;
            public bool _fltNrms = true;
            public bool _fltUVs = true;
            public bool _addClrs = false;
            public bool _rmpClrs = true;
            public bool _rmpMats = true;
            public bool _forceCCW = false;
            public bool _useReg = true;
            public bool _ignoreColors = false;
            public CullMode _culling = CullMode.Cull_None;
            public RGBAPixel _dfltClr = new RGBAPixel(100, 100, 100, 255);
            public uint _cacheSize = 52;
            public uint _minStripLen = 2;
            public bool _pushCacheHits = true;
            public bool _useTristrips = true;
            public bool _setOrigPath = false;
            public float _weightPrecision = 0.0001f;
            public int _modelVersion = 9;
            public bool _useOneNode = true;
            public MatWrapMode _wrap = MatWrapMode.Repeat;

            //This doesn't work, but it's optional and not efficient with the cache on anyway
            public bool _backwardSearch = false;

            internal RGBAPixel[] _singleColorNodeEntries;
        }
    }
}
