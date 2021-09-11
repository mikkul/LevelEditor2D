using System.IO;
using System;

namespace LevelEditor2D
{
	static class Global
	{
		public const string CompanyName = "mikkul";
		public const string ProgramName = "LevelEditor2D";

		public const int DefaultGridCellSize = 32;
		public const float SelectionDistanceTolerance = 10;
		public const float VertexRenderSize = 10;

		public static float SelectionDistanceToleranceSquared
		{
			get
			{
				return SelectionDistanceTolerance * SelectionDistanceTolerance;
			}
		}

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
