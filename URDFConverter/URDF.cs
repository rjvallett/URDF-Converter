/*
 * Copyright (c) 2017 Richard Vallett
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Schema;

namespace URDF
{
    #region Robot
    /// <summary>
    /// Defines the URDF Robot model.
    /// </summary>
    [Serializable]
    public class Robot : ICloneable
    {
        public string Name { get; set; }
        public List<Link> Links = new List<Link>();
        public List<Joint> Joints = new List<Joint>();

        public Robot(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Clones the Robot object into a new object.
        /// </summary>
        /// <returns>Cloned Robot object.</returns>
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

        public void WriteURDFToFile(string filename)
        {
            XmlTextWriter URDFWriter = new XmlTextWriter(filename, null);
            URDFWriter.Formatting = Formatting.Indented;
            URDFWriter.WriteStartDocument(false);
            URDFWriter.WriteComment(" Exported at " + DateTime.Now.ToString() + " ");
            URDFWriter.WriteStartElement("robot");
            URDFWriter.WriteAttributeString("name", this.Name);

            foreach (Link link in Links)
            {
                link.PrintLinkTag(URDFWriter);
            }

            foreach (Joint joint in Joints)
            {
                joint.PrintJointTag(URDFWriter);
            }

            URDFWriter.WriteEndElement();

            //Write the XML to file and close the writer
            URDFWriter.Flush();
            URDFWriter.Close();
            if (URDFWriter != null)
                URDFWriter.Close();
        }
    }
    #endregion

    #region Link
    /// <summary>
    /// Defines the URDF Link model.
    /// </summary>
    [Serializable]
    public class Link : ICloneable
    {
        public string Name { get; set; }
        public Link Parent { get; set; }
        public Inertial Inertial { get; set; }
        public Visual Visual { get; set; }
        public Collision Collision { get; set; }
        public List<Collision> CollisionGroup { get; set; }

        public Link(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Clones the Link object into a new object.
        /// </summary>
        /// <returns>Cloned Link object.</returns>
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

        public void PrintLinkTag(XmlTextWriter URDFWriter)
        {
            /* <link name="...">
             *     <inertial>
             *         ...
             *     </inertial>
             *     <visual>
             *         ...
             *     </visual>
             *     <collision>
             *         ...
             *     </collision>
             * </link>
             */
            URDFWriter.WriteStartElement("link");
            URDFWriter.WriteAttributeString("name", this.Name);
            if (this.Inertial != null)
            {
                this.Inertial.PrintInertialTag(URDFWriter);
            }
            if (this.Visual != null)
            {
                this.Visual.PrintVisualTag(URDFWriter);
            }
            if (this.Collision != null)
            {
                this.Collision.PrintCollisionTag(URDFWriter);
            }
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// Link inertial properties.
    /// </summary>
    [Serializable]
    public class Inertial : Origin
    {
        public double Mass { get; set; }
        public double[,] InertiaMatrix { get; private set; }
        public double[] InertiaVector { get; private set; }

        /// <summary>
        /// Set link's mass and moment of inertia.
        /// </summary>
        /// <param name="mass">Link mass (Kg).</param>
        /// <param name="inertiaMatrix">3x3 element moment of inertia matrix (Kg*m^2) [Ixx Ixy Ixz; Ixy Iyy Iyz; Ixz Iyz Izz]</param>
        public Inertial(double mass, double[,] inertiaMatrix)
        {
            this.Mass = mass;
            this.InertiaMatrix = inertiaMatrix;
            this.InertiaVector = new double[] { inertiaMatrix[0, 0], 
                inertiaMatrix[0, 1], 
                inertiaMatrix[0, 2], 
                inertiaMatrix[1, 1], 
                inertiaMatrix[1, 2], 
                inertiaMatrix[2, 2] };
        }

        /// <summary>
        /// Set link's mass and moment of inertia.
        /// </summary>
        /// <param name="mass">Link mass (Kg).</param>
        /// <param name="inertiaVector">1x6 vector of principal moments and products of inertia (Kg*m^2) [Ixx Ixy Ixz Iyy Iyz Izz]</param>
        public Inertial(double mass, double[] inertiaVector)
        {
            this.Mass = mass;
            this.InertiaVector = inertiaVector;
            this.InertiaMatrix = new double[,] { 
                { inertiaVector[0], inertiaVector[1], inertiaVector[2] },
                { inertiaVector[1], inertiaVector[3], inertiaVector[4] },
                { inertiaVector[2], inertiaVector[4], inertiaVector[5] } };
        }

        public void PrintInertialTag(XmlTextWriter URDFWriter)
        {
            /* <inertial>
             *     <origin xyz="# # #" rpy="# # #"/>
             *     <mass value="#"/>
             *     <inertia ixx="#"  ixy="#"  ixz="#" iyy="#" iyz="#" izz="#" />
             * </inertial>
             */
            URDFWriter.WriteStartElement("inertial");
            PrintOriginTag(URDFWriter);
            URDFWriter.WriteStartElement("mass");
            URDFWriter.WriteAttributeString("value", this.Mass.ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteStartElement("inertia");
            URDFWriter.WriteAttributeString("ixx", this.InertiaVector[0].ToString());
            URDFWriter.WriteAttributeString("ixy", this.InertiaVector[1].ToString());
            URDFWriter.WriteAttributeString("ixz", this.InertiaVector[2].ToString());
            URDFWriter.WriteAttributeString("iyy", this.InertiaVector[3].ToString());
            URDFWriter.WriteAttributeString("iyz", this.InertiaVector[4].ToString());
            URDFWriter.WriteAttributeString("izz", this.InertiaVector[5].ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// Link visual properties.
    /// </summary>
    [Serializable]
    public class Visual : Origin
    {
        public Shape Shape { get; private set; }
        public Material Material { get; private set; }

        public Visual(Shape shape)
        {
            this.Shape = shape;
        }

        public Visual(Shape shape, Material material)
        {
            this.Shape = shape;
            this.Material = material;
        }

        public void PrintVisualTag(XmlTextWriter URDFWriter)
        {
            /* <visual>
             *     <origin ... />
             *     <geometry>
             *         ...
             *     </geometry>
             *     <material>
             *         ...
             *     </material>
             * </visual>
             */
            URDFWriter.WriteStartElement("visual");
            PrintOriginTag(URDFWriter);
            this.Shape.PrintGeometryTag(URDFWriter);
            if (Material != null)
            {
                this.Material.PrintMaterialTag(URDFWriter);
            }
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// Link material properties.
    /// </summary>
    [Serializable]
    public class Material
    {
        public string Name { get; set; }
        public double[] ColorRGBA { get; set; }

        public Material(string name, double[] colorRGBA)
        {
            this.Name = name;
            this.ColorRGBA = colorRGBA;
        }

        public void PrintMaterialTag(XmlTextWriter URDFWriter)
        {
            /* <material name="...">
             *     <color rgba="# # # #"/>
             * </material>
             */
            URDFWriter.WriteStartElement("material");
            URDFWriter.WriteAttributeString("name", this.Name);
            URDFWriter.WriteStartElement("color");
            URDFWriter.WriteAttributeString("rgba", this.ColorRGBA[0].ToString() + " "
                + this.ColorRGBA[1].ToString() + " "
                + this.ColorRGBA[2].ToString() + " "
                + this.ColorRGBA[3].ToString() + " ");
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }

    }

    /// <summary>
    /// Link collision properties.
    /// </summary>
    [Serializable]
    public class Collision : Origin
    {
        public Shape Shape { get; set; }

        public Collision(Shape shape)
        {
            this.Shape = shape;
        }

        public void PrintCollisionTag(XmlTextWriter URDFWriter)
        {
            /* <collision>
             *     <origin ... />
             *     <geometry>
             *         ...
             *     </geometry>
             * </collision>
             */
            URDFWriter.WriteStartElement("collision");
            PrintOriginTag(URDFWriter);
            this.Shape.PrintGeometryTag(URDFWriter);
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// Link and Joint origin properties.
    /// </summary>
    [Serializable]
    public class Origin
    {
        public double[] XYZ { get; set; }
        public double[] RPY { get; set; }

        public void PrintOriginTag(XmlTextWriter URDFWriter)
        {
            // <origin xyz="# # #" rpy="# # #"/>
            if (XYZ != null && RPY != null)
            {
                URDFWriter.WriteStartElement("origin");
                if (XYZ != null)
                {
                    URDFWriter.WriteAttributeString("xyz", XYZ[0].ToString() + " " + XYZ[1].ToString() + " " + XYZ[2].ToString());
                }
                if (RPY != null)
                {
                    URDFWriter.WriteAttributeString("rpy", RPY[0].ToString() + " " + RPY[1].ToString() + " " + RPY[2].ToString());
                }
                URDFWriter.WriteEndElement();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Shape
    {
        protected double[] Size = new double[3];
        protected double Radius, Length, Scale;
        protected string Filename;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="radius"></param>
        /// <param name="length"></param>
        /// <param name="filename"></param>
        /// <param name="scale"></param>
        public Shape(double[] size, double radius, double length, string filename, double scale)
        {
            this.Size = size;
            this.Radius = radius;
            this.Length = length;
            this.Filename = filename;
            this.Scale = scale;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="URDFWriter"></param>
        public virtual void PrintGeometryTag(XmlTextWriter URDFWriter)
        {
            // Insert code into inherited classes.
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Box : Shape
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="size">Extents of the box.</param>
        public Box(double[] size)
            : base(size, 0, 0, null, 0)
        {
        }

        public override void PrintGeometryTag(XmlTextWriter URDFWriter)
        {
            /* <geometry>
             *     <box size="# # #"/>
             * </geometry>
             */
            URDFWriter.WriteStartElement("geometry");
            URDFWriter.WriteStartElement("box");
            URDFWriter.WriteAttributeString("size", Size[0].ToString() + " " + Size[1].ToString() + " " + Size[2].ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Cylinder : Shape
    {
        public Cylinder(double radius, double length)
            : base(null, radius, length, null, 0)
        {
        }

        public override void PrintGeometryTag(XmlTextWriter URDFWriter)
        {
            /* <geometry>
             *     <cylinder radius="#" length="#"/>
             * </geometry>
             */
            URDFWriter.WriteStartElement("geometry");
            URDFWriter.WriteStartElement("cylinder");
            URDFWriter.WriteAttributeString("radius", Radius.ToString());
            URDFWriter.WriteAttributeString("length", Length.ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Sphere : Shape
    {
        public Sphere(double radius)
            : base(null, radius, 0, null, 0)
        {
        }

        public override void PrintGeometryTag(XmlTextWriter URDFWriter)
        {
            /* <geometry>
             *     <sphere radius="#"/>
             * </geometry>
             */
            URDFWriter.WriteStartElement("geometry");
            URDFWriter.WriteStartElement("cylinder");
            URDFWriter.WriteAttributeString("radius", Radius.ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Mesh : Shape
    {
        public Mesh(string filename)
            : base(null, 0, 0, filename, 1)
        {
        }

        public Mesh(string filename, double scale)
            : base(null, 0, 0, filename, scale)
        {
        }

        public override void PrintGeometryTag(XmlTextWriter URDFWriter)
        {
            /* <geometry>
             *     <sphere filename="package://..." scale="#"/>
             * </geometry>
             */
            URDFWriter.WriteStartElement("geometry");
            URDFWriter.WriteStartElement("mesh");
            URDFWriter.WriteAttributeString("filename", Filename);
            URDFWriter.WriteAttributeString("scale", Scale.ToString());
            URDFWriter.WriteEndElement();
            URDFWriter.WriteEndElement();
        }
    }

    #endregion

    #region Joint
    /// <summary>
    /// Defines the URDF Joint model.
    /// </summary>
    [Serializable]
    public class Joint : Origin
    {
        public string Name { get; set; }
        public JointType JointType { get; set; }
        public Link Parent { get; set; }
        public Link Child { get; set; }
        public Limit Limit { get; set; }

        public double[] Axis { get; set; }
        public Calibration Calibration { get; set; }
        public Dynamics Dynamics { get; set; }
        public SafetyController SafetyController { get; set; }

        public Joint(string name, JointType jointType)
        {
            this.Name = name;
            this.JointType = jointType;
            if (this.JointType == JointType.Revolute || this.JointType == JointType.Prismatic)
            {
                // Default values for limit that can be modified later.
                this.Limit = new Limit(1.0, 30.0, 0.0, 180.0);
            }
        }

        public Joint(string name, JointType jointType, Link parent, Link child)
        {
            this.Name = name;
            this.JointType = jointType;
            if (this.JointType == JointType.Revolute || this.JointType == JointType.Prismatic)
            {
                // Default values for limit that can be modified later.
                this.Limit = new Limit(1.0, 30.0, 0.0, 180.0);
            }
            this.Parent = parent;
            this.Child = child;
        }

        /// <summary>
        /// Clones the Joint object into a new object.
        /// </summary>
        /// <returns>Cloned Joint object.</returns>
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

        public void PrintJointTag(XmlTextWriter URDFWriter)
        {
            /* <joint name="..." type="...">
             *     <origin ... />
             *     <parent link="..."/>
             *     <child link="..."/>
             *     
             *     <axis xyz="# # #"/>
             *     <calibration 'type'(rising/falling)="#"/>
             *     <dynamics damping="#" friction="#"/>
             *     <limit ... />
             *     <safety_controller
             * </joint>
             */
            URDFWriter.WriteStartElement("joint");
            URDFWriter.WriteAttributeString("name", this.Name);
            URDFWriter.WriteAttributeString("type", this.JointType.Type);
            PrintOriginTag(URDFWriter);
            if (this.Parent != null)
            {
                URDFWriter.WriteStartElement("parent");
                URDFWriter.WriteAttributeString("link", this.Parent.Name);
                URDFWriter.WriteEndElement();
            }
            if (this.Child != null)
            {
                URDFWriter.WriteStartElement("child");
                URDFWriter.WriteAttributeString("link", this.Child.Name);
                URDFWriter.WriteEndElement();
            }

            if (this.Axis != null)
            {
                URDFWriter.WriteStartElement("axis");
                URDFWriter.WriteAttributeString("xyz", this.Axis[0] + " " + this.Axis[1] + " " + this.Axis[2]);
                URDFWriter.WriteEndElement();
            }

            if (this.Calibration != null)
            {
                URDFWriter.WriteStartElement("calibration");
                URDFWriter.WriteAttributeString(this.Calibration.Type, this.Calibration.Value.ToString());
                URDFWriter.WriteEndElement();
            }

            if (this.Dynamics != null)
            {
                this.Dynamics.PrintDynamicsTag(URDFWriter);
            }

            if (this.Limit != null)
            {
                this.Limit.PrintLimitTag(URDFWriter);
            }

            if (this.SafetyController != null)
            {
                this.SafetyController.PrintSafetyTag(URDFWriter);
            }

            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Limit
    {
        public double Effort { get; set; }
        public double Velocity { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }

        public Limit(double effort, double velocity, double lower, double upper)
        {
            this.Effort = effort;
            this.Velocity = velocity;
            this.Lower = lower;
            this.Upper = upper;
        }

        public void PrintLimitTag(XmlTextWriter URDFWriter)
        {
            // <limit effort="#" velocity="#" lower="#" upper="#"/>
            URDFWriter.WriteStartElement("limit");
            URDFWriter.WriteAttributeString("effort", this.Effort.ToString());
            URDFWriter.WriteAttributeString("velocity", this.Velocity.ToString());
            URDFWriter.WriteAttributeString("lower", this.Lower.ToString());
            URDFWriter.WriteAttributeString("upper", this.Upper.ToString());
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Dynamics
    {
        public double Damping { get; set; }
        public double Friction { get; set; }

        public Dynamics(double damping, double friction)
        {
            this.Damping = damping;
            this.Friction = friction;
        }

        public void PrintDynamicsTag(XmlTextWriter URDFWriter)
        {
            URDFWriter.WriteStartElement("dynamics");
            URDFWriter.WriteAttributeString("damping", this.Damping.ToString());
            URDFWriter.WriteAttributeString("friction", this.Friction.ToString());
            URDFWriter.WriteEndElement();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SafetyController
    {
        public double SoftLowerLimit { get; set; }
        public double SoftUpperLimit { get; set; }
        public double KPosition { get; set; }
        public double KVelocity { get; set; }

        public SafetyController(double softLowerLimit, double softUpperLimit, double kPosition, double kVelocity)
        {
            this.SoftLowerLimit = softLowerLimit;
            this.SoftUpperLimit = softUpperLimit;
            this.KPosition = kPosition;
            this.KVelocity = kVelocity;
        }

        public void PrintSafetyTag(XmlTextWriter URDFWriter)
        {
            URDFWriter.WriteStartElement("safety_controller");
            URDFWriter.WriteAttributeString("soft_lower_limit", this.SoftLowerLimit.ToString());
            URDFWriter.WriteAttributeString("soft_upper_limit", this.SoftUpperLimit.ToString());
            URDFWriter.WriteAttributeString("k_position", this.KPosition.ToString());
            URDFWriter.WriteAttributeString("k_velocity", this.KVelocity.ToString());
            URDFWriter.WriteEndElement();
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

        private Calibration(string type, double value)
        {
            Type = type;
            Value = value;
        }

        public string Type { get; private set; }
        public double Value { get; set; }
    }

    #endregion
}
