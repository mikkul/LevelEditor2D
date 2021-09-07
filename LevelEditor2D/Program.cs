using System;

namespace LevelEditor2D
{
	public static class Program
	{
		static void Main()
		{
			using (var game = new Main())
			{
				game.Run();
			}
		}
	}
}
