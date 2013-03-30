#region Using

using System;
using System.Collections.Generic;
using System.Threading;
using Libra.Graphics;

#endregion

namespace Libra.Games
{
    public abstract class Game : IDisposable
    {
        #region UpdateCountHistory

        sealed class UpdateCountHistory
        {
            const int badUpdateCount = 2;

            int[] counts;

            int nextIndex;

            float threshold;

            public bool IsRunningSlowly { get; private set; }

            public UpdateCountHistory()
            {
                counts = new int[4];
                nextIndex = 0;
                var maxLastCount = 2 * Math.Min(badUpdateCount, counts.Length);
                threshold = (float) (maxLastCount + (counts.Length - maxLastCount)) / counts.Length;
            }

            public void Update(int count)
            {
                counts[nextIndex] = count;

                float mean = 0;
                for (int i = 0; i < counts.Length; i++)
                {
                    mean += counts[i];
                }

                mean /= counts.Length;
                nextIndex = (nextIndex + 1) % counts.Length;

                IsRunningSlowly = (threshold < mean);
            }

            public void Clear()
            {
                Array.Clear(counts, 0, counts.Length);
                nextIndex = 0;
            }
        }

        #endregion

        public event EventHandler Activated;
        
        public event EventHandler Deactivated;
        
        public event EventHandler Disposed;
        
        public event EventHandler Exiting;

        const long defaultTargetElapsedTicks = 10000000L / 60L;

        bool isFixedTimeStep;

        bool isRunning;

        bool isExiting;

        bool updatedOnce;

        bool suppressDraw;

        bool isMouseVisible;

        List<IDrawable> drawables;
        
        List<IUpdateable> updateables;

        GameTime gameTime;

        IGameTimer timer;

        UpdateCountHistory updateCountHistory;

        bool shouldResetElapsedTime;

        TimeSpan maximumElapsedTime;

        TimeSpan updateElapsedGameTime;

        TimeSpan drawElapsedGameTime;

        TimeSpan targetElapsedTime;

        TimeSpan inactiveSleepTime;

        IGamePlatform gamePlatform;

        IGraphicsManager graphicsManager;

        public GameComponentCollection Components { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsFixedTimeStep
        {
            get { return isFixedTimeStep; }
            set
            {
                if (isFixedTimeStep == value) return;

                isFixedTimeStep = value;

                updateCountHistory.Clear();
            }
        }

        // TODO
        //
        // フォーム アプリケーションでは、Cursor.Hide/Show で自由に切り替えられる。
        // しかし、どちらの状態であるかを知ることができない。
        // ゲーム内から表示を切り替えるにはプラットフォームを経由させれば良いだけだが、
        // 表示状態はどこからも参照できない。

        public bool IsMouseVisible
        {
            get { return isMouseVisible; }
            set
            {
                if (isMouseVisible == value)
                    return;

                isMouseVisible = value;
            }
        }

        public GameServiceContainer Services { get; private set; }

        public GameWindow Window { get; private set; }

        public TimeSpan TargetElapsedTime
        {
            get { return targetElapsedTime; }
            set { targetElapsedTime = value; }
        }

        public TimeSpan InactiveSleepTime
        {
            get { return inactiveSleepTime; }
            set { inactiveSleepTime = value; }
        }

        public IDevice Device { get; private set; }

        public SwapChain SwapChain { get; private set; }

        public Game()
        {
            drawables = new List<IDrawable>();
            updateables = new List<IUpdateable>();

            Components = new GameComponentCollection();
            Components.ComponentAdded += OnComponentAdded;
            Components.ComponentRemoved += OnComponentRemoved;

            Services = new GameServiceContainer();

            gameTime = new GameTime();
            updateCountHistory = new UpdateCountHistory();
            isFixedTimeStep = true;

            // 500ms が経過時間の上限 (XNA と同等)
            maximumElapsedTime = TimeSpan.FromMilliseconds(500);
            // 固定更新は 16.6ms 基準 (XNA と同等)
            targetElapsedTime = TimeSpan.FromTicks(10000000L / 60L);

            IsActive = true;
        }

        public void SuppressDraw()
        {
            suppressDraw = true;
        }

        public void ResetElapsedTime()
        {
            shouldResetElapsedTime = true;
        }

        public void Exit()
        {
            // プラットフォームへの終了命令を送信するのみ。
            // Existing イベントは、プラットフォームからの Existing イベント発生に呼応させる。

            gamePlatform.Exit();
        }

        public void Run()
        {
            if (isRunning)
                throw new InvalidOperationException("Game is already running.");
            
            isRunning = true;

            BeginRun();

            Initialize();

            IsActive = true;

            timer.Reset();
            gamePlatform.Run(Tick);

            EndRun();
            isRunning = false;
        }

        public void Tick()
        {
            // XNA の Update/Draw
            // http://blogs.msdn.com/b/ito/archive/2007/03/08/2-update.aspx

            // SharpDX.Toolkit の実装が XNA の設計と同一と思われるため、これを基本として実装。
            // ただし、一部、XNA と振る舞いが異なると思われる点を修正。

            if (isExiting)
                return;

            if (!IsActive)
                Thread.Sleep(inactiveSleepTime);

            timer.Tick();

            var elapsedTime = timer.ElapsedTime;

            if (shouldResetElapsedTime)
            {
                shouldResetElapsedTime = false;

                elapsedTime = TimeSpan.Zero;

                updateCountHistory.Clear();
                gameTime.IsRunningSlowly = false;
            }

            if (maximumElapsedTime < elapsedTime)
                elapsedTime = maximumElapsedTime;

            int updateCount = 1;

            if (isFixedTimeStep)
            {
                if (Math.Abs(elapsedTime.Ticks - TargetElapsedTime.Ticks) < (TargetElapsedTime.Ticks >> 6))
                    elapsedTime = TargetElapsedTime;

                updateElapsedGameTime += elapsedTime;

                updateCount = (int) (updateElapsedGameTime.Ticks / TargetElapsedTime.Ticks);

                if (updateCount == 0)
                    return;

                updateCountHistory.Update(updateCount);

                // 固定更新では経過時間固定。
                gameTime.ElapsedGameTime = TargetElapsedTime;
                gameTime.IsRunningSlowly = updateCountHistory.IsRunningSlowly;

                // 余剰分の経過時間を次の更新判定のために残す。
                updateElapsedGameTime = new TimeSpan(updateElapsedGameTime.Ticks - (updateCount * TargetElapsedTime.Ticks));
            }
            else
            {
                // 可変更新では実際の経過時間。
                gameTime.ElapsedGameTime = elapsedTime;
                gameTime.IsRunningSlowly = false;
            }

            bool shouldDraw = true;
            for (int i = 0; i < updateCount; i++)
            {
                if (isExiting)
                    return;

                try
                {
                    // Update の GameTime は、前回 Update が呼び出されてからの経過時間 (XNA)。

                    gameTime.TotalGameTime += gameTime.ElapsedGameTime;

                    Update(gameTime);

                    updatedOnce = true;

                    shouldDraw &= !suppressDraw;
                    suppressDraw = false;
                }
                finally
                {
                    drawElapsedGameTime += gameTime.ElapsedGameTime;
                }
            }

            if (shouldDraw && !isExiting && updatedOnce)
            {
                try
                {
                    if (BeginDraw())
                    {
                        // Draw の GameTime は、前回 Draw が呼び出されてからの経過時間 (XNA)。

                        gameTime.ElapsedGameTime = drawElapsedGameTime;

                        Draw(gameTime);

                        EndDraw();
                    }
                }
                finally
                {
                    drawElapsedGameTime = TimeSpan.Zero;
                }
            }
        }

        protected virtual bool ShowMissingRequirementMessage(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected virtual void Initialize()
        {
            // ゲーム プラットフォームの初期化。
            gamePlatform = Services.GetRequiredService<IGamePlatform>();
            gamePlatform.Activated += OnActivated;
            gamePlatform.Deactivated += OnDeactivated;
            gamePlatform.Exiting += OnExiting;
            gamePlatform.Initialize();
            
            // ウィンドウの取得。
            Window = gamePlatform.Window;
            Window.ClientSizeChanged += OnWindowClientSizeChanged;

            // タイマーの取得。
            timer = gamePlatform.GameTimer;

            // デバイスの初期化。
            graphicsManager = Services.GetRequiredService<IGraphicsManager>();
            graphicsManager.Initialize();

            var graphicsService = Services.GetRequiredService<IGraphicsService>();
            Device = graphicsService.Device;
            SwapChain = graphicsService.SwapChain;

            // コンテンツのロード。
            LoadContent();

            // ゲーム コンポーネントの初期化。
            int count = Components.Count;
            for (int i = 0; i < count; i++)
                Components[i].Initialize();
        }

        protected virtual void LoadContent() { }

        protected virtual void UnloadContent() { }

        protected virtual void BeginRun() { }

        protected virtual void EndRun() { }

        protected virtual void Update(GameTime gameTime)
        {
            for (int i = 0; i < updateables.Count; i++)
                updateables[i].Update(gameTime);
        }

        protected virtual bool BeginDraw()
        {
            return graphicsManager.BeginDraw();
        }

        protected virtual void EndDraw()
        {
            graphicsManager.EndDraw();
        }

        protected virtual void Draw(GameTime gameTime)
        {
            for (int i = 0; i < drawables.Count; i++)
                drawables[i].Draw(gameTime);
        }

        protected virtual void OnActivated(object sender, EventArgs e)
        {
            IsActive = true;

            if (Activated != null)
                Activated(sender, e);
        }

        protected virtual void OnDeactivated(object sender, EventArgs e)
        {
            IsActive = false;

            if (Deactivated != null)
                Deactivated(sender, e);
        }

        protected virtual void OnExiting(object sender, EventArgs e)
        {
            isExiting = true;

            if (Exiting != null)
                Exiting(sender, e);
        }

        void OnWindowClientSizeChanged(object sender, EventArgs e)
        {
            // クライアント領域サイズでスワップ チェーンのバッファをリサイズ。
            var clientWidth = Window.ClientBounds.Width;
            var clientHeight = Window.ClientBounds.Height;
            SwapChain.ResizeBuffers(clientWidth, clientHeight);
        }

        void OnComponentAdded(object sender, GameComponentCollectionEventArgs e)
        {
            var component = e.GameComponent;

            if (isRunning)
            {
                component.Initialize();
            }

            var drawable = component as IDrawable;
            if (drawable != null)
            {
                drawable.DrawOrderChanged += OnDrawableDrawOrderChanged;
                drawable.VisibleChanged += OnDrawableVisibleChanged;

                if (drawable.Visible)
                    AddDrawable(drawable);
            }

            var updateable = component as IUpdateable;
            if (updateable != null)
            {
                updateable.UpdateOrderChanged += OnUpdatableUpdateOrderChanged;
                updateable.EnabledChanged += OnUpdatableEnabledChanged;

                if (updateable.Enabled)
                    AddUpdatable(updateable);
            }
        }

        void OnComponentRemoved(object sender, GameComponentCollectionEventArgs e)
        {
            var component = e.GameComponent;

            var drawable = component as IDrawable;
            if (drawable != null)
            {
                drawable.DrawOrderChanged -= OnDrawableDrawOrderChanged;
                drawable.VisibleChanged -= OnDrawableVisibleChanged;

                if (drawable.Visible)
                    drawables.Remove(drawable);
            }

            var updateable = component as IUpdateable;
            if (updateable != null)
            {
                updateable.UpdateOrderChanged -= OnUpdatableUpdateOrderChanged;
                updateable.EnabledChanged -= OnUpdatableEnabledChanged;

                if (updateable.Enabled)
                    updateables.Remove(updateable);
            }
        }

        void AddUpdatable(IUpdateable updateable)
        {
            updateables.Add(updateable);
            updateables.Sort(UpdatableComparison);
        }

        void OnUpdatableEnabledChanged(object sender, EventArgs e)
        {
            var updateable = sender as IUpdateable;
            if (updateable.Enabled)
            {
                AddUpdatable(updateable);
            }
            else
            {
                updateables.Remove(updateable);
            }
        }

        void OnUpdatableUpdateOrderChanged(object sender, EventArgs e)
        {
            updateables.Sort(UpdatableComparison);
        }

        static int UpdatableComparison(IUpdateable x, IUpdateable y)
        {
            return x.UpdateOrder.CompareTo(y.UpdateOrder);
        }

        void AddDrawable(IDrawable drawable)
        {
            drawables.Add(drawable);
            drawables.Sort(DrawableComparison);
        }

        void OnDrawableVisibleChanged(object sender, EventArgs e)
        {
            var drawable = sender as IDrawable;
            if (drawable.Visible)
                AddDrawable(drawable);
            else
                drawables.Remove(drawable);
        }

        void OnDrawableDrawOrderChanged(object sender, EventArgs e)
        {
            drawables.Sort(DrawableComparison);
        }

        static int DrawableComparison(IDrawable x, IDrawable y)
        {
            return x.DrawOrder - y.DrawOrder;
        }

        #region IDisposable

        bool disposed;

        ~Game()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                IDisposable disposable;

                for (int i = 0; i < Components.Count; i++)
                {
                    disposable = Components[i] as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }

                disposable = gamePlatform as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            disposed = true;

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);
        }

        #endregion
    }
}

