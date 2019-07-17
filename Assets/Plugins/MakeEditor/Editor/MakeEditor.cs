﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public static class MakeEditor
    {
        private static string _editorTemplatePath;
        
        static MakeEditor()
        {
            try
            {
                _editorTemplatePath = PathUtility.FindEditorTemplatePath();
            }
            catch(FileNotFoundException e)
            {
                Debug.LogError(e.Message);
            }
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80, validate = true)]
        public static bool ValidateCreateEditorScriptForSelection()
        {
            return Selection
                    .GetFiltered<MonoScript>(SelectionMode.Assets)
                    .Length > 0;
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScriptForSelection()
        {
            var selectedScripts = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);
            CreateEditorScript(selectedScripts);
        }

        public static void CreateEditorScript(params MonoScript[] selectedScripts)
        {
            if(selectedScripts == null)
            {
                throw new System.ArgumentNullException(nameof(selectedScripts));
            }

            if (selectedScripts.Length > 0)
            {
                Object lastCreatedObject = null;
                var editorScriptTemplate = File.ReadAllLines(_editorTemplatePath);

                foreach (var selectedScript in selectedScripts)
                {
                    var selectedScriptPath = AssetDatabase.GetAssetPath(selectedScript);

                    //deep copy to prevent template modifications
                    var newScriptCode = editorScriptTemplate.ToList();

                    lastCreatedObject = CreateScriptAsset(newScriptCode, selectedScriptPath);
                }

                Selection.activeObject = lastCreatedObject ?? Selection.activeObject;
            }
        }

        private static Object CreateScriptAsset(List<string> scriptCode, string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            if (script == null)
            {
                Debug.LogError("Select valid MonoScript object");

                return null;
            }

            var scriptContent = PrepareScriptContent(scriptCode, script);

            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
            if (asmPath != null)
            {
                var rootPath = Path.GetDirectoryName(asmPath);
                string outputPath = PathUtility.GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                AssetDatabase.Refresh();

                CreateAssembly(asmPath, outputPath);
                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
            }
            else
            {
                var rootPath = "Assets";
                string outputPath = PathUtility.GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
            }
        }

        private static void CreateAssembly(string subjectAsmDefPath, string newEditorScriptPath)
        {
            Debug.Log($"Subject assembly: {subjectAsmDefPath} for created editor script {newEditorScriptPath}");

            var outasmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (outasmPath != null)
            {
                var str = File.ReadAllText(outasmPath);
                AssemblyDefinition asmdef = JsonUtility.FromJson<AssemblyDefinition>(str);
                var guidRef = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);
                var requiredGuid = $"GUID:{guidRef}";

                if (!asmdef.IncludePlatforms.Contains("Editor"))
                {
                    var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);

                    var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                    var editorPath = Path.Combine(rootPath, "Editor");

                    var editorAsmDefPath = Path.Combine(editorPath, $"{refName}.Editor.asmdef");
                    var newAssemblyName = $"{refName}.Editor";

                    AssemblyDefinition editorAsmDef = new AssemblyDefinition(newAssemblyName)
                    {
                        References = new List<string>(1) { requiredGuid }
                    };

                    SaveAssemblyDefinition(editorAsmDef, editorAsmDefPath);
                }
                else if(!asmdef.References.Contains(requiredGuid) && !asmdef.References.Contains(asmdef.Name))
                {
                    asmdef.References.Add(requiredGuid);
                    SaveAssemblyDefinition(asmdef, outasmPath);
                }
            }
            else
            {
                throw new System.NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }
        }

        private static void SaveAssemblyDefinition(AssemblyDefinition assemblyDefinition, string savePath)
        {
            var serializedObject = JsonUtility.ToJson(assemblyDefinition, true);

            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            File.WriteAllText(savePath, serializedObject);
        }

        private static string PrepareScriptContent(List<string> scriptCode, MonoScript s)
        {
            //bench / try StringBuilder
            var type = s.GetClass();
            int namespaceIndex = scriptCode.FindIndex(str => str.Contains("#NAMESPACE#"));
            if (type.Namespace != null)
            {
                for (int i = namespaceIndex + 1; i < scriptCode.Count; ++i)
                {
                    if (scriptCode[i].Length > 0)
                    {
                        scriptCode[i] = scriptCode[i].Insert(0, "\t");
                    }
                }

                string usedNamespace = scriptCode[namespaceIndex].Replace("#NAMESPACE#", $"namespace {type.Namespace}");
                scriptCode[namespaceIndex] = "{";
                scriptCode.Insert(namespaceIndex, usedNamespace);
                scriptCode.Add("}");
            }
            else
            {
                scriptCode.RemoveAt(namespaceIndex);
            }

            for (int i = 0; i < scriptCode.Count; ++i)
            {
                scriptCode[i] = scriptCode[i].Replace("#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", scriptCode.ToArray());

            return finalCode;
        }

        private static class PathUtility
        {
            private const string _pluginAssemblyFilter = "t:asmdef Bloodstone.MakeEditor";
            private const string _templateFolder = "Templates";
            private const string _scriptExtension = ".cs";
            private const string _editorTemplateName = "editor_template.txt";

            public static string GetScriptPath(string rootPath, string subjectPath)
            {
                var editorPath = Path.Combine(rootPath, "Editor");
                var pathMod = Path.GetDirectoryName(subjectPath.Substring(rootPath.Length + 1)); //+1 to remove '/'

                var dirPath = Path.Combine(editorPath, pathMod);

                var name = Path.GetFileNameWithoutExtension(subjectPath);
                var outputPath = Path.Combine(dirPath, $"{name}Editor.cs");

                if (File.Exists(outputPath))
                {
                    outputPath = GenerateNotExistingFilename(outputPath);
                }

                return outputPath;
            }

            public static string FindEditorTemplatePath()
            {
                var guids = AssetDatabase.FindAssets(_pluginAssemblyFilter);
                if (guids.Length <= 0)
                {
                    throw new FileNotFoundException("Cannot find Bloodstone.MakeEditor assembly definition");
                }

                var pluginPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
                return Path.Combine(pluginPath, _templateFolder, _editorTemplateName);
            }

            private static string GenerateNotExistingFilename(in string path)
            {
                var directory = Path.GetDirectoryName(path);
                var fileName = Path.GetFileNameWithoutExtension(path);

                var newFileName = fileName + _scriptExtension;

                return Path.Combine(directory, newFileName);
            }
        }
    }
}