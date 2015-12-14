using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using SharpDX;
using SharpDX.Toolkit.Graphics;

namespace KinectDXBootstrap
{
    public interface IKinectState
    {
        IList<IBody> Players { get; }
        byte ActivePlayerIndex { get; }
        IBody ActivePlayer { get; }
        bool HadPlayer { get; }
        bool IsAvailable { get; }
        bool IsLegacy { get; }

        ICoordinateMapper CoordinateMapper { get; }

        event Action<byte[]> OnProcessColor;
        Texture2DBase ColorImage { get; }
        Point ColorImageSize { get; }
        Texture2DBase DepthImage { get; }
        Point DepthImageSize { get; }
        DepthImageMode DepthImageMode { get; set; }
        IList<ColorBGRA> UserColors { get; set; }
        Texture2DBase JointImage { get; set; }

        ushort GetDepth(int x, int y);
        byte GetPlayerIndex(int x, int y);
        IEnumerable<DepthDataPixel> DepthData { get; }

        FaceFrameResult ActivePlayerFace { get; }

        bool Update(object frameReader);
        FrameSourceTypes Render(GraphicsDevice graphics);
    }

    public struct DepthDataPixel
    {
        public int Index;
        public int X;
        public int Y;
        public ushort Depth;
        public byte PlayerIndex;
        public ushort MinDepth;
        public ushort MaxDepth;

        public bool IsValidDepth
        {
            get
            {
                return MinDepth <= Depth && Depth <= MaxDepth;
            }
        }

        public bool HasPlayer
        {
            get
            {
                return PlayerIndex != KinectState.NO_ACTIVE_PLAYER;
            }
        }
    }

    public enum DepthImageMode
    {
        Normal = 0,
        Wrapped = 1,
        UserColor = 2,
        IncludeJoints = 4
    }

    public sealed class KinectState : IKinectState
    {
        public const byte NO_ACTIVE_PLAYER = byte.MaxValue;
        public static readonly IList<ColorBGRA> DEFAULT_USER_COLORS = new ColorBGRA[] {
            new ColorBGRA(0xFFFFFF00),
            new ColorBGRA(0xFF00FFFF),
            new ColorBGRA(0xFFFF00FF),
            new ColorBGRA(0xFF88FF88),
            new ColorBGRA(0xFF8888FF),
            new ColorBGRA(0xFFFF8888),
        };

        public event Action<byte[]> OnProcessColor;
        public IList<IBody> Players { get { return players; } }
        public byte ActivePlayerIndex { get; private set; }
        public IBody ActivePlayer { get { return activePlayer.body == null ? null : activePlayer; } }
        public bool HadPlayer { get; private set; }
        public bool IsAvailable { get; private set; }
        private bool wasAvailable;
        public bool IsLegacy { get { return false; } }

        private WrappedBody activePlayer = new WrappedBody(null);
        private WrappedBody[] players = new WrappedBody[0];

        public ICoordinateMapper CoordinateMapper { get; private set; }

        public Texture2DBase ColorImage { get; private set; }
        private readonly ColorImageFormat colorImageFormat = ColorImageFormat.Bgra;
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
        private ushort minDepth;
        private SpriteBatch spriteBatch;

        private Body[] bodies;
        private byte[] bodyIndexData;
        private byte[] colorData;
        private ushort[] depthData;
        private ushort[] infraredData;
        private ushort[] infraredExposureData;

        public FaceFrameResult ActivePlayerFace { get; private set; }

        private FrameSourceTypes modified;

        public KinectState(KinectSensor sensor)
        {
            this.CoordinateMapper = new WrappedCoordinateMapper(sensor.CoordinateMapper);
            sensor.IsAvailableChanged += (s, e) => IsAvailable = (s as KinectSensor).IsAvailable;
            UserColors = DEFAULT_USER_COLORS;
        }

        public FrameSourceTypes Render(GraphicsDevice graphics)
        {
            FrameSourceTypes modified = this.modified;
            if ((modified & FrameSourceTypes.Color) != 0)
            {
                if (ColorImage == null)
                {
                    ColorImage = Texture2D.New(graphics, ColorImageSize.X, ColorImageSize.Y, PixelFormat.B8G8R8A8.UNorm,
                        TextureFlags.ShaderResource, 1, SharpDX.Direct3D11.ResourceUsage.Dynamic);
                }
                if (OnProcessColor != null)
                {
                    OnProcessColor(colorData);
                }
                ColorImage.SetData(colorData);
            }
            if ((modified & FrameSourceTypes.Depth) != 0 || 
                (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.IncludeJoints) && (modified & FrameSourceTypes.Body) != 0))
            {
                if (depthImage == null)
                {
                    depthImage = RenderTarget2D.New(graphics, DepthImageSize.X, DepthImageSize.Y, MipMapCount.Auto, PixelFormat.B8G8R8X8.UNorm);
                    depthRender = new ColorBGRA[depthData.Length];
                }
                byte intensity = 0;
                for (int i = 0; i < depthRender.Length; i++)
                {
                    if (minDepth <= depthData[i])
                    {
                        int distance = Math.Min(depthData[i] - minDepth, depthRange);
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
                if (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.UserColor) && bodyIndexData != null)
                {
                    byte bodyIndex;
                    for (int i = 0; i < depthRender.Length; i++)
                    {
                        bodyIndex = bodyIndexData[i];
                        if (ActivePlayerIndex != NO_ACTIVE_PLAYER)
                        {
                            // Ensure the active player always has the first user color
                            if (bodyIndex == ActivePlayerIndex)
                            {
                                bodyIndex = 0;
                            }
                            else if (bodyIndex < ActivePlayerIndex)
                            {
                                bodyIndex++;
                            }
                        }
                        switch (bodyIndex)
                        {
                            case byte.MaxValue:
                                break;
                            default:
                                depthRender[i] *= UserColors[bodyIndex];
                                break;
                        }
                    }
                }
                depthImage.SetData(depthRender);
                if (0 != (DepthImageMode & KinectDXBootstrap.DepthImageMode.IncludeJoints) && ActivePlayer != null)
                {
                    // Map to appropriate space
                    SharpDX.Direct3D11.DepthStencilView depthView;
                    SharpDX.Direct3D11.RenderTargetView[] renderTargets = graphics.GetRenderTargets(out depthView);
                    graphics.SetRenderTargets(depthImage);
                    CameraSpacePoint[] cameraPoints = ActivePlayer.Joints.Select(joint => joint.Value.Position).ToArray();
                    DepthSpacePoint[] depthPoints = new DepthSpacePoint[cameraPoints.Length];
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
                    foreach (DepthSpacePoint point in depthPoints)
                    {
                        v.X = point.X - JointImage.Width / 2; 
                        v.Y = point.Y - JointImage.Height / 2;
                        spriteBatch.Draw(JointImage, v, Color.White);
                    }
                    spriteBatch.End();
                    graphics.SetRenderTargets(renderTargets);
                }
            }
            this.modified = FrameSourceTypes.None;
            return modified;
        }

        public ushort GetDepth(int x, int y)
        {
            return depthData[y * DepthImageSize.X + x];
        }
        public byte GetPlayerIndex(int x, int y)
        {
            return bodyIndexData[y * DepthImageSize.X + x];
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
                data.MinDepth = minDepth;
                data.MaxDepth = (ushort)(minDepth + depthRange);
                for (data.Index = 0, data.Y = 0; data.Index < depthData.Length; data.Y++)
                {
                    for (data.X = 0; data.X < DepthImageSize.X; data.X++, data.Index++)
                    {
                        data.Depth = depthData[data.Index];
                        data.PlayerIndex = bodyIndexData[data.Index];
                        yield return data;
                    }
                }
            }
        }

        #region Update
        public bool Update(object frameReader)
        {
            bool updated = false;
            if (IsAvailable != wasAvailable)
            {
                wasAvailable = IsAvailable;
                updated = true;
            }
            var reader = frameReader as MultiSourceFrameReader;
            if (reader != null)
            {
                updated |= Update(reader.AcquireLatestFrame());
            }
            var faceReader = frameReader as FaceFrameReader;
            if (faceReader != null)
            {
                updated |= Update(faceReader.AcquireLatestFrame());
            }
            return updated;
        }

        public bool Update(MultiSourceFrame frame)
        {
            if (frame != null)
            {
                // Update each source
                Update(frame.BodyFrameReference.AcquireFrame());
                Update(frame.BodyIndexFrameReference.AcquireFrame());
                Update(frame.ColorFrameReference.AcquireFrame());
                Update(frame.DepthFrameReference.AcquireFrame());
                Update(frame.InfraredFrameReference.AcquireFrame());
                Update(frame.LongExposureInfraredFrameReference.AcquireFrame());
                return true;
            }
            return false;
        }

        public bool Update(BodyFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (bodies == null)
                    {
                        const int count = 6; // frame.BodyFrameSource.BodyCount
                        bodies = new Body[count];
                        players = new WrappedBody[count];
                        for (int i = 0; i < count; i++)
                        {
                            players[i] = new WrappedBody(bodies[i]);
                        }
                    }
                    frame.GetAndRefreshBodyData(bodies);
                    for (int i = 0; i < bodies.Length; i++)
                    {
                        players[i].body = bodies[i];
                    }
                    // Choose a player that is tracked, preferring one that is centered and engaged
                    var active = bodies.Select((b, i) => new KeyValuePair<byte, Body>((byte)i, b))
                        .Where(kv => kv.Value.IsTracked)
                        .OrderBy(kv =>
                            Math.Abs(kv.Value.Joints[JointType.SpineMid].Position.X)
                            + Math.Abs(2 - kv.Value.Joints[JointType.SpineMid].Position.Z));
                    HadPlayer = ActivePlayer != null;
                    if (active.Count() == 0)
                    {
                        activePlayer.body = null;
                        ActivePlayerIndex = NO_ACTIVE_PLAYER;
                    }
                    else
                    {
                        var mostActive = active.First();
                        ActivePlayerIndex = mostActive.Key;
                        activePlayer.body = mostActive.Value;
                    }
                    modified |= FrameSourceTypes.Body;
                    return true;
                }
            }
            return false;
        }
        public bool Update(BodyIndexFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (bodyIndexData == null)
                    {
                        bodyIndexData = new byte[frame.FrameDescription.LengthInPixels];
                    }
                    frame.CopyFrameDataToArray(bodyIndexData);
                    modified |= FrameSourceTypes.BodyIndex;
                    return true;
                }
            }
            return false;
        }
        public bool Update(ColorFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (colorData == null)
                    {
                        var frameDescription = frame.FrameDescription;
                        colorData = new byte[frameDescription.LengthInPixels * colorImageBPP];
                        ColorImageSize = new Point(frameDescription.Width, frameDescription.Height);
                    }
                    if (frame.RawColorImageFormat == colorImageFormat)
                    {
                        frame.CopyRawFrameDataToArray(colorData);
                    }
                    else
                    {
                        frame.CopyConvertedFrameDataToArray(colorData, colorImageFormat);
                    }
                    modified |= FrameSourceTypes.Color;
                    return true;
                }
            }
            return false;
        }
        public bool Update(DepthFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (depthData == null)
                    {
                        var frameDescription = frame.FrameDescription;
                        depthData = new ushort[frameDescription.LengthInPixels];
                        DepthImageSize = new Point(frameDescription.Width, frameDescription.Height);
                        if (bodyIndexData == null)
                        {
                            bodyIndexData = new byte[depthData.Length];
                            for (int i = 0; i < bodyIndexData.Length; i++)
                            {
                                bodyIndexData[i] = byte.MaxValue;
                            }
                        }
                    }
                    frame.CopyFrameDataToArray(depthData);
                    minDepth = frame.DepthMinReliableDistance;
                    depthRange = frame.DepthMaxReliableDistance - minDepth;
                    modified |= FrameSourceTypes.Depth;
                    return true;
                }
            }
            return false;
        }
        public bool Update(InfraredFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (infraredData == null)
                    {
                        infraredData = new ushort[frame.FrameDescription.LengthInPixels];
                    }
                    frame.CopyFrameDataToArray(infraredData);
                    modified |= FrameSourceTypes.Infrared;
                    return true;
                }
            }
            return false;
        }
        public bool Update(LongExposureInfraredFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    if (infraredExposureData == null)
                    {
                        infraredExposureData = new ushort[frame.FrameDescription.LengthInPixels];
                    }
                    frame.CopyFrameDataToArray(infraredExposureData);
                    modified |= FrameSourceTypes.LongExposureInfrared;
                    return true;
                }
            }
            return false;
        }
        public bool Update(FaceFrame frame)
        {
            if (frame != null)
            {
                using (frame)
                {
                    ActivePlayerFace = frame.FaceFrameResult;
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
