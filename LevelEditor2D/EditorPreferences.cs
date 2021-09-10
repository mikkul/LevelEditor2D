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

		public static string StateFilePath
		{
			get
			{
				var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Global.CompanyName, Global.ProgramName, PreferencesFileName);
				return result;
			}
		}

		[DisplayName("Background color")]
		public Color BackgroundColor { get; set; }
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
