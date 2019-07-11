﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    /// <summary>
    /// Modification and serialization helper class for Unity's 
    /// <see href="https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html"> asmdef files</see>
    /// </summary>
    [Serializable]
    public class AssemblyDefinition
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private bool autoReferenced;
        [SerializeField]
        private bool allowUnsafeCode;
        [SerializeField]
        private bool overrideReferences;
        [SerializeField]
        private List<string> references;
        [SerializeField]
        private List<string> versionDefines;
        [SerializeField]
        private List<string> includePlatforms;
        [SerializeField]
        private List<string> excludePlatforms;
        [SerializeField]
        private List<string> defineConstraints;
        [SerializeField]
        private List<string> precompiledReferences;
        [SerializeField]
        private List<string> optionalUnityReferences;

        public string Name
        {
            get => name;
            set => name = value;
        }
        public bool AutoReferenced
        {
            get => autoReferenced;
            set => autoReferenced = value;
        }
        public bool AllowUnsafeCode
        {
            get => allowUnsafeCode;
            set => allowUnsafeCode = value;
        }
        public bool OverrideReferences
        {
            get => overrideReferences;
            set => overrideReferences = value;
        }
        public List<string> References
        {
            get => references;
            set => references = value;
        }
        public List<string> VersionDefines
        {
            get => versionDefines;
            set => versionDefines = value;
        }
        public List<string> IncludePlatforms
        {
            get => includePlatforms;
            set => includePlatforms = value;
        }
        public List<string> ExcludePlatforms
        {
            get => excludePlatforms;
            set => excludePlatforms = value;
        }
        public List<string> DefineConstraints
        {
            get => defineConstraints;
            set => defineConstraints = value;
        }
        public List<string> PrecompiledReferences
        {
            get => precompiledReferences;
            set => precompiledReferences = value;
        }
        public List<string> OptionalUnityReferences
        {
            get => optionalUnityReferences;
            set => optionalUnityReferences = value;
        }
    }
}