﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting;
using IronPython.Runtime.Exceptions;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace BrawlBox.API
{
    public class PluginScript
    {
        public PluginScript(string name, ScriptSource script, ScriptScope scope)
        {
            Name = name;
            Script = script;
            Scope = scope;
        }

        public string Name { get; set; }
        public ScriptSource Script { get; set; }
        public ScriptScope Scope { get; set; }

        public void Execute()
        {
            Script.Execute(Scope);
        }
    }
}