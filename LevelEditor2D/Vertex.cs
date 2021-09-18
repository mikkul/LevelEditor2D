using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	public class Vertex : GameObject
	{
		private Vertex()
		{
		}

		private Vertex(float x, float y, int id) : base(id)
		{
			X = x;
			Y = y;
		}

		public float X { get; set; }
		public float Y { get; set; }
		[XmlIgnore]
		public List<Edge> ConnectedEdges { get; set; } = new List<Edge>();

		[XmlIgnore]
		[Browsable(false)]
		public Vector2 Position => new Vector2(X, Y);

		public static Vertex Create(float x, float y)
		{
			return new Vertex(x, y, Global.GetUniqueId());
		}

		public override bool Equals(GameObject other)
		{
			if(other is Vertex vertex)
			{
				return Id == vertex.Id;
			}
			return false;
		}

		public override string ToString()
		{
			return $"{{X: {X}, Y: {Y}}}";
		}
	}
}
