﻿using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace ExplorerWindows
{
	public sealed class CanvasExplorer : ExploreWindow<Canvas>
	{
		readonly string[] m_renderModeOptions =
		{
			"ScreenSpaceOverlay",
			"ScreenSpaceCamera",
			"WorldSpace",
		};

		Column[] m_columnsScreenOverlay;
		Column[] m_columnsScreenCamera;
		Column[] m_columnsWorldSpace;
		string[] m_sortingLayerNames;
		int[] m_sortingLayerUniquIDs;
		RenderMode m_renderMode;
		string m_searchString = string.Empty;
		bool m_lockList;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Window/Explorer/Canvas Explorer")]
		public static CanvasExplorer Open()
		{
			return GetWindow<CanvasExplorer>();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		protected override void OnEnable()
		{
			titleContent = new GUIContent("Canvas Explorer");
			minSize = new Vector2(500, 150);

			m_columnsScreenOverlay = new Column[]
			{
				new Column("Name", 120f, NameField),
				new Column("On", 26f, EnabledField),
				new Column("Sorting Layer", 100f, SortingLayerField),
				new Column("Order in Layer", 100f, SortingOrderField),
			};
			m_columnsScreenCamera = new Column[]
			{
				new Column("Name", 120f, NameField),
				new Column("On", 26f, EnabledField),
				new Column("Camera", 100f, CameraField),
				new Column("Sorting Layer", 100f, SortingLayerField),
				new Column("Order in Layer", 100f, SortingOrderField),
			};
			m_columnsWorldSpace = new Column[]
			{
				new Column("Name", 120f, NameField),
				new Column("On", 26f, EnabledField),
				new Column("Camera", 100f, CameraField),
				new Column("Sorting Layer", 100f, SortingLayerField),
				new Column("Order in Layer", 100f, SortingOrderField),
			};

			base.OnEnable();
		}

		protected override void OnGUI()
		{
			// 表示しながらレイヤーを編集している可能性も考慮して毎回更新する
			m_sortingLayerNames = GetSortingLayerNames();
			m_sortingLayerUniquIDs = GetSortingLayerUniqueIDs();

			base.OnGUI();
		}

		//------------------------------------------------------
		// abstract methods
		//------------------------------------------------------

		protected override Column[] GetColumns()
		{
			switch (m_renderMode)
			{
				default:
				case RenderMode.ScreenSpaceOverlay: return m_columnsScreenOverlay;
				case RenderMode.ScreenSpaceCamera: return m_columnsScreenCamera;
				case RenderMode.WorldSpace: return m_columnsWorldSpace;
			}
		}

		protected override List<Canvas> GetItemList(List<Canvas> prev) 
		{
			if (m_lockList)
			{
				prev.RemoveAll(i => i == null);
				return prev;
			}

			var tmp = new List<Canvas>(FindObjectsOfType<Canvas>().Where(i => i.renderMode == m_renderMode));
			if (!string.IsNullOrEmpty(m_searchString))
			{
				tmp.RemoveAll(i => !i.name.Contains(m_searchString));
			}

			tmp.Sort(CanvasCompareTo);

			return tmp;
		}

		int CanvasCompareTo(Canvas x, Canvas y)
		{
			int result = 0;

			switch (m_renderMode)
			{
				case RenderMode.ScreenSpaceCamera:
				case RenderMode.WorldSpace:
					result = GetCameraDepth(x).CompareTo(GetCameraDepth(y));
					if (result != 0) return result;
					break;
			}

			result = Array.IndexOf(m_sortingLayerUniquIDs, x.sortingLayerID).CompareTo(Array.IndexOf(m_sortingLayerUniquIDs, y.sortingLayerID));
			if (result != 0) return result;

			result = x.sortingOrder.CompareTo(y.sortingOrder);
			if (result != 0) return result;

			return x.name.CompareTo(y.name);
		}

		static float GetCameraDepth(Canvas canvas)
		{
			return canvas.worldCamera ? canvas.worldCamera.depth : 0f;
		}

		protected override void DrawHeader()
		{
			GUI.enabled = !m_lockList;
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(30);
				m_renderMode = (RenderMode)GUILayout.Toolbar((int)m_renderMode, m_renderModeOptions, GUILayout.Height(24));
				GUILayout.Space(30);
			}
			GUI.enabled = true;

			using (new EditorGUILayout.HorizontalScope())
			{
				m_lockList = GUILayout.Toggle(m_lockList, "Lock List");

				m_searchString = GUILayout.TextField(m_searchString, "SearchTextField", GUILayout.Width(300));
				if (GUILayout.Button(GUIContent.none, "SearchCancelButton"))
				{
					m_searchString = string.Empty;
					GUI.FocusControl(null);
				}
			}
		}


		//------------------------------------------------------
		// Canvas column field
		//------------------------------------------------------

		void NameField(Rect r, Canvas canvas)
		{
			EditorGUI.LabelField(r, canvas.name, m_labelStyle);
		}

		void EnabledField(Rect r, Canvas canvas)
		{
			canvas.enabled = EditorGUI.Toggle(r, canvas.enabled);
		}

		void CameraField(Rect r, Canvas canvas)
		{
			canvas.worldCamera = EditorGUI.ObjectField(r, canvas.worldCamera, typeof(Camera), true) as Camera;
		}

		void SortingLayerField(Rect r, Canvas canvas)
		{
			canvas.sortingLayerID = EditorGUI.IntPopup(r, canvas.sortingLayerID,
				m_sortingLayerNames,
				m_sortingLayerUniquIDs);
		}

		void SortingOrderField(Rect r, Canvas canvas)
		{
			canvas.sortingOrder = EditorGUI.IntField(r, canvas.sortingOrder);
		}


		//------------------------------------------------------
		// unity internals
		//------------------------------------------------------

		static string[] GetSortingLayerNames()
		{
			Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			return (string[])sortingLayersProperty.GetValue(null, new object[0]);
		}

		static int[] GetSortingLayerUniqueIDs()
		{
			Type internalEditorUtilityType = typeof(InternalEditorUtility);
			PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
			return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
		}
	}
}