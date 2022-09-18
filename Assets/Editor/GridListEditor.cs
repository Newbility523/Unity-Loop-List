using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using Engine;

[CustomEditor(typeof(GridList), true)]
[CanEditMultipleObjects]
public class GridListEditor : Editor
{
    SerializedProperty padding;
    SerializedProperty startCorner;
    SerializedProperty horizontal;
    SerializedProperty vertical;
    SerializedProperty cellSize;
    SerializedProperty spacing;
    SerializedProperty viewport;
    SerializedProperty content;
    SerializedProperty itemCountInLine;
    SerializedProperty visibleLineCount_Vertical;

    private void OnEnable()
    {
        padding = serializedObject.FindProperty("padding");
        startCorner = serializedObject.FindProperty("startCorner");
        horizontal = serializedObject.FindProperty("m_Horizontal");
        vertical = serializedObject.FindProperty("m_Vertical");
        cellSize = serializedObject.FindProperty("cellSize");
        spacing = serializedObject.FindProperty("spaceing");
        viewport = serializedObject.FindProperty("m_Viewport");
        content = serializedObject.FindProperty("m_Content");
        itemCountInLine = serializedObject.FindProperty("itemCountInLine");
        visibleLineCount_Vertical = serializedObject.FindProperty("visibleLineCount_Vertical");
    }

    /// <summary>
    /// TODO: 多选的情况所有的GridList的Viewport和Content都会被修改为最后一个GridList的
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GridList comp = target as GridList;

        EditorGUILayout.PropertyField(startCorner, new GUIContent("Start Corner"));

        horizontal.boolValue = EditorGUILayout.Toggle("Horizontal", comp.Horizontal);
		vertical.boolValue = EditorGUILayout.Toggle("Vertical", comp.Vertical);
		cellSize.vector2Value = EditorGUILayout.Vector2Field("Cell Size", comp.CellSize);
		spacing.vector2Value = EditorGUILayout.Vector2Field("Spacing", comp.Spacing);
		EditorGUILayout.PropertyField(padding, new GUIContent("Padding"));

		viewport.objectReferenceValue = (RectTransform)EditorGUILayout.ObjectField("Viewport", comp.viewport, typeof(RectTransform), true);
		content.objectReferenceValue = (RectTransform)EditorGUILayout.ObjectField("Content", comp.content, typeof(RectTransform), true);

		itemCountInLine.intValue = EditorGUILayout.IntField("Item Count In Line", comp.itemCountInLine);
        visibleLineCount_Vertical.intValue = EditorGUILayout.IntField("Visible Line Count", comp.visibleLineCount_Vertical);

		serializedObject.ApplyModifiedProperties();
    }
}
