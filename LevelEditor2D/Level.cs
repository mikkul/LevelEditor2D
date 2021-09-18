using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelEditor2D
{
	public class Level : IEquatable<Level>
	{
		public Point2 Scale { get; set; }
		public List<GameObject> Objects { get; set; } = new List<GameObject>();

		public Level()
		{
		}

		public static Level Clone(Level level)
		{
			return new Level
			{
				Objects = level.Objects.ToList(),
				Scale = level.Scale,
			};
		}

		public bool Equals(Level other)
		{
			return Objects.Count == other.Objects.Count
				&& Objects.All(a => other.Objects.Any(b => a.Equals(b)));
		}
	}
}
