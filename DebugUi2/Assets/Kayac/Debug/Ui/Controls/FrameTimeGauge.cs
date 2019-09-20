namespace Kayac.Debug.Ui
{
    public class FrameTimeGauge : DualGauge
    {
        readonly FrameTimeWatcher watcher;
        readonly bool watcherIsMine;

        public FrameTimeGauge(
            float width,
            float height,
            FrameTimeWatcher watcher) : base(0f, 66.667f, width, height, asInteger: true)
        {
            if (watcher == null)
            {
                this.watcher = new FrameTimeWatcher();
                watcherIsMine = true;
            }
            else
            {
                this.watcher = watcher;
            }
        }

        protected override void OnEnable()
        {
            if (watcherIsMine)
            {
                watcher.Reset();
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (watcherIsMine)
            {
                watcher.Update();
            }
            PrimaryValue = watcher.AverageFrameTime * 0.001f;
            SecondaryValue = watcher.MaxFrameTime * 0.001f;
        }
    }
}