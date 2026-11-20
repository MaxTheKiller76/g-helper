using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    internal sealed class FluentTransition(Control owner, float initialValue = 0f, Action? onFrame = null)
    {
        public const int Quick = 83;
        public const int Standard = 167;
        public const int Entrance = 250;

        private const uint GetClientAreaAnimation = 0x1042;
        [ThreadStatic] private static FrameClock? clock;

        private float startValue = initialValue;
        private float target = initialValue;
        private long started;
        private int duration;

        public float Value { get; private set; } = initialValue;

        private bool CanDisplay => owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing &&
            owner.Visible && owner.Enabled;

        private static bool AnimationsEnabled => !SystemInformation.HighContrast &&
            SystemParametersInfoW(GetClientAreaAnimation, 0, out int enabled, 0) && enabled != 0;

        public void AnimateTo(float value, int milliseconds = Standard)
        {
            if (target != value) PlayFrom(Value, value, milliseconds);
        }

        public void PlayFrom(float from, float to, int milliseconds)
        {
            if (from == to || !CanDisplay || !AnimationsEnabled)
            {
                SnapTo(to);
                return;
            }

            startValue = from;
            target = to;
            duration = milliseconds;
            started = Stopwatch.GetTimestamp();
            (clock ??= new FrameClock()).Schedule(this);
            SetValue(from);
        }

        public void SnapTo(float value)
        {
            target = value;
            clock?.Remove(this);
            SetValue(value);
        }

        private void Advance(long now, bool animate)
        {
            float progress = (float)(Stopwatch.GetElapsedTime(started, now).TotalMilliseconds / duration);
            if (!animate || !CanDisplay || progress >= 1f)
            {
                SnapTo(target);
                return;
            }

            float remaining = 1f - Math.Max(0f, progress);
            float eased = 1f - remaining * remaining * remaining;
            SetValue(startValue + (target - startValue) * eased);
        }

        private void SetValue(float value)
        {
            if (Value == value) return;
            Value = value;
            if (owner.IsDisposed || owner.Disposing) return;
            onFrame?.Invoke();
            owner.Invalidate();
        }

        private sealed class FrameClock
        {
            private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
            private readonly HashSet<FluentTransition> pending = [];
            private readonly List<FluentTransition> frame = [];
            private bool ticking;

            public FrameClock() => timer.Tick += Tick;

            public void Schedule(FluentTransition transition)
            {
                if (pending.Add(transition) && pending.Count == 1) timer.Start();
            }

            public void Remove(FluentTransition transition)
            {
                if (pending.Remove(transition) && pending.Count == 0) timer.Stop();
            }

            private void Tick(object? sender, EventArgs e)
            {
                if (ticking) return;

                ticking = true;
                try
                {
                    bool animate = AnimationsEnabled;
                    long now = Stopwatch.GetTimestamp();
                    frame.AddRange(pending);
                    foreach (FluentTransition transition in frame)
                        if (pending.Contains(transition)) transition.Advance(now, animate);
                }
                finally
                {
                    frame.Clear();
                    ticking = false;
                }
            }
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoW(uint action, uint parameter, out int value, uint flags);
    }
}
