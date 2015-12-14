using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using Microsoft.Kinect.Face;
#if INCLUDE_LEGACY
using Microsoft.Kinect.Toolkit;
#endif

using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Audio;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit.Input;

namespace KinectDXBootstrap
{
    public class Activation : Game
    {
        public const FrameSourceTypes DEFAULT_KINECT_SOURCES = FrameSourceTypes.Body | 
            FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth;

        public GraphicsDeviceManager Graphics { get; private set; }
        protected Color4 BackgroundColor { get; set; }
        protected SpriteBatch SpriteBatch { get; private set; }
        protected string Title { get; set; }

        private KeyboardManager keyboard;
        private List<Keys> keys = new List<Keys>();
        private List<Keys> previousKeys = new List<Keys>();
        protected KeyboardState KeyboardState { get; private set; }

        private object sensor;
        protected object FrameReader { get; private set; }
        private FaceFrameSource faceSource;
        private FaceFrameReader faceReader;
        private FrameSourceTypes kinectSources;
        private FaceFrameFeatures faceFeatures;
        private IKinectState kinectState;
        public Texture2DBase ColorImage { get { return kinectState.ColorImage; } }
        public Texture2DBase DepthImage { get { return kinectState.DepthImage; } }
        public FrameSourceTypes RenderedSources { get; private set; }
        public Point ColorImageSize { get { return kinectState.ColorImageSize; } }
        public Point DepthImageSize { get { return kinectState.DepthImageSize; } }
        public Texture2D Pixel { get; private set; }
        
        protected bool RenderDirect { get; private set; }
        private RenderTarget2D renderTarget;
        public Rectangle ScreenSize { get; private set; }
        private Rectangle drawScreenSize;
#if DEBUG
        private float fps = 60;
#endif
        
        public Activation(FrameSourceTypes kinectSources = DEFAULT_KINECT_SOURCES,
            int renderWidth = 1920, int renderHeight = 1080, bool forceRenderBuffer = false)
            : this(kinectSources, FaceFrameFeatures.None, renderWidth, renderHeight, forceRenderBuffer)
        {
        }

        public Activation(FrameSourceTypes kinectSources = DEFAULT_KINECT_SOURCES, FaceFrameFeatures faceFeatures = FaceFrameFeatures.None,
            int renderWidth = 1920, int renderHeight = 1080, bool forceRenderBuffer = false)
        {
            this.kinectSources = kinectSources;
            this.faceFeatures = faceFeatures;
            Graphics = new GraphicsDeviceManager(this);
            var displayMode = GraphicsAdapter.Default.GetOutputAt(0).CurrentDisplayMode;
#if !DEBUG
            Graphics.IsFullScreen = true;
#endif
            if (displayMode == null)
            {
                Graphics.PreferredBackBufferWidth = renderWidth;
                Graphics.PreferredBackBufferHeight = renderHeight;
                drawScreenSize = new Rectangle(0, 0, renderWidth, renderHeight);
                RenderDirect = !forceRenderBuffer;
            }
            else if (!forceRenderBuffer && ((Graphics.IsFullScreen && displayMode.Width == renderWidth && displayMode.Height == renderHeight) ||
                (!Graphics.IsFullScreen && displayMode.Width >= renderWidth && displayMode.Height >= renderHeight)))
            {
                Graphics.PreferredBackBufferWidth = renderWidth;
                Graphics.PreferredBackBufferHeight = renderHeight;
                RenderDirect = true;
            }
            else
            {
                float scaleRatio = Math.Min(1, Math.Min((float)displayMode.Width / renderWidth, (float)displayMode.Height / renderHeight));
                drawScreenSize = new Rectangle(0, 0, (int)(renderWidth * scaleRatio), (int)(renderHeight * scaleRatio));
                Graphics.PreferredBackBufferWidth = drawScreenSize.Width;
                Graphics.PreferredBackBufferHeight = drawScreenSize.Height;
                if (drawScreenSize.Width == displayMode.Width && drawScreenSize.Height == displayMode.Height)
                {
                    Graphics.IsFullScreen = true;
                }
                RenderDirect = false;
            }
            ScreenSize = new Rectangle(0, 0, renderWidth, renderHeight);
            Content.RootDirectory = "Content";
            BackgroundColor = Color.CornflowerBlue;
            Title = "Kinect Bootstrap Activation";
        }

        protected override void OnWindowCreated()
        {
            base.OnWindowCreated();
            Window.Title = Title;
            Window.AllowUserResizing = false;
        }

        private void InitializeKinect()
        {
#if INCLUDE_LEGACY
            if (Legacy.KinectState.UseLegacy)
            {
                KinectSensorChooser sensor = new KinectSensorChooser();
                sensor.KinectChanged += (sender, args) =>
                {
                    if (args.NewSensor != null)
                    {
                        args.NewSensor.ColorStream.Enable();
                        args.NewSensor.DepthStream.Enable();
                        args.NewSensor.SkeletonStream.Enable();
                    }
                };
                sensor.Start();
                if (sensor.Kinect != null)
                {
                    sensor.Kinect.ColorStream.Enable();
                    sensor.Kinect.DepthStream.Enable();
                    sensor.Kinect.SkeletonStream.Enable();
                }
                kinectState = new Legacy.KinectState();
                FrameReader = sensor;
                this.sensor = sensor;
            }
            else
            {
#endif
                KinectSensor sensor = KinectSensor.GetDefault();
                if (faceFeatures != FaceFrameFeatures.None)
                {
                    faceSource = new FaceFrameSource(sensor, 0, faceFeatures);
                    faceReader = faceSource.OpenReader();
                }
                FrameReader = sensor.OpenMultiSourceFrameReader(kinectSources);
                kinectState = new KinectState(sensor);
                sensor.Open();
                this.sensor = sensor;
#if INCLUDE_LEGACY
            }
#endif
            OnKinectInitialized(kinectState, this.sensor);
        }

        protected virtual void OnKinectInitialized(IKinectState state, object sensor)
        {

        }

        protected override void Initialize()
        {
            base.Initialize();
            keyboard = new KeyboardManager(this);
            InitializeKinect();
            new AudioManager(this);
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);
            IDisposable sensor = this.sensor as IDisposable;
            if (sensor is IDisposable)
            {
                sensor.Dispose();
            }
            if (faceReader != null)
            {
                faceReader.Dispose();
            }
            if (faceSource != null)
            {
                faceSource.Dispose();
            }
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Pixel = Texture2D.New(GraphicsDevice, 1, 1, PixelFormat.B8G8R8X8.UNorm);
            Pixel.SetData(new Color[] { Color.White });
            if (!RenderDirect)
            {
                renderTarget = RenderTarget2D.New(GraphicsDevice, ScreenSize.Width, ScreenSize.Height, PixelFormat.B8G8R8X8.UNorm);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            UpdateKeyboard(gameTime);
            if (kinectState.Update(FrameReader) || kinectState.Update(faceReader))
            {
                ulong trackingId = kinectState.ActivePlayer == null ? 0 : kinectState.ActivePlayer.TrackingId;
                if (faceSource != null && trackingId != faceSource.TrackingId)
                {
                    faceSource.TrackingId = trackingId;
                }
                OnKinectUpdated(gameTime, kinectState);
            }
            base.Update(gameTime);
        }

        private void UpdateKeyboard(GameTime gameTime)
        {
            KeyboardState = keyboard.GetState();
            var tmpKeys = previousKeys;
            previousKeys = keys;
            keys = tmpKeys;
            KeyboardState.GetDownKeys(keys);
            foreach (Keys k in keys.Where(k => !previousKeys.Contains(k)))
            {
                OnKeyDown(k);
            }
            foreach (Keys k in previousKeys.Where(k => !keys.Contains(k)))
            {
                OnKeyUp(k);
            }
        }

        protected virtual bool OnKinectUpdated(GameTime gameTime, IKinectState kinectState)
        {
            return false;
        }

        protected virtual bool OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                Exit();
                return true;
            }
            return false;
        }

        protected virtual bool OnKeyUp(Keys key)
        {
            return false;
        }

        protected sealed override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            // Rendering to a RenderTarget to support scaling
            RenderedSources = kinectState.Render(GraphicsDevice);
            SharpDX.Direct3D11.DepthStencilView depthView;
            SharpDX.Direct3D11.RenderTargetView[] renderTargets = RenderDirect ? null : 
                GraphicsDevice.GetRenderTargets(out depthView);
            if (!RenderDirect)
            {
                GraphicsDevice.SetRenderTargets(this.renderTarget);
            }
            GraphicsDevice.Clear(BackgroundColor);
            DrawScene(gameTime);
            if (!RenderDirect)
            {
                GraphicsDevice.SetRenderTargets(renderTargets);
                GraphicsDevice.Clear(BackgroundColor);
                SpriteBatch.Begin();
                SpriteBatch.Draw(this.renderTarget, drawScreenSize, Color.White);
                SpriteBatch.End();
            }
#if DEBUG
            fps = MathUtil.Lerp(fps, 1 / (float)gameTime.ElapsedGameTime.TotalSeconds, 0.01f);
            Window.Title = string.Format("{0}          FPS: {1:0}", Title, fps);
#endif
        }

        protected virtual void DrawScene(GameTime gameTime)
        {

        }

        public virtual void SaveScreenshot()
        {
            const string SCREENSHOT_DIR = "Screenshots";
            if (!Directory.Exists(SCREENSHOT_DIR))
            {
                Directory.CreateDirectory(SCREENSHOT_DIR);
            }
            SaveScreenshot(Path.Combine(SCREENSHOT_DIR, DateTime.Now.ToString("yyMMddHHmmss") + ".png"));
        }

        public void SaveScreenshot(string filename)
        {
            if (RenderDirect)
            {
                throw new NotImplementedException("Screenshots not supported for direct render");
            }
            string extension = filename.Split('.').Last();
            ImageFileType type;
            if (!(extension.Length > 1 && Enum.TryParse(char.ToUpper(extension[0]) + extension.Substring(1).ToLower(), out type)))
            {
                Console.WriteLine("Unknown extension: " + extension);
                type = ImageFileType.Png;
            }
            renderTarget.Save(filename, type);
        }
    }
}
