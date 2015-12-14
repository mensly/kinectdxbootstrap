using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectDXBootstrap
{
    public interface ICoordinateMapper
    {
        //
        // Summary:
        //     Maps an array of points from camera space to color space.
        //
        // Parameters:
        //   cameraPoints:
        //     The points to map from camera space.
        //
        //   colorPoints:
        //     The to-be-filled points mapped to color space.
        //
        // Remarks:
        //     The colorPoints array should be the same size as the cameraPoints array.
        void MapCameraPointsToColorSpace(CameraSpacePoint[] cameraPoints, ColorSpacePoint[] colorPoints);
        //
        // Summary:
        //     Maps an array of points from camera space to depth space.
        //
        // Parameters:
        //   cameraPoints:
        //     The points to map from camera space.
        //
        //   depthPoints:
        //     The to-be-filled points mapped to depth space.
        //
        // Remarks:
        //     The depthPoints array should be the same size as the cameraPoints array.
        void MapCameraPointsToDepthSpace(CameraSpacePoint[] cameraPoints, DepthSpacePoint[] depthPoints);
        //
        // Summary:
        //     Maps a point from camera space to color space.
        //
        // Parameters:
        //   cameraPoint:
        //     The point to map from camera space.
        //
        // Returns:
        //     The point mapped to color space.
        ColorSpacePoint MapCameraPointToColorSpace(CameraSpacePoint cameraPoint);
        //
        // Summary:
        //     Maps a point from camera space to depth space.
        //
        // Parameters:
        //   cameraPoint:
        //     The point to map from camera space.
        //
        // Returns:
        //     The point mapped to depth space.
        DepthSpacePoint MapCameraPointToDepthSpace(CameraSpacePoint cameraPoint);
        //
        // Summary:
        //     Uses the depth frame data to map the entire frame from color space to camera
        //     space.
        //
        // Parameters:
        //   depthFrameData:
        //     The full image data from a depth frame.
        //
        //   cameraSpacePoints:
        //     The to-be-filled array of mapped camera points.
        //
        // Remarks:
        //     The cameraSpacePoints array should be the size of the number of color frame
        //     pixels.
        void MapColorFrameToCameraSpace(ushort[] depthFrameData, CameraSpacePoint[] cameraSpacePoints);
        //
        // Summary:
        //     Uses the depth frame data to map the entire frame from color space to depth
        //     space.
        //
        // Parameters:
        //   depthFrameData:
        //     The full image data from a depth frame.
        //
        //   depthSpacePoints:
        //     The to-be-filled array of mapped depth points.
        //
        // Remarks:
        //     The depthSpacePoints array should be the size of the number of color frame
        //     pixels.
        void MapColorFrameToDepthSpace(ushort[] depthFrameData, DepthSpacePoint[] depthSpacePoints);
        //
        // Summary:
        //     Uses the depth frame data to map the entire frame from depth space to camera
        //     space.
        //
        // Parameters:
        //   depthFrameData:
        //     The full image data from a depth frame.
        //
        //   cameraSpacePoints:
        //     The to-be-filled points mapped to camera space.
        //
        // Remarks:
        //     The cameraSpacePoints array should be the same size as the depthPoints and
        //     depths arrays.
        void MapDepthFrameToCameraSpace(ushort[] depthFrameData, CameraSpacePoint[] cameraSpacePoints);
        //
        // Summary:
        //     Uses the depth frame data to map the entire frame from depth space to color
        //     space.
        //
        // Parameters:
        //   depthFrameData:
        //     The full image data from a depth frame.
        //
        //   colorSpacePoints:
        //     The to-be-filled array of mapped color points
        //
        // Remarks:
        //     The colorSpacePoints array should be the size of the number of depth frame
        //     pixels.
        void MapDepthFrameToColorSpace(ushort[] depthFrameData, ColorSpacePoint[] colorSpacePoints);
        //
        // Summary:
        //     Maps an array of points/depths from depth space to camera space.
        //
        // Parameters:
        //   depthPoints:
        //     The points to map from depth space.
        //
        //   depths:
        //     The depths of the points in depth space.
        //
        //   cameraPoints:
        //     The to-be-filled points mapped to camera space.
        //
        // Remarks:
        //     The cameraPoints array should be the same size as the depthPoints and depths
        //     arrays.
        void MapDepthPointsToCameraSpace(DepthSpacePoint[] depthPoints, ushort[] depths, CameraSpacePoint[] cameraPoints);
        //
        // Summary:
        //     Maps an array of points/depths from depth space to color space.
        //
        // Parameters:
        //   depthPoints:
        //     The points to map from depth space.
        //
        //   depths:
        //     The depths of the points in depth space.
        //
        //   colorPoints:
        //     The to-be-filled points mapped to color space.
        //
        // Remarks:
        //     The colorPoints array should be the same size as the depthPoints and depths
        //     arrays.
        void MapDepthPointsToColorSpace(DepthSpacePoint[] depthPoints, ushort[] depths, ColorSpacePoint[] colorPoints);
        //
        // Summary:
        //     Maps a point/depth from depth space to camera space.
        //
        // Parameters:
        //   depthPoint:
        //     The point to map from depth space.
        //
        //   depth:
        //     The depth of the point in depth space.
        //
        // Returns:
        //     The point mapped to camera space.
        CameraSpacePoint MapDepthPointToCameraSpace(DepthSpacePoint depthPoint, ushort depth);
        //
        // Summary:
        //     Maps a point/depth from depth space to color space.
        //
        // Parameters:
        //   depthPoint:
        //
        //   depth:
        //
        // Returns:
        //     The mapped to camera space.
        ColorSpacePoint MapDepthPointToColorSpace(DepthSpacePoint depthPoint, ushort depth);
    }


    class WrappedCoordinateMapper : ICoordinateMapper
    {
        public CoordinateMapper mapper;
        public WrappedCoordinateMapper(CoordinateMapper mapper)
        {
            this.mapper = mapper;
        }

        public void MapCameraPointsToColorSpace(CameraSpacePoint[] cameraPoints, ColorSpacePoint[] colorPoints)
        {
            mapper.MapCameraPointsToColorSpace(cameraPoints, colorPoints);
        }
        public void MapCameraPointsToDepthSpace(CameraSpacePoint[] cameraPoints, DepthSpacePoint[] depthPoints)
        {
            mapper.MapCameraPointsToDepthSpace(cameraPoints, depthPoints);
        }
        public ColorSpacePoint MapCameraPointToColorSpace(CameraSpacePoint cameraPoint)
        {
            return mapper.MapCameraPointToColorSpace(cameraPoint);
        }
        public DepthSpacePoint MapCameraPointToDepthSpace(CameraSpacePoint cameraPoint)
        {
            return mapper.MapCameraPointToDepthSpace(cameraPoint);
        }
        public void MapColorFrameToCameraSpace(ushort[] depthFrameData, CameraSpacePoint[] cameraSpacePoints)
        {
            mapper.MapColorFrameToCameraSpace(depthFrameData, cameraSpacePoints);
        }
        public void MapColorFrameToDepthSpace(ushort[] depthFrameData, DepthSpacePoint[] depthSpacePoints)
        {
            mapper.MapColorFrameToDepthSpace(depthFrameData, depthSpacePoints);
        }
        public void MapDepthFrameToCameraSpace(ushort[] depthFrameData, CameraSpacePoint[] cameraSpacePoints)
        {
            mapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);
        }
        public void MapDepthFrameToColorSpace(ushort[] depthFrameData, ColorSpacePoint[] colorSpacePoints)
        {
            mapper.MapDepthFrameToColorSpace(depthFrameData, colorSpacePoints);
        }
        public void MapDepthPointsToCameraSpace(DepthSpacePoint[] depthPoints, ushort[] depths, CameraSpacePoint[] cameraPoints)
        {
            mapper.MapDepthPointsToCameraSpace(depthPoints, depths, cameraPoints);
        }
        public void MapDepthPointsToColorSpace(DepthSpacePoint[] depthPoints, ushort[] depths, ColorSpacePoint[] colorPoints)
        {
            mapper.MapDepthPointsToColorSpace(depthPoints, depths, colorPoints);
        }
        public CameraSpacePoint MapDepthPointToCameraSpace(DepthSpacePoint depthPoint, ushort depth)
        {
            return mapper.MapDepthPointToCameraSpace(depthPoint, depth);
        }
        public ColorSpacePoint MapDepthPointToColorSpace(DepthSpacePoint depthPoint, ushort depth)
        {
            return mapper.MapDepthPointToColorSpace(depthPoint, depth);
        }

    }
}
