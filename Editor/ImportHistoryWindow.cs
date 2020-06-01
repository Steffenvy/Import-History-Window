﻿#define FOLDER_BUTTON

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class ImportHistoryProcessing : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var windows = Resources.FindObjectsOfTypeAll<ImportHistoryWindow>();

        for (int i = 0; i < windows.Length; i++)
        {
            var w = windows[i];

            for (int ii = 0; ii < deletedAssets.Length; ii++)
                w.history.Remove(deletedAssets[ii]);
            for (int ii = 0; ii < movedFromAssetPaths.Length; ii++)
                w.history.Remove(movedFromAssetPaths[ii]);

            for (int ii = 0; ii < importedAssets.Length; ii++)
                w.Add(importedAssets[ii]);
            for (int ii = 0; ii < movedAssets.Length; ii++)
                w.Add(movedAssets[ii]);
        }
    }
}

public class ImportHistoryWindow : EditorWindow, IHasCustomMenu
{
    //Constants
    private static readonly string[] IGNORED_EXTENSIONS = new string[]
    {
        "", //Most often a folder
        ".afdesign",
    };

    private const int HISTORY_LENGTH = 32;
    private const float DOUBLE_CLICK_TIME = 0.5f;
    private const float HEIGHT = 20;
    private const int MARGIN = 0;



    //Fields
    public List<string> history = new List<string>();
    public Vector2 scrollPosition;
    private GUIStyle pingStyle, folderStyle;
    private Texture folderIcon;
    private UnityEngine.Object previouslyClicked;
    private double clickTime;



    //Methods
    [MenuItem("Window/Import History")]
    static void ShowWindow()
    {
        ImportHistoryWindow window = CreateInstance<ImportHistoryWindow>();
        window.titleContent = new GUIContent("Import History");
        window.Show();
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(EditorGUIUtility.TrTextContent("Clear"), false, Clear);
    }
    private void Clear()
    {
        history.Clear();
    }

    public void Add(string path)
    {
        //Ignores by extension
        var lower = path.ToLowerInvariant();
        for (int i = 0; i < IGNORED_EXTENSIONS.Length; i++)
        {
            var ext = IGNORED_EXTENSIONS[i];

            if (Path.GetExtension(lower) == ext)
                return;
        }

        //Removes Duplicates
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if(history[i] == path)
                history.RemoveAt(i);
        }
        
        //Adds
        history.Insert(0, path);

        //Removes extra
        while (history.Count > HISTORY_LENGTH)
            history.RemoveAt(HISTORY_LENGTH);
    }



    //Lifecycle
    private void OnEnable()
    {
        minSize = new Vector2(200, 50);

        folderIcon = Resources.Load<Texture>("IHW Folder Icon");
    }

    public void OnGUI()
    {
        //Creates Styles
        if (pingStyle == null)
        {
            folderStyle = new GUIStyle(GUI.skin.button);

            folderStyle.margin.bottom = MARGIN;
            folderStyle.margin.top = MARGIN;
            folderStyle.margin.left = MARGIN;
            folderStyle.margin.right = MARGIN;

            pingStyle = new GUIStyle(folderStyle);
            pingStyle.alignment = TextAnchor.MiddleLeft; // MiddleRight;
        }


        //Widths and Height
        float windowWidth = position.width - 13;

#if FOLDER_BUTTON
        const float FOLDER_WIDTH = 50;
        var pingWidth = GUILayout.Width(windowWidth - FOLDER_WIDTH - MARGIN * 2);
        var folderWidth = GUILayout.Width(FOLDER_WIDTH - MARGIN * 2);
#else
        var pingWidth = GUILayout.Width(windowWidth);
#endif
        var height = GUILayout.Height(HEIGHT);


        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
        {
            for (int i = 0; i < history.Count; i++)
            {
                var path = history[i];


                //Removes if can't be loaded
                var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                if (asset == null)
                {
                    history.RemoveAt(i);
                    i--;
                    continue;
                }


                GUILayout.BeginHorizontal(GUILayout.Width(windowWidth));
                {
                    //Ping Button
                    string name = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileName(path);

                    if (GUILayout.Button(name, pingStyle, pingWidth, height))
                    {
                        //Pings and selects
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;

                        var clickDelay = EditorApplication.timeSinceStartup - clickTime;
                        if (previouslyClicked == asset && clickDelay < DOUBLE_CLICK_TIME)
                        {
                            //Double clicked, will open
                            AssetDatabase.OpenAsset(asset);
                            previouslyClicked = null;
                        }
                        else
                        {
                            //Prepares double click
                            previouslyClicked = asset;
                            clickTime = EditorApplication.timeSinceStartup;
                        }
                    }


#if FOLDER_BUTTON
                    //Folder Button
                    if (GUILayout.Button(folderIcon, folderStyle, folderWidth, height))
                    {
                        System.Diagnostics.Process.Start(Path.GetDirectoryName(path));
                    }
#endif
                }
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}