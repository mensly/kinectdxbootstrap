extern alias oldKinect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Toolkit.Graphics;

using NewKinect = Microsoft.Kinect;
using oldKinect.Microsoft.Kinect;
using OldKinect = oldKinect.Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

namespace KinectDXBootstrap.Legacy
{
    class KinectState : IKinectState
    {
        public static bool UseLegacy
        {
            get { return KinectSensor.KinectSensors.Count() > 0; } 
        }

        private const byte NO_ACTIVE_PLAYER = KinectDXBootstrap.KinectState.NO_ACTIVE_PLAYER;
        private const int INDEX_MASK = (1 << 3) - 1; // 0000111 = 7

        public IList<IBody> Players { get { return players; } }
        public byte ActivePlayerIndex { get; private set; }
        public IBody ActivePlayer { get; private set; }
        public bool HadPlayer { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsLegacy { get { return true; } }

        public ICoordinateMapper CoordinateMapper { get { return coordinateMapper; } }
        private Legacy.CoordinateMapper coordinateMapper;

        public event Action<byte[]> OnProcessColor;
        public Texture2DBase ColorImage { get; private set; }
        private readonly int colorImageBPP = 4;
        public Point ColorImageSize { get; private set; }

        public Texture2DBase DepthImage { get { return depthImage; } }
        private RenderTarget2D depthImage;
        public DepthImageMode DepthImageMode { get; set; }
        public Texture2DBase JointImage { get; set; }
        public IList<ColorBGRA> UserColors { get; set; }
        public Point DepthImageSize { get; private set; }
        private ColorBGRA[] depthRender;
        private int depthRange;
        private int minDepth;
        private SpriteBatch spriteBatch;

        private bool colorUpdated;
        private bool depthUpdated;
        private bool skeletonUpdated;
        private bool colorNeedRender;
        private bool depthNeedRender;
        private bool skeletonsNeedRender;

        private byte[] colorData;
        private short[] depthData;
        private Skeleton[] skeletons;
        private Body[] players = new Body[0];

        public NewKinect.Face.FaceFrameResult ActivePlayerFace { get { return null; } }

        public KinectState()
        {
            UserColors = KinectDXBootstrap.KinectState.DEFAULT_USER_COLORS;
        }

        public ushort GetDepth(int x, int y)
        {
            return (ushort)(depthData[y * DepthImageSize.X + x] >> 3);
        }
        public byte GetPlayerIndex(int x, int y)
        {
            int playerIndex = (depthData[y * DepthImageSize.X + x] & INDEX_MASK);
            return playerIndex == 0 ? NO_ACTIVE_PLAYER : (byte)(playerIndex - 1);
        }
        public IEnumerable<DepthDataPixel> DepthData
        {
            get
            {
                if (depthData == null)
                {
                    yield break;
                }
                DepthDataPixel data = new DepthDataPixel();
                data.MinDepth = (ushort)minDepth;
                data.MaxDepth = (ushort)(minDepth + depthRange);
                int playerIndex;
                for (data.Index = 0, data.Y = 0; data.Index < depthData.Length; data.Y++)
                {
                    for (data.X = 0; data.X < DepthImageSize.X; data.X++, data.Index++)
                    {
                        data.Depth = (ushort)(depthData[data.Index] >> 3);
                        playerIndex = depthData[data.Index] & INDEX_MASK;
                        data.PlayerIndex = playerIndex == 0 ? NO_ACTIVE_PLAYER : (byte)(playerIndex - 1);
                        yield return data;
                    }
                }
            }
        }
        public NewKinect.FrameSourceTypes Render(GraphicsDevice graphics)
        {
            NewKinect.FrameSourceTypes modified = NewKinect.FrameSourceTypes.None;
            if (colorNeedRender)
            {
                if (ColorImage == null)
                {
                    ColorImage = Texture2D.New(graphics, ColorImageSize.X, ColorImageSize.Y, PixelFormat.B8G8R8X8.UNorm,
                        TextureFlags.ShaderResource, 1, SharpDX.Direct3D11.ResourceUsage.Dynamic);
                }
                if (OnProcessColor != null)
                {

                }
                ColorImage.SetData(colorData);
                colorNeedRender = false;
                modified |= NewKinect.FrameSourceTypes.Color;
            }
            if (depthNeedRender)
            {
                if (DepthImage == null)
                {
                    depthImage = RenderTarget2D.New(graphics, DepthImageSize.X, DepthImageSize.Y, MipMapCount.Auto, PixelFormat.B8G8R8X8.UNorm);
                    depthRender = new ColorBGRA[depthData.Length];
                }
                byte intensity = 0;
                int depth;
                for (int i = 0; i < depthRender.Length; i++)
                {
                    depth = depthData[i] >> 3;
                    if (minDepth <= depth)
                    {
                        int distance = Math.Min(depth - minDepth, depthRange);
                        if (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.Wrapped))
                        {
                            intensity = (byte)(distance % byte.MaxValue);
                        }
                        else
                        {
                            intensity = (byte)(byte.MaxValue * distance / depthRange);
                        }
                    }
                    else
                    {
                        intensity = 0;
                    }
                    depthRender[i] = new ColorBGRA(intensity);
                }
                if (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.UserColor))
                {
                    int playerIndex;
                    for (int i = 0; i < depthRender.Length; i++)
                    {
                        playerIndex = depthData[i] & INDEX_MASK;
                        if (ActivePlayerIndex != NO_ACTIVE_PLAYER)
                        {
                            // Ensure the active player always has the first user color
                            if (playerIndex == ActivePlayerIndex + 1)
                            {
                                playerIndex = 1;
                            }
                            else if (0 < playerIndex && playerIndex < ActivePlayerIndex)
                            {
                                playerIndex++;
                            }
                        }
                        switch (playerIndex)
                        {
                            case 0:
                                break;
                            default:
                                depthRender[i] *= UserColors[playerIndex - 1];
                                break;
                        }
                    }
                    modified |= NewKinect.FrameSourceTypes.BodyIndex;
                }
                DepthImage.SetData(depthRender);
                modified |= NewKinect.FrameSourceTypes.Depth;
                if (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.IncludeJoints) && ActivePlayer != null)
                {
                    // Map to appropriate space
                    SharpDX.Direct3D11.DepthStencilView depthView;
                    SharpDX.Direct3D11.RenderTargetView[] renderTargets = graphics.GetRenderTargets(out depthView);
                    graphics.SetRenderTargets(depthImage);
                    NewKinect.CameraSpacePoint[] cameraPoints = ActivePlayer.Joints.Select(joint => joint.Value.Position).ToArray();
                    NewKinect.DepthSpacePoint[] depthPoints = new NewKinect.DepthSpacePoint[cameraPoints.Length];
                    CoordinateMapper.MapCameraPointsToDepthSpace(cameraPoints, depthPoints);
                    // Ensure required assets exist
                    if (spriteBatch == null)
                    {
                        spriteBatch = new SpriteBatch(graphics);
                    }
                    if (JointImage == null)
                    {
                        const int size = 4;
                        JointImage = Texture2D.New(graphics, size, size, PixelFormat.B8G8R8X8.UNorm);
                        JointImage.SetData(Enumerable.Repeat(Color.White, size * size).ToArray());
                    }
                    spriteBatch.Begin();
                    // Draw points 
                    Vector2 v;
                    foreach (NewKinect.DepthSpacePoint point in depthPoints)
                    {
                        v.X = point.X - JointImage.Width / 2;
                        v.Y = point.Y - JointImage.Height / 2;
                        spriteBatch.Draw(JointImage, v, Color.White);
                    }
                    spriteBatch.End();
                    graphics.SetRenderTargets(renderTargets);
                    modified |= NewKinect.FrameSourceTypes.Body;
                }
            }
            return modified;
        }
        #region Update
        public bool Update(object frameReader)
        {
            if (frameReader is KinectSensorChooser)
            {
                frameReader = ((KinectSensorChooser)frameReader).Kinect;
            }
            var reader = frameReader as KinectSensor;
            if (reader != null)
            {
                return Update(reader);
            }
            return false;
        }
        public bool Update(KinectSensor sensor)
        {
            if (CoordinateMapper == null)
            {
                coordinateMapper = new Legacy.CoordinateMapper(sensor.CoordinateMapper);
            }
            bool availabilityUpdated = IsAvailable != sensor.IsRunning;
            if (availabilityUpdated)
            {
                IsAvailable = sensor.IsRunning;
            }
            IsAvailable = sensor.IsRunning;
            // Update each source
            colorUpdated |= Update(sensor.ColorStream.OpenNextFrame(0));
            depthUpdated |= Update(sensor.DepthStream.OpenNextFrame(0));
            skeletonUpdated |= Update(sensor.SkeletonStream.OpenNextFrame(0));
            if (colorUpdated && depthUpdated && skeletonUpdated)
            {
                // Report new frame when everything is updated
                colorUpdated = false;
                depthUpdated = false;
                skeletonUpdated = false;
                return true;
            }
            else
            {
                return availabilityUpdated;
            }
        }

        public bool Update(ColorImageFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (colorData == null)
                    {
                        colorData = new byte[frame.PixelDataLength];
                        ColorImageSize = new Point(frame.Width, frame.Height);
                    }
                    frame.CopyPixelDataTo(colorData);
                    colorNeedRender = true;
                    return true;
                }
            }
            return false;
        }

        public bool Update(DepthImageFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (depthData == null)
                    {
                        depthData = new short[frame.PixelDataLength];
                        DepthImageSize = new Point(frame.Width, frame.Height);
                    }
                    frame.CopyPixelDataTo(depthData);
                    minDepth = frame.MinDepth;
                    depthRange = frame.MaxDepth - minDepth;
                    depthNeedRender = true;
                    return true;
                }
            }
            return false;
        }

        public bool Update(SkeletonFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (skeletons == null)
                    {
                        skeletons = new Skeleton[frame.SkeletonArrayLength];
                        players = new Body[skeletons.Length];
                    for (int i = 0; i < skeletons.Length; i++)
                    {
                        players[i] = new Body();
                    }
                    }
                    frame.CopySkeletonDataTo(skeletons);
                    for (int i = 0; i < skeletons.Length; i++)
                    {
                        players[i].Update(skeletons[i]);
                    }

                    // Choose a player that is tracked, preferring one that is centered and engaged
                    var active = skeletons.Select((b, i) => new KeyValuePair<byte, Skeleton>((byte)i, b))
                        .Where(kv => kv.Value.TrackingState == SkeletonTrackingState.Tracked)
                        .OrderBy(kv => 
                            Math.Abs(kv.Value.Position.X)
                            + Math.Abs(2 - kv.Value.Position.Z));
                    HadPlayer = ActivePlayer != null;
                    if (active.Count() == 0)
                    {
                        ActivePlayerIndex = NO_ACTIVE_PLAYER;
                        ActivePlayer = null;
                    }
                    else
                    {
                        ActivePlayerIndex = active.First().Key;
                        ActivePlayer = players[ActivePlayerIndex];
                    }
                    skeletonsNeedRender = true;
                    return true;
                }
            }
            return false;
        }

        #endregion

    }
}
