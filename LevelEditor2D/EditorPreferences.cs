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
		private Color _toolbarBackgroundColor;
		private Color _gridLinesColor;
		private Color _backgroundColor;

		public static string StateFilePath
		{
			get
			{
				var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Global.CompanyName, Global.ProgramName, PreferencesFileName);
				return result;
			}
		}

		public event EventHandler BackgroundColorChanged;
		public event EventHandler GridLinesColorChanged;
		public event EventHandler ToolbarBackgroundColorChanged;

		[DisplayName("Background color")]
		public Color BackgroundColor 
		{ 
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
				BackgroundColorChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		[DisplayName("Grid lines color")]
		public Color GridLinesColor 
		{ 
			get => _gridLinesColor;
			set
			{
				_gridLinesColor = value;
				GridLinesColorChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		[DisplayName("Toolbar background")]
		public Color ToolbarBackgroundColor 
		{ 
			get => _toolbarBackgroundColor;
			set
			{
				_toolbarBackgroundColor = value;
				ToolbarBackgroundColorChanged?.Invoke(this, EventArgs.Empty);
			}
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
