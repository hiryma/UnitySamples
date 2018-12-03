namespace Kayac
{

	public class DebugUiToggleGroup
	{
		public delegate void OnSelect(DebugUiToggle newSelected, DebugUiToggle oldSelected);
		public OnSelect onSelect{ private get; set; }
		private DebugUiToggle _selected;

		public DebugUiToggleGroup()
		{
			_selected = null;
		}

		public DebugUiToggle selected
		{
			get
			{
				return _selected;
			}
		}

		public void SetOnToggle(DebugUiToggle selected)
		{
			// コールバック
			if (onSelect != null)
			{
				onSelect(selected, _selected);
			}
			// 今onのものをoffにする
			if (_selected != null)
			{
				_selected.SetOffFromGroup();
			}
			_selected = selected;
		}
	}
}