using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelEditor2D
{
	public class Level : IEquatable<Level>
	{
		public List<GameObject> Objects { get; set; } = new List<GameObject>();

		public Level()
		{
		}

		public static Level Clone(Level level)
		{
			return new Level
			{
				Objects = new List<GameObject>(level.Objects.ToList()),
			};
		}

		public bool Equals(Level other)
		{
			return Objects.Count == other.Objects.Count
				&& Objects.All(a => other.Objects.Any(b => a.Equals(b)));
		}
	}
}
