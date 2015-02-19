﻿using BrawlLib.Wii.Models;
using BrawlLib.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BrawlLib.Modeling
{
    public interface IBoneNode : IMatrixNode
    {
        string Name { get; set; }
        bool Locked { get; set; }
        Matrix BindMatrix { get; }
        Matrix InverseBindMatrix { get; }
        int WeightCount { get; set; }
        FrameState BindState { get; set; }
        FrameState FrameState { get; set; }
        Color NodeColor { get; set; }
        Color BoneColor { get; set; }
        int BoneIndex { get; }
        IModel IModel { get; }
        List<Influence> LinkedInfluences { get; }
        bool IsRendering { get; set; }
        void Render(bool targetModel, GLViewport viewport);
    }
}
