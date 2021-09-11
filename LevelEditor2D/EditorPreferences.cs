using Microsoft.Xna.Framework;
using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	public class EditorPreferences
	{
		public const string PreferencesFileName = "editor_preferences.config";
		private Color _toolbarBackground;
		private Color _gridLinesColor;
		private Color _backgroundColor;
		private Color _vertexColor;
		private Color _selectedVertexColor;
		private Color _toolbarButtonBackground;
		private Color _toolbarButtonHoverBackground;
		private Color _edgeColor;

		public static string StateFilePath
		{
			get
			{
				var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Global.CompanyName, Global.ProgramName, PreferencesFileName);
				return result;
			}
		}

		public event EventHandler SidebarBackgroundChanged;
		public event EventHandler ToolbarButtonBackgroundChanged;
		public event EventHandler ToolbarButtonHoverBackgroundChanged;

		[DisplayName("Background color")]
		public Color BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
			}
		}
		[DisplayName("Grid lines color")]
		public Color GridLinesColor
		{
			get => _gridLinesColor;
			set
			{
				_gridLinesColor = value;
			}
		}
		[DisplayName("Toolbar background")]
		public Color SidebarBackground
		{
			get => _toolbarBackground;
			set
			{
				_toolbarBackground = value;
				SidebarBackgroundChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		[DisplayName("Toolbar button background")]
		public Color ToolbarButtonBackground
		{
			get => _toolbarButtonBackground;
			set
			{
				_toolbarButtonBackground = value;
				ToolbarButtonBackgroundChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		[DisplayName("Toolbar button hover background")]
		public Color ToolbarButtonHoverBackground
		{
			get => _toolbarButtonHoverBackground;
			set
			{
				_toolbarButtonHoverBackground = value;
				ToolbarButtonHoverBackgroundChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		[DisplayName("Vertex color")]
		public Color VertexColor
		{
			get => _vertexColor;
			set => _vertexColor = value;
		}
		[DisplayName("Selected vertex color")]
		public Color SelectedVertexColor
		{
			get => _selectedVertexColor;
			set => _selectedVertexColor = value;
		}
		[DisplayName("Edge color")]
		public Color EdgeColor 
		{ 
			get => _edgeColor; 
			set => _edgeColor = value; 
		}

		[Browsable(false)]
		public float LeftSplitterPosition { get; set; }
		[Browsable(false)]
		public float RightSplitterPosition { get; set; }

		public void Save()
		{
			using (var stream = new StreamWriter(StateFilePath, false))
			{
				var serializer = new XmlSerializer(typeof(EditorPreferences));
				serializer.Serialize(stream, this);
			}
		}

		public static EditorPreferences Load()
		{
			if (!File.Exists(StateFilePath))
			{
				return null;
			}

			EditorPreferences state;
			using (var stream = new StreamReader(StateFilePath))
			{
				var serializer = new XmlSerializer(typeof(EditorPreferences));
				state = (EditorPreferences)serializer.Deserialize(stream);
			}

			return state;
		}
	}
}
