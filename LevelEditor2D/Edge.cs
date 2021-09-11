namespace LevelEditor2D
{
	public class Edge : GameObject
	{
		public Edge()
		{
		}

		public Edge(Vertex a, Vertex b)
		{
			A = a;
			B = b;
		}

		public Vertex A { get; set; }
		public Vertex B { get; set; }
	}
}
