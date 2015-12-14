using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectDXBootstrap
{
    public static class Extensions
    {
        public static bool IsBehind(this Joint joint, Joint other)
        {
            return joint.Position.Z > other.Position.Z;
        }
        public static bool IsAbove(this Joint joint, Joint other)
        {
            return joint.Position.Y > other.Position.Y;
        }
        public static bool IsBelow(this Joint joint, Joint other)
        {
            return joint.Position.Y < other.Position.Y;
        }
        public static bool IsLeftOf(this Joint joint, Joint other)
        {
            return joint.Position.X < other.Position.X;
        }
        public static bool IsRightOf(this Joint joint, Joint other)
        {
            return joint.Position.Y < other.Position.Y;
        }
        public static CameraSpacePoint Position(this Body player)
        {
            return player.Joints[JointType.SpineMid].Position;
        }
    }
}
