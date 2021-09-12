using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	[XmlInclude(typeof(Vertex)), XmlInclude(typeof(Edge))]
	public abstract class GameObject : IEquatable<GameObject>
	{
		public GameObject()
		{
		}

		public GameObject(int id)
		{
			Id = id;
		}

		[Browsable(false)]
		public int Id { get; set; }

		public abstract bool Equals(GameObject other);
	}
}
