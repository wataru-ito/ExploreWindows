﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ExplorerWindows
{
	/// <summary>
	/// Componentの一覧を表示するエディタの基底クラス
	/// </summary>
	public abstract class ExploreWindow<T> : EditorWindow
		where T : Component
	{
		protected delegate void ColumnDrawer(Rect position, T item, bool selected);

		protected class Column
		{
			public readonly string name;
			public ColumnDrawer drawer;
			float m_width;
			float m_widthMin;
			bool m_flexible;

			public Column(string name, float width, ColumnDrawer drawer, float widthMin = 20, bool flexible = true)
			{
				this.name = name;
				this.drawer = drawer; 
				m_width = Mathf.Max(widthMin, width);
				m_widthMin = widthMin;
				m_flexible = flexible;
			}

			public float width
			{ 
				get { return m_width; }
				set { if (m_flexible) m_width = Mathf.Max(m_widthMin, value); }
			}
		}

		const float kHeaderHeight = 28f;
		const float kItemHeight = 16f;
		const float kSepalatorWidth = 4;
		const float kItemPaddingX = 4;

		Column m_nameColumn;
		List<T> m_itemList = new List<T>();

		Vector2 m_scrollPosition;
		GUIStyle m_labelStyle;
		GUIStyle m_backgroundStyle;

		Column m_selected;
		Vector2 m_dragBeganPosition;
		float m_widthBegan;



		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		protected virtual void OnEnable()
		{
			InitGUI();
		}

		protected virtual void OnFocus()
		{
			GUI.FocusControl(string.Empty);
		}

		protected virtual void OnSelectionChange()
		{
			Repaint();
		}

		protected virtual void OnInspectorUpdate()
		{
			// 他のインスペクタで更新された値を更新するために毎フレーム更新する
			// > インスペクタで値が変更された時のコールバックないかな？
			Repaint();
		}

		protected virtual void OnGUI()
		{
			if (m_labelStyle == null)
			{
				EditorGUILayout.HelpBox("guiskin initialize failed.", MessageType.Error);
				return;
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(12);
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Space(8);
					DrawHeader();
					DrawList();
					GUILayout.Space(4);
				}
				GUILayout.Space(12);
			}

			if (m_selected != null)
			{
				EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.ResizeHorizontal);
			}
		}


		//------------------------------------------------------
		// abstract methods
		//------------------------------------------------------

		protected abstract Column[] GetColumns();
		protected abstract List<T> GetItemList(List<T> prev);
		protected abstract void DrawHeader();


		//------------------------------------------------------
		// events
		//------------------------------------------------------

	

		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void InitGUI()
		{
			m_nameColumn = new Column("Name", 120f, NameField, 100f);
			
			// 以前は自前のguiskinを持っていたが、free/proのスキン切替失念してた。
			// 今のスキンから複製する方が安い
			var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
			m_backgroundStyle = skin.FindStyle("CN EntryBackOdd");

			var style = skin.FindStyle("Hi Label");
			if (style != null)
			{
				m_labelStyle = new GUIStyle(style);
				m_labelStyle.padding.left = 4;
			}
		}

		void DrawList()
		{
			var columns = GetColumns();
			m_itemList = GetItemList(m_itemList);

			var r = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			if (Event.current.type == EventType.Repaint)
			{
				GUI.skin.box.Draw(r, GUIContent.none, 0);
			}

			var height = DrawColumns(r, columns);
			r.y += height;
			r.height -= height;

			// この後描画されるbackgrounで枠線が消えてしまうので削る
			r.x += 1f;
			r.width -= 2f;
			r.height -= 1f;
			DrawItems(r, columns);
		}

		//------------------------------------------------------
		// カラム部
		//------------------------------------------------------

		float DrawColumns(Rect position, Column[] columns)
		{
			var columnRect = new Rect(position.x, position.y, position.width, kHeaderHeight);
			GUI.Box(columnRect, GUIContent.none);

			var viewRect = new Rect(0, 0, position.width, kHeaderHeight);
			using (new GUI.ScrollViewScope(columnRect, m_scrollPosition, viewRect, GUIStyle.none, GUIStyle.none))
			{
				var x = kItemPaddingX;
				x = DrawColumn(x, m_nameColumn);
				foreach (var column in columns)
				{
					x = DrawColumn(x, column);
				}
			}

			return kHeaderHeight;
		}

		float DrawColumn(float x, Column column)
		{
			var position = new Rect(x,
				kHeaderHeight - kItemHeight - 2,
				column.width,
				kItemHeight);
			
			EditorGUI.LabelField(position, column.name);
			position.x += position.width;

			position = new Rect(position.x, position.y - 6, kSepalatorWidth, position.height + 4);
			DrawSeparator(position, column);

			return position.xMax;
		}

		void DrawSeparator(Rect position, Column column)
		{
			EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);

			var controlID = GUIUtility.GetControlID(FocusType.Passive);
			var ev = Event.current;
			switch (ev.GetTypeForControl(controlID))
			{
				case EventType.Repaint:
					// 微妙にLightingExplorerの仕切りと違うのでもっといいスタイルを探す
					// > むしろ Handle.DrawLine か？
					EditorGUI.LabelField(position, GUIContent.none, "DopesheetBackground");
					break;

				case EventType.MouseDown:
					if (position.Contains(ev.mousePosition) && ev.button == 0)
					{
						GUIUtility.hotControl = controlID;
						m_selected = column;
						m_dragBeganPosition = ev.mousePosition;
						m_widthBegan = column == null ? 0 : column.width;
						ev.Use();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID && m_selected != null)
					{
						m_selected.width = m_widthBegan + (ev.mousePosition.x - m_dragBeganPosition.x);
						GUI.changed = true;
						ev.Use();
					}
					break;

				case EventType.MouseUp:
					if(GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
						m_selected = null;
						ev.Use();
					}
					break;
			}
		}

		//------------------------------------------------------
		// アイテム
		//------------------------------------------------------

		void DrawItems(Rect position, Column[] columns)
		{
			var viewRect = new Rect(0, 0,
				GetContentWidth(columns),
				m_itemList.Count * kItemHeight);

			using (var scroll = new GUI.ScrollViewScope(position, m_scrollPosition, viewRect))
			{
				var ev = Event.current;

				var min = Mathf.FloorToInt(m_scrollPosition.y / kItemHeight);
				var max = min + Mathf.CeilToInt(position.height / kItemHeight);

				// 背景の縞々
				if (ev.type == EventType.Repaint)
				{
					var prev = GUI.color;
					var gray = new Color(prev.r * 0.95f, prev.g * 0.95f, prev.b * 0.95f);
					for (int i = min; i < max; ++i)
					{
						var area = new Rect(m_scrollPosition.x, i * kItemHeight, position.width, kItemHeight);
						GUI.color = i % 2 == 1 ? prev : gray;
						m_backgroundStyle.Draw(area, GUIContent.none, 0);
					}
					GUI.color = prev;
				}

				// 本体
				// 未表示部分も回さないとControlIDズレるんだよなぁ…
				for (int i = min; i < Mathf.Min(m_itemList.Count, max); ++i)
				{
					var itemPosition = new Rect(0, i * kItemHeight, position.width, kItemHeight);
					DrawItem(itemPosition, columns, m_itemList[i]);
				}

				switch (ev.type)
				{
					case EventType.MouseDown:
						SelectItem(ev);
						break;
				}

				m_scrollPosition = scroll.scrollPosition;
			}			
		}

		void SelectItem(Event ev)
		{
			if (ev.mousePosition.y < 0)
				return;

			var index = Mathf.FloorToInt(ev.mousePosition.y / kItemHeight);
			if (index >= m_itemList.Count)
			{
				Selection.activeGameObject = null;
				return;
			}

			if (IsSelectionAdditive(ev))
			{
				var targetGO = m_itemList[index].gameObject;
				var gos = new List<GameObject>(Selection.gameObjects);
				if (gos.Contains(targetGO))
				{
					gos.Remove(targetGO);
					if (Selection.activeGameObject == targetGO)
					{
						Selection.activeGameObject = gos.Count > 0 ? gos[0] : null;
					}
				}
				else
				{
					gos.Add(targetGO);
				}
				Selection.objects = gos.ToArray();
				ev.Use();
				return;
			}
			else if (ev.shift)
			{
				var firstItem = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<T>() : null;
				var firstIndex = m_itemList.IndexOf(firstItem);
				if (firstIndex >= 0 && index != firstIndex)
				{
					var diff = index - firstIndex;
					var objects = new UnityEngine.Object[Mathf.Abs(diff) + 1];
					var step = diff > 0 ? 1 : -1;
					for (int i = 0; i < objects.Length; ++i, firstIndex += step)
					{
						objects[i] = m_itemList[firstIndex].gameObject;
					}
					Selection.objects = objects;
					ev.Use();
					return;
				}
			}

			Selection.activeGameObject = m_itemList[index].gameObject;
			ev.Use();
		}

		bool IsSelectionAdditive(Event ev)
		{
			#if UNITY_EDITOR_OSX
			return ev.command;
			#else
			return ev.control;
			#endif
		}


		float GetContentWidth(Column[] columns)
		{
			float width = kItemPaddingX * 2f;
			width += m_nameColumn.width + kSepalatorWidth;
			foreach (var column in columns)
			{
				width += column.width + kSepalatorWidth;
			}
			return width;
		}

		void DrawItem(Rect position, Column[] columns, T item)
		{
			var selected = Selection.gameObjects.Contains(item.gameObject);
			if (Event.current.type == EventType.Repaint)
			{
				m_labelStyle.Draw(position, selected && focusedWindow == this, false, selected, false);
			}

			position.x += kItemPaddingX;
			position.x = DrawItem(position, m_nameColumn, item, selected);
			foreach (var column in columns)
			{
				position.x = DrawItem(position, column, item, selected);
			}
		}

		float DrawItem(Rect position, Column column, T item, bool selected)
		{
			position.width = column.width;
			column.drawer(position, item, selected);
			return position.xMax + kSepalatorWidth;
		}

		void NameField(Rect r, T component, bool selected)
		{
			if (Event.current.type == EventType.Repaint)
			{
				m_labelStyle.Draw(r, new GUIContent(component.name), selected && focusedWindow == this, false, selected, false);
			}
		}
	}
}