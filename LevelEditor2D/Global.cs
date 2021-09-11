using System.IO;
using System;
using System.Linq;

namespace LevelEditor2D
{
	static class Global
	{
		public const string CompanyName = "mikkul";
		public const string ProgramName = "LevelEditor2D";

		public const int DefaultGridCellSize = 32;
		public const float SelectionDistanceTolerance = 10;
		public const float VertexRenderSize = 10;
		public const float EdgeRenderSize = 5;

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

		private static int _idCounter;

		public static EditorPreferences EditorPreferences { get; set; }

		public static void InitIds(Level level)
		{
			if(level.Vertices.Count == 0)
			{
				_idCounter = 0;
				return;
			}

			_idCounter = level.Vertices.Max(v => v.Id);
		}

		public static int GetUniqueId()
		{
			return ++_idCounter;
		}
	}
}
