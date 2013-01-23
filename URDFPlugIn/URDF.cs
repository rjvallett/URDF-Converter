using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Schema;

namespace URDF
{
    public class Robot : ICloneable
    {
        public string Name;
        public List<Link> Links = new List<Link>();
        public List<Joint> Joints = new List<Joint>();

        /// <summary>
        /// Initialize a URDF robot with a given name
        /// </summary>
        /// <param name="name">Robot name</param>
        public Robot(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();
            return obj;
        }

        /// <summary>
        /// Write the robot's structure to a URDF file
        /// </summary>
        /// <param name="filename">URDF filename</param>
        public void PrintURDF(string filename)
        {
            XmlTextWriter URDFWriter = new XmlTextWriter(filename, null);
            URDFWriter.Formatting = Formatting.Indented;
            URDFWriter.WriteStartDocument(false);
            URDFWriter.WriteComment(" Exported at " + DateTime.Now.ToString() + " ");
            URDFWriter.WriteStartElement("robot");
            URDFWriter.WriteAttributeString("name", this.Name);


            foreach (Link link in this.Links)
            {
                link.PrintLinkURDF(URDFWriter);
            }

            foreach (Joint joint in this.Joints)
            {
                joint.PrintJointURDF(URDFWriter);
            }

            URDFWriter.WriteEndElement();

            //Write the XML to file and close the writer
            URDFWriter.Flush();
            URDFWriter.Close();
            if (URDFWriter != null)
                URDFWriter.Close();

        }

    }

    [Serializable]
    public class Link : ICloneable
    {
        public string Name;
        public Link Parent;
        public Inertial Intertial;
        public Visual.Geometry Geometry;
        public Visual.Materal Material;
        public Sensor Sensor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Link(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();
            return obj;
        }

        public void PrintLinkURDF(XmlTextWriter URDFWriter)
        {
            URDFWriter.WriteStartElement("link");
            URDFWriter.WriteAttributeString("name", Name);

            if (this.Intertial != null)
            {
                URDFWriter.WriteStartElement("inertial");
                if (this.Intertial.XYZ != null || this.Intertial.RPY != null)
                {
                    URDFWriter.WriteStartElement("origin");
                    if (this.Intertial.XYZ != null)
                        URDFWriter.WriteAttributeString("xyz", this.Intertial.XYZ[0].ToString() + " " + this.Intertial.XYZ[1].ToString() + " " + this.Intertial.XYZ[2].ToString());
                    if (this.Intertial.RPY != null)
                        URDFWriter.WriteAttributeString("rpy", this.Intertial.RPY[0].ToString() + " " + this.Intertial.RPY[1].ToString() + " " + this.Intertial.RPY[2].ToString());
                    URDFWriter.WriteEndElement();
                }
                if (this.Intertial.Mass >= 0.0)
                {
                    URDFWriter.WriteStartElement("mass");
                    URDFWriter.WriteAttributeString("value", this.Intertial.Mass.ToString());
                    URDFWriter.WriteEndElement();
                }
                if (this.Intertial.Inertia != null)
                {
                    URDFWriter.WriteStartElement("inertia");
                    URDFWriter.WriteAttributeString("ixx", this.Intertial.Inertia[0, 0].ToString());
                    URDFWriter.WriteAttributeString("ixy", this.Intertial.Inertia[0, 1].ToString());
                    URDFWriter.WriteAttributeString("ixz", this.Intertial.Inertia[0, 2].ToString());
                    URDFWriter.WriteAttributeString("iyy", this.Intertial.Inertia[1, 1].ToString());
                    URDFWriter.WriteAttributeString("iyz", this.Intertial.Inertia[1, 2].ToString());
                    URDFWriter.WriteAttributeString("izz", this.Intertial.Inertia[2, 2].ToString());
                    URDFWriter.WriteEndElement();
                }
                URDFWriter.WriteEndElement();
            }

            if (this.Geometry != null || this.Material != null)
            {
                URDFWriter.WriteStartElement("visual");
                if (this.Geometry.XYZ != null || this.Geometry.RPY != null)
                {
                    URDFWriter.WriteStartElement("origin");
                    if (this.Geometry.XYZ != null)
                        URDFWriter.WriteAttributeString("xyz", this.Geometry.XYZ[0].ToString() + " " + this.Geometry.XYZ[1].ToString() + " " + this.Geometry.XYZ[2].ToString());
                    if (this.Geometry.RPY != null)
                        URDFWriter.WriteAttributeString("rpy", this.Geometry.RPY[0].ToString() + " " + this.Geometry.RPY[1].ToString() + " " + this.Geometry.RPY[2].ToString());
                    URDFWriter.WriteEndElement();
                }

                if (this.Geometry != null)
                {
                    URDFWriter.WriteStartElement("geometry");
                    URDFWriter.WriteStartElement(this.Geometry.Shape.Type);
                    foreach (string[] value in this.Geometry.Shape.Value)
                    {
                        URDFWriter.WriteAttributeString(value[0], value[1]);
                    }

                    URDFWriter.WriteEndElement();
                    URDFWriter.WriteEndElement();
                }



                URDFWriter.WriteEndElement();
            }

            URDFWriter.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class Inertial : Origin
        {
            public double Mass { get; set; }
            public double[,] Inertia { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class Visual
        {
            /// <summary>
            /// 
            /// </summary>
            [Serializable]
            public class Geometry : Origin
            {
                public Shape Shape { get; set; }

                public Geometry(Shape shape)
                {
                    this.Shape = shape;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [Serializable]
            public class Materal
            {
                public string Name;
                public double[] ColorRGBA = new double[4];
                public string Texture;

                public Materal(string name)
                {
                    this.Name = name;
                }

                public Materal(string name, double[] colorRGBA)
                {
                    this.Name = name;
                    this.ColorRGBA = colorRGBA;
                }

                public Materal(string name, string textureFilename)
                {
                    this.Name = name;
                    this.Texture = textureFilename;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Joint : ICloneable
    {
        public string Name;
        public Link Parent, Child;
        public JointType JointType;
        public double[] Axis = new double[] { 1, 0, 0 };

        public Calibration Calibration;
        public Limit Limit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="jointType"></param>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public Joint(string name, JointType jointType, Link parent, Link child)
        {
            this.Name = name;
            this.JointType = jointType;
            this.Parent = parent;
            this.Child = child;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();
            return obj;
        }

        public void PrintJointURDF(XmlTextWriter URDFWriter)
        {
            URDFWriter.WriteStartElement("joint");
            URDFWriter.WriteAttributeString("name", this.Name);
            URDFWriter.WriteAttributeString("type", this.JointType.Type);
            URDFWriter.WriteStartElement("parent");
            URDFWriter.WriteAttributeString("link", this.Parent.Name);
            URDFWriter.WriteEndElement();
            URDFWriter.WriteStartElement("child");
            URDFWriter.WriteAttributeString("link", this.Child.Name);
            URDFWriter.WriteEndElement();
            URDFWriter.WriteStartElement("axis");
            URDFWriter.WriteAttributeString("xyz", this.Axis[0].ToString() + " " + this.Axis[1].ToString() + " " + this.Axis[2].ToString());
            URDFWriter.WriteEndElement();

            if (this.Calibration != null)
            {
                URDFWriter.WriteStartElement("calibration");
                URDFWriter.WriteAttributeString(this.Calibration.Type, this.Calibration.Value.ToString());
                URDFWriter.WriteEndElement();
            }

            URDFWriter.WriteEndElement();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Shape
    {
        public static readonly Shape Box = new Shape(new double[3] { 1.0, 1.0, 1.0 });
        public static readonly Shape Cylinder = new Shape(1.0, 1.0);
        public static readonly Shape Mesh = new Shape("", 1.0);
        public static readonly Shape Sphere = new Shape(1.0);

        public Shape()
        {

        }

        private Shape(double[] size)
        {
            this.Type = "box";
            this.Size = size;
            string[][] value = { new[] { "size", this.Size[0].ToString() + " " + this.Size[1].ToString() + " " + this.Size[2].ToString() } };
            this.Value = value;


        }

        private Shape(double length, double radius)
        {
            this.Type = "cylinder";
            this.Length = length;
            this.Radius = radius;
            string[][] value = { new[] { "length", this.Length.ToString() },
                                 new[] { "radius", this.Radius.ToString() } };
            this.Value = value;

        }

        private Shape(double radius)
        {
            this.Type = "sphere";
            this.Radius = radius;
            string[][] value = { new[] { "radius", this.Radius.ToString() } };
            this.Value = value;
        }

        private Shape(string filename)
        {
            this.Type = "mesh";
            this.Filename = filename;
            this.Scale = 1.0;
            string[][] value = { new[] { "filename", "package://" + this.Filename },
                                 new[] { "scale", this.Scale.ToString() } };
            this.Value = value;
        }

        private Shape(string filename, double scale)
        {
            this.Type = "mesh";
            this.Filename = filename;
            this.Scale = scale;
            string[][] value = { new[] { "filename", "package://" + this.Filename },
                                 new[] { "scale", this.Scale.ToString() } };
            this.Value = value;
        }


        public string Type { get; private set; }
        public string[][] Value { get; private set; }
        private double[] Size { get; set; }
        private double Length { get; set; }
        private double Radius { get; set; }
        private string Filename { get; set; }
        private double Scale { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Origin
    {
        public double[] XYZ { get; set; }
        public double[] RPY { get; set; }

        //public Origin()
        //{

        //}

        //public Origin(double[] xyz)
        //{
        //    this.XYZ = xyz;
        //}

        //public Origin(double[] xyz, double[] rpy)
        //{
        //    this.XYZ = xyz;
        //    this.RPY = rpy;
        //}
    }


    private class Sensor : Origin
    {
        public string Name;
        public double UpdateRate;
        public Link Parent;

        public Sensor()
        {

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Camera : Sensor
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public ImageFormat Format { get; set; }
        public double HFOV { get; set; }
        public double Near { get; set; }
        public double Far { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Camera()
        {
            this.Width = 640;
            this.Height = 480;
            this.Format = ImageFormat.RGB8;
            this.HFOV = Math.PI/2;
            this.Near = 0;
            this.Far = 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        /// <param name="hfov"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        public Camera(int width, int height, ImageFormat format, double hfov, double near, double far)
        {
            this.Width = width;
            this.Height = height;
            this.Format = format;
            this.HFOV = hfov;
            this.Near = near;
            this.Far = far;
        }

    }

    public class Ray : Sensor
    {
        public Ray()
        {

        }


    }



    /// <summary>
    /// 
    /// </summary>
    public sealed class ImageFormat
    {
        public static readonly ImageFormat RGB8 = new ImageFormat("rgb8");
        public static readonly ImageFormat RGBA8 = new ImageFormat("rgba8");
        public static readonly ImageFormat RGB16 = new ImageFormat("prismatic");
        public static readonly ImageFormat RGBA16 = new ImageFormat("fixed");
        public static readonly ImageFormat BGR8 = new ImageFormat("revolute");
        public static readonly ImageFormat BGRA8 = new ImageFormat("continuous");
        public static readonly ImageFormat BGR16 = new ImageFormat("prismatic");
        public static readonly ImageFormat BGRA16 = new ImageFormat("fixed");
        public static readonly ImageFormat Mono8 = new ImageFormat("floating");
        public static readonly ImageFormat Mono16 = new ImageFormat("planar");

        private ImageFormat(string type)
        {
            Type = type;
        }

        public string Type { get; private set; }
    }
    }


    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class JointType
    {
        public static readonly JointType Revolute = new JointType("revolute");
        public static readonly JointType Continuous = new JointType("continuous");
        public static readonly JointType Prismatic = new JointType("prismatic");
        public static readonly JointType Fixed = new JointType("fixed");
        public static readonly JointType Floating = new JointType("floating");
        public static readonly JointType Planar = new JointType("planar");

        private JointType(string type)
        {
            Type = type;
        }

        public string Type { get; private set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class Calibration
    {
        public static readonly Calibration Rising = new Calibration("rising", 0.0);
        public static readonly Calibration Falling = new Calibration("falling", 0.0);

        public Calibration()
        {

        }

        private Calibration(string type, double value)
        {
            Type = type;
            Value = value;
        }

        public string Type { get; private set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class Limit
    {
        public Limit()
        {

        }

        public double Effort { get; set; }
        public double Velocity { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
    }






}