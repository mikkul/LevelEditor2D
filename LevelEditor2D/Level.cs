using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelEditor2D
{
	public class Level : IEquatable<Level>
	{
		public List<Vertex> Vertices { get; set; } = new List<Vertex>();
		public List<Edge> Edges { get; set; } = new List<Edge>();

		public static Level Clone(Level level)
		{
			return new Level
			{
				Vertices = new List<Vertex>(level.Vertices),
			};
		}

		public bool Equals(Level other)
		{
			return Vertices.Count == other.Vertices.Count
				&& Vertices.All(a => other.Vertices.Any(b => a.Equals(b)));
		}
	}
}
