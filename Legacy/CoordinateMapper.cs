extern alias oldKinect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NewKinect = Microsoft.Kinect;
using oldKinect.Microsoft.Kinect;
using OldKinect = oldKinect.Microsoft.Kinect;

using KinectDXBootstrap;

namespace KinectDXBootstrap.Legacy
{
    class CoordinateMapper : ICoordinateMapper
    {
        private const ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;
        private const DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution640x480Fps30;

        private const string ERROR_MESSAGE = "This method has not been implemented for legacy Kinect yet";

        public OldKinect.CoordinateMapper mapper;
        public CoordinateMapper(OldKinect.CoordinateMapper mapper)
        {
            this.mapper = mapper;
        }


        public void MapCameraPointsToColorSpace(NewKinect.CameraSpacePoint[] cameraPoints, NewKinect.ColorSpacePoint[] colorPoints)
        {
            int count = Math.Min(cameraPoints.Length, colorPoints.Length);
            for (int i = 0; i < count; i++)
            {
                colorPoints[i] = MapCameraPointToColorSpace(cameraPoints[i]);
            }
        }
        public void MapCameraPointsToDepthSpace(NewKinect.CameraSpacePoint[] cameraPoints, NewKinect.DepthSpacePoint[] depthPoints)
        {
            int count = Math.Min(cameraPoints.Length, depthPoints.Length);
            for (int i = 0; i < count; i++)
            {
                depthPoints[i] = MapCameraPointToDepthSpace(cameraPoints[i]);
            }
        }

        public NewKinect.ColorSpacePoint MapCameraPointToColorSpace(NewKinect.CameraSpacePoint cameraPoint)
        {
            var point = mapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
            {
                X = cameraPoint.X,
                Y = cameraPoint.Y,
                Z = cameraPoint.Z
            }, COLOR_FORMAT);
            return new NewKinect.ColorSpacePoint()
            {
                X = point.X,
                Y = point.Y
            };
        }
        public NewKinect.DepthSpacePoint MapCameraPointToDepthSpace(NewKinect.CameraSpacePoint cameraPoint)
        {
            var point = mapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
            {
                X = cameraPoint.X,
                Y = cameraPoint.Y,
                Z = cameraPoint.Z
            }, DEPTH_FORMAT);
            return new NewKinect.DepthSpacePoint()
            {
                X = point.X,
                Y = point.Y
            };
        }

        public void MapColorFrameToCameraSpace(ushort[] depthFrameData, NewKinect.CameraSpacePoint[] cameraSpacePoints)
        {
            DepthImagePixel[] depthPixels = depthFrameData.Select(depth => new DepthImagePixel() { Depth = (short)depth }).ToArray();
            SkeletonPoint[] skeletonPoints = new SkeletonPoint[cameraSpacePoints.Length];
            mapper.MapColorFrameToSkeletonFrame(COLOR_FORMAT, DEPTH_FORMAT, depthPixels, skeletonPoints);
            int count = Math.Min(depthPixels.Length, skeletonPoints.Length);
            for (int i = 0; i < count; i++)
            {
                cameraSpacePoints[i] = skeletonPoints[i].Convert();
            }
        }
        public void MapColorFrameToDepthSpace(ushort[] depthFrameData, NewKinect.DepthSpacePoint[] depthSpacePoints)
        {
            DepthImagePixel[] depthPixels = depthFrameData.Select(depth => new DepthImagePixel() { Depth = (short)depth }).ToArray();
            DepthImagePoint[] depthPoints = new DepthImagePoint[depthSpacePoints.Length];
            mapper.MapColorFrameToDepthFrame(COLOR_FORMAT, DEPTH_FORMAT, depthPixels, depthPoints);
            int count = Math.Min(depthPixels.Length, depthPoints.Length);
            for (int i = 0; i < count; i++)
            {
                depthSpacePoints[i] = new NewKinect.DepthSpacePoint()
                {
                    X = depthPoints[i].X,
                    Y = depthPoints[i].Y
                };
            }
        }
        public void MapDepthFrameToCameraSpace(ushort[] depthFrameData, NewKinect.CameraSpacePoint[] cameraSpacePoints)
        {
            DepthImagePixel[] depthPixels = depthFrameData.Select(depth => new DepthImagePixel() { Depth = (short)depth }).ToArray();
            SkeletonPoint[] skeletonPoints = new SkeletonPoint[cameraSpacePoints.Length];
            mapper.MapDepthFrameToSkeletonFrame(DEPTH_FORMAT, depthPixels, skeletonPoints);
            int count = Math.Min(depthPixels.Length, skeletonPoints.Length);
            for (int i = 0; i < count; i++)
            {
                cameraSpacePoints[i] = skeletonPoints[i].Convert();
            }
        }
        public void MapDepthFrameToColorSpace(ushort[] depthFrameData, NewKinect.ColorSpacePoint[] colorSpacePoints)
        {
            DepthImagePixel[] depthPixels = depthFrameData.Select(depth => new DepthImagePixel() { Depth = (short)depth }).ToArray();
            ColorImagePoint[] colorPoints = new ColorImagePoint[colorSpacePoints.Length];
            mapper.MapDepthFrameToColorFrame(DEPTH_FORMAT, depthPixels, COLOR_FORMAT, colorPoints);
            int count = Math.Min(depthPixels.Length, colorPoints.Length);
            for (int i = 0; i < count; i++)
            {
                colorSpacePoints[i] = new NewKinect.ColorSpacePoint()
                {
                    X = colorPoints[i].X,
                    Y = colorPoints[i].Y
                };
            }
        }

        public void MapDepthPointsToCameraSpace(NewKinect.DepthSpacePoint[] depthPoints, ushort[] depths, NewKinect.CameraSpacePoint[] cameraPoints)
        {
            int count = Math.Min(depthPoints.Length, cameraPoints.Length);
            for (int i = 0; i < count; i++)
            {
                var point = mapper.MapDepthPointToSkeletonPoint(DEPTH_FORMAT, new DepthImagePoint()
                {
                    X = (int)depthPoints[i].X,
                    Y = (int)depthPoints[i].Y,
                    Depth = depths[i]
                });
                cameraPoints[i] = new NewKinect.CameraSpacePoint()
                {
                    X = point.X,
                    Y = point.Y,
                    Z = point.Z
                };
            }
        }
        public void MapDepthPointsToColorSpace(NewKinect.DepthSpacePoint[] depthPoints, ushort[] depths, NewKinect.ColorSpacePoint[] colorPoints)
        {
            int count = Math.Min(depthPoints.Length, colorPoints.Length);
            for (int i = 0; i < count; i++)
            {
                var point = mapper.MapDepthPointToColorPoint(DEPTH_FORMAT, new DepthImagePoint()
                {
                    X = (int)depthPoints[i].X,
                    Y = (int)depthPoints[i].Y,
                    Depth = depths[i]
                }, COLOR_FORMAT);
                colorPoints[i] = new NewKinect.ColorSpacePoint()
                {
                    X = point.X,
                    Y = point.Y
                };
            }
        }

        public NewKinect.CameraSpacePoint MapDepthPointToCameraSpace(NewKinect.DepthSpacePoint depthPoint, ushort depth)
        {
            var point = mapper.MapDepthPointToSkeletonPoint(DEPTH_FORMAT, new DepthImagePoint()
            {
                X = (int)depthPoint.X,
                Y = (int)depthPoint.Y,
                Depth = depth
            });
            return new NewKinect.CameraSpacePoint()
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z
            };
        }
        public NewKinect.ColorSpacePoint MapDepthPointToColorSpace(NewKinect.DepthSpacePoint depthPoint, ushort depth)
        {
            var point = mapper.MapDepthPointToColorPoint(DEPTH_FORMAT, new DepthImagePoint()
            {
                X = (int)depthPoint.X,
                Y = (int)depthPoint.Y,
                Depth = depth
            }, COLOR_FORMAT);
            return new NewKinect.ColorSpacePoint()
            {
                X = point.X,
                Y = point.Y
            };
        }
    }
}
