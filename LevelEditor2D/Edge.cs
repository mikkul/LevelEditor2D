using System.ComponentModel;

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
		}

		[Browsable(false)]
		public Vertex A { get; set; }
		[Browsable(false)]
		public Vertex B { get; set; }

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
