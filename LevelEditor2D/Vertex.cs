using System;

namespace LevelEditor2D
{
	public class Vertex : GameObject, IEquatable<Vertex>
	{
		private Vertex()
		{
		}

		private Vertex(float x, float y, int id)
		{
			X = x;
			Y = y;
			Id = id;
		}

		public int Id { get; set; }
		public float X { get; set; }
		public float Y { get; set; }

		public static Vertex Create(float x, float y)
		{
			return new Vertex(x, y, Global.GetUniqueId());
		}

		public bool Equals(Vertex other)
		{
			return Id == other.Id;
		}

		public override string ToString()
		{
			return $"{{X: {X}, Y: {Y}}}";
		}
	}
}
