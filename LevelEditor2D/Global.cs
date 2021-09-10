using System.IO;
using System;

namespace LevelEditor2D
{
	static class Global
	{
		public const string CompanyName = "mikkul";
		public const string ProgramName = "LevelEditor2D";

		public static string AppDataPath
		{
			get
			{
				var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), CompanyName, ProgramName);
				return result;
			}
		}

		public static EditorPreferences EditorPreferences { get; set; }
	}
}
