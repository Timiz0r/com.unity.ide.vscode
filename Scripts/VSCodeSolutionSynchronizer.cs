﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VSCodePackage;

public class VSCodeSolutionSynchronizer : ScriptableObject
{
    static VSCodeScriptEditor s_ScriptEditor;

    [MenuItem("VSCode/Generate")]
    public static void Test()
    {
    }

    public static void RegisterScriptEditor(VSCodeScriptEditor scriptEditor)
    {
        s_ScriptEditor = scriptEditor;
    }
}

[InitializeOnLoad]
public class VSCodeScriptEditor : IExternalScriptEditor
{
    VSCodeDiscovery m_Discoverability;
    ProjectGeneration m_ProjectGeneration;
    string m_Arguments;

    public bool TryGetInstallationForPath(string editorPath, out ScriptEditor.Installation installation)
    {
        var lowerCasePath = editorPath.ToLower();
        var filename = Path.GetFileName(lowerCasePath).Replace(" ", "");
        if (filename.StartsWith("code"))
        {
            try
            {
                installation = Installations.First(inst => inst.Path == editorPath);
            }
            catch (InvalidOperationException)
            {
                installation = new ScriptEditor.Installation
                {
                    Name = "Visual Studio Code",
                    Path = editorPath
                };
            }

            return true;
        }

        installation = default(ScriptEditor.Installation);
        return false;
    }

    public void OnGUI()
    {
    }

    public void SyncIfNeeded(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles)
    {
        m_ProjectGeneration.SyncIfNeeded(affectedFiles, reimportedFiles);
    }

    public void Sync()
    {
        m_ProjectGeneration.GenerateSolutionAndProjectFiles();
    }

    public void Initialize(string editorInstallationPath)
    {
    }

    public bool OpenFileAtLine(string path, int line)
    {
        line = line == -1 ? 0 : line;
        var argument = $"{m_ProjectGeneration.ProjectDirectory}" + (path.Length == 0 ? "" : $" -g {path}:{line}");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = EditorPrefs.GetString("kScriptsDefaultApp"),
                Arguments = argument,
                UseShellExecute = true,
            }
        };

        process.Start();
        return true;
    }

    public string DefaultArgument { get; } = "\"$(ProjectPath)\" -g \"$(File)\":$(Line)";

    public string Arguments
    {
        get
        {
            if (m_Arguments == null)
            {
                m_Arguments = EditorPrefs.GetString("vscode_arguments", DefaultArgument);
            }

            return m_Arguments;
        }
        set
        {
            m_Arguments = value;
            EditorPrefs.SetString("vscode_arguments", value);
        }
    }

    public bool CustomArgumentsAllowed => true;

    public ScriptEditor.Installation[] Installations => m_Discoverability.PathCallback();

    public VSCodeScriptEditor()
    {
        m_Discoverability = new VSCodeDiscovery();
        m_ProjectGeneration = new ProjectGeneration();
    }

    static VSCodeScriptEditor()
    {
        ScriptEditor.Register(new VSCodeScriptEditor());
    }
}