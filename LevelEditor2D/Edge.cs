using System.ComponentModel;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	public class Edge : GameObject
	{
		private Edge()
		{
		}

		private Edge(Vertex a, Vertex b, int id) : base(id)
		{
			A = a;
			B = b;
			VertexAId = A.Id;
			VertexBId = B.Id;
		}

		[Browsable(false)]
		[XmlIgnore]
		public Vertex A { get; set; }
		[Browsable(false)]
		[XmlIgnore]
		public Vertex B { get; set; }

		[Browsable(false)]
		public int VertexAId { get; set; }
		[Browsable(false)]
		public int VertexBId { get; set; }

		public static Edge Create(Vertex a, Vertex b)
		{
			return new Edge(a, b, Global.GetUniqueId());
		}

		public override bool Equals(GameObject other)
		{
			if (other is Edge edge)
			{
				return A.Equals(edge.A) && B.Equals(edge.B);
			}
			return false;
		}
	}
}
