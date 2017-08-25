using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;


namespace ExplorerWindows
{
	public sealed class CameraExplorer : ExploreWindow<Camera>
	{
		Column[] m_columns;
		string m_searchString = string.Empty;
		string[] m_layerOptions;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Window/Explorer/Camera Explorer")]
		public static CameraExplorer Open()
		{
			return GetWindow<CameraExplorer>();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		protected override void OnEnable()
		{
			titleContent = new GUIContent("Camera Explorer");
			minSize = new Vector2(340, 150);

			m_columns = new Column[]
			{
				new Column("Name", 120f, NameField),
				new Column("On", 26f, EnabledField),
				new Column("Depth", 60f, DepthField),
				new Column("Culling Mask", 120f, CullingMaskField),
				new Column("Clear Flags", 200f, ClearFlagsField),
			};

			base.OnEnable();
		}

		protected override void OnGUI()
		{
			m_layerOptions = Enumerable.Range(0, 32)
				.Select(i => LayerMask.LayerToName(i))
				.ToArray();
	
			base.OnGUI();
		}


		//------------------------------------------------------
		// abstract methods
		//------------------------------------------------------

		protected override Column[] GetColumns()
		{
			return m_columns;
		}

		protected override List<Camera> GetItemList(List<Camera> prev)
		{
			var tmp = new List<Camera>(Camera.allCameras);

			// ここで寝かせた奴はここで有効にしたいので追加しておく
			// > 通常寝た奴はそもそもCamera.allCamerasで取得されない
			tmp.AddRange(prev.Where(i => i && !i.enabled));
			tmp.Sort(CameraCompareTo);

			if (!string.IsNullOrEmpty(m_searchString))
			{
				tmp.RemoveAll(i => !i.name.Contains(m_searchString));
			}

			return tmp;
		}

		static int CameraCompareTo(Camera x, Camera y)
		{
			var result = x.depth.CompareTo(y.depth);
			return result == 0 ? x.name.CompareTo(y.name) : result;
		}

		protected override void DrawHeader()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				m_searchString = GUILayout.TextField(m_searchString, "SearchTextField", GUILayout.Width(300));
				if (GUILayout.Button(GUIContent.none, "SearchCancelButton"))
				{
					m_searchString = string.Empty;
					GUI.FocusControl(null);
				}
			}
		}


		//------------------------------------------------------
		// camera column field
		//------------------------------------------------------

		void NameField(Rect r, Camera camera)
		{
			EditorGUI.LabelField(r, camera.name, m_labelStyle);
		}

		void EnabledField(Rect r, Camera camera)
		{
			camera.enabled = EditorGUI.Toggle(r, camera.enabled);
		}

		void DepthField(Rect r, Camera camera)
		{
			camera.depth = EditorGUI.FloatField(r, camera.depth);
		}

		void CullingMaskField(Rect r, Camera camera)
		{
			camera.cullingMask = EditorGUI.MaskField(r, GUIContent.none, camera.cullingMask, m_layerOptions);
		}

		void ClearFlagsField(Rect r, Camera camera)
		{
			camera.clearFlags = (CameraClearFlags)EditorGUI.EnumPopup(r, camera.clearFlags);
		}
	}
}