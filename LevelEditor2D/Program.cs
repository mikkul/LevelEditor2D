using System;

namespace LevelEditor2D
{
	public static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			using (var game = new Main(args))
			{
				game.Run();
			}
		}
	}
}
