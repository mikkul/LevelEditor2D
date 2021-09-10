using System;
using System.Diagnostics.CodeAnalysis;

namespace LevelEditor2D
{
	public class Vertex : IEquatable<Vertex>
	{
		public Vertex()
		{
		}

		public Vertex(float x, float y)
		{
			X = x;
			Y = y;
		}

		public float X { get; set; }
		public float Y { get; set; }

		public bool Equals([AllowNull] Vertex other)
		{
			return X == other.X && Y == other.Y;
		}

		public override string ToString()
		{
			return $"{{X: {X}, Y: {Y}}}";
		}
	}
}
