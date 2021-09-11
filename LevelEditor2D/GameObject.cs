using System;
using System.ComponentModel;

namespace LevelEditor2D
{
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
