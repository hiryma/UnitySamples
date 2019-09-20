namespace Kayac.Debug.Ui
{
    public class ToggleGroup
    {
        public delegate void OnSelectFunc(Toggle newSelected, Toggle oldSelected);
        public OnSelectFunc OnSelect { private get; set; }

        public ToggleGroup()
        {
            Selected = null;
        }

        public Toggle Selected { get; private set; }

        public void SetOnToggle(Toggle selected)
        {
            // コールバック
            if (OnSelect != null)
            {
                OnSelect(selected, Selected);
            }
            // 今onのものをoffにする
            if (Selected != null)
            {
                Selected.SetOffFromGroup();
            }
            Selected = selected;
        }
    }
}