namespace Kayac
{
	// enumや定数を持つだけの基底クラス
	public abstract class DebugUi
	{
		public enum AlignX
		{
			Left,
			Center,
			Right,
		}

		public enum AlignY
		{
			Top,
			Center,
			Bottom,
		}

		public enum Direction
		{
			Left,
			Right,
			Up,
			Down,
			Unknown,
		}
	}
}