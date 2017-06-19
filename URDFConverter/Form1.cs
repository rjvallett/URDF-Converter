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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using URDF;
using Inventor;

namespace URDFConverter
{
    public partial class Form1 : Form
    {
        Inventor.Application _invApp;
        bool _started = false;

        public Form1()
        {
            InitializeComponent();

            #region Get Inventor session
            try
            {
                _invApp = (Inventor.Application)Marshal.GetActiveObject("Inventor.Application");
            }
            catch (Exception ex)
            {
                try
                {
                    Type invAppType = Type.GetTypeFromProgID("Inventor.Application");

                    _invApp = (Inventor.Application)System.Activator.CreateInstance(invAppType);
                    _invApp.Visible = true;

                    /* Note: if the Inventor session is left running after this
                     * form is closed, there will still an be and Inventor.exe 
                     * running. We will use this Boolean to test in Form1.Designer.cs 
                     * in the dispose method whether or not the Inventor App should
                     * be shut down when the form is closed.
                     */
                    _started = true;

                }
                catch (Exception ex2)
                {
                    MessageBox.Show(ex2.ToString());
                    MessageBox.Show("Unable to get or start Inventor");
                }
            }

            #endregion

            #region Test code
            /*
            // Define a new Robot, robot, with the name "HuboPlus"
            Robot robot = new Robot("HuboPlus");
            
            // Define a new Link, link1, with the name "link1".
            Link link1 = new Link("link1");
            
            // Set the Visual attributes, geometric and material, of the link.
            link1.Visual = new Visual(new Mesh("package://link1.stl"), 
                new URDF.Material("Red", new double[] { 255, 0, 0, 1.0 }));

            // Set the Collision attributes of the link.
            link1.Collision = new Collision(new URDF.Cylinder(1, 2));

            // Set the Inertial attributes of the link.
            link1.Inertial = new Inertial(5, new double[] { 1, 0, 0, 1, 0, 1 });

            // Add the link to the list of links within the robot.
            robot.Links.Add(link1);

            // Make a clone of link1 and add it to the robot model.
            robot.Links.Add((Link)link1.Clone());


            // Define a new Joint, joint1, with the name "joint1".
            Joint joint1 = new Joint("joint1", JointType.Prismatic, link1, link1);

            robot.Joints.Add(joint1);

            robot.Joints.Add((Joint)joint1.Clone());

            robot.WriteURDFToFile("hubo.xml");
            */

            #endregion

            WriteURDF("HuboPlus.xml");

        }

        public void WriteURDF(string xmlfilename)
        {
            UnitsOfMeasure oUOM = _invApp.ActiveDocument.UnitsOfMeasure;
            AssemblyDocument oAsmDoc = (AssemblyDocument)_invApp.ActiveDocument;
            AssemblyComponentDefinition oAsmCompDef = oAsmDoc.ComponentDefinition;
            ComponentOccurrence Parent;
            string ParentName, AbsolutePosition, name, mirname, mirParentName;
            double[] ParentCOM, Offset;

            Robot hubo = new Robot("HuboPlus");

            foreach (ComponentOccurrence oCompOccur in oAsmCompDef.Occurrences)
            {
                // Generate links from available subassemblies in main assembly.
                hubo.Links.Add(new Link(oCompOccur.Name));
                int c = hubo.Links.Count - 1;
                for (int i = 0; i < hubo.Links.Count; i++)
                {
                    if (String.Equals(hubo.Links[i].Name, ReturnParentName(oCompOccur)))
                        hubo.Links[c].Parent = hubo.Links[i];
                }

                if (hubo.Links[c].Parent != null)
                {
                    hubo.Joints.Add(new Joint(FormatJointName(hubo.Links[c].Name), JointType.Revolute, hubo.Links[c].Parent, hubo.Links[c]));
                    int j = hubo.Joints.Count - 1;
                    switch (hubo.Joints[j].Name[hubo.Joints[j].Name.Length - 1])
                    {
                        case 'R':
                            hubo.Joints[j].Axis = new double[] { 1, 0, 0 };
                            break;
                        case 'P':
                            hubo.Joints[j].Axis = new double[] { 0, 1, 0 };
                            break;
                        case 'Y':
                            hubo.Joints[j].Axis = new double[] { 0, 0, 1 };
                            break;
                        default:
                            break;
                    }
                }

                // Get mass properties for each link.
                double[] iXYZ = new double[6];
                oCompOccur.MassProperties.XYZMomentsOfInertia(out iXYZ[0], out iXYZ[3], out iXYZ[5], out iXYZ[1], out iXYZ[4], out iXYZ[2]); // Ixx, Iyy, Izz, Ixy, Iyz, Ixz -> Ixx, Ixy, Ixz, Iyy, Iyz, Izz
                hubo.Links[c].Inertial = new Inertial(oCompOccur.MassProperties.Mass, iXYZ);
                hubo.Links[c].Inertial.XYZ = FindCenterOfMassOffset(oCompOccur);

                // Set shape properties for each link.
                hubo.Links[c].Visual = new Visual(new Mesh("package://" + hubo.Name + "/" + hubo.Links[c].Name + ".stl"));
            }


        }

        public double[] ComputeRelativeOffset(ComponentOccurrence Child, ComponentOccurrence Parent)
        {
            double[] c1 = FindOrigin(Parent);
            double[] c2 = FindOrigin(Child);
            double[] c3 = new double[3];

            for (int k = 0; k < 3; k++)
            {
                c3[k] = c2[k] - c1[k];
            }

            return c3;
        }

        public double[] FindOrigin(ComponentOccurrence oCompOccur)
        {
            UnitsOfMeasure oUOM = _invApp.ActiveDocument.UnitsOfMeasure;
            AssemblyComponentDefinition oCompDef = (AssemblyComponentDefinition)oCompOccur.Definition;
            object oWorkPointProxy;
            double[] c = new double[3];
            WorkPoint oWP = oCompDef.WorkPoints[1];
            oCompOccur.CreateGeometryProxy(oWP, out oWorkPointProxy);

            c[0] = ((WorkPointProxy)oWorkPointProxy).Point.X;
            c[1] = ((WorkPointProxy)oWorkPointProxy).Point.Y;
            c[2] = ((WorkPointProxy)oWorkPointProxy).Point.Z;

            for (int k = 0; k < 3; k++)
            {
                c[k] = oUOM.ConvertUnits(c[k], "cm", "m");
            }

            string AbsolutePosition, name;
            name = FormatName(oCompOccur.Name);

            return c;
        }

        public int CheckBody(string strData)
        {
            // Match Bodies to actually export based on naming convention
            MatchCollection REMatches = Regex.Matches(strData, "^Body_", RegexOptions.IgnoreCase);

            return REMatches.Count;
        }

        public double[] FindCenterOfMassOffset(ComponentOccurrence oDoc)
        {
            // Store temporary variables and names
            MassProperties oMassProps = oDoc.MassProperties;
            double[] c = new double[3];

            c[0] = oMassProps.CenterOfMass.X;
            c[1] = oMassProps.CenterOfMass.Y;
            c[2] = oMassProps.CenterOfMass.Z;

            UnitsOfMeasure oUOM = _invApp.ActiveDocument.UnitsOfMeasure;

            for (int k = 0; k < 3; k++)
            {
                c[k] = oUOM.ConvertUnits(c[k], "cm", "m");
            }

            return c;
        }

        public string ReturnParentName(ComponentOccurrence occur)
        {
            try
            {
                return occur.Definition.Document.PropertySets.Item("Inventor User Defined Properties").Item("Parent").Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public string FormatName(string strData)
        {
            // Match Bodies to actually export based on naming convention
            string res = strData;

            try
            {
                res = res.Split(':')[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return res;
        }

        public string FormatJointName(string strData)
        {
            // Match Bodies to actually export based on naming convention
            int Count;

            Match REMatches = Regex.Match(strData, "[LRTH][HSKAEWNRPYBD][RPY]", RegexOptions.IgnoreCase);

            Count = REMatches.Length;

            return REMatches.Value;
        }

        public ComponentOccurrence FindComponentOccurrence(ComponentOccurrences Comp, string name)
        {
            foreach (ComponentOccurrence occur in Comp)
            {
                if (occur.Name.IndexOf(name) >= 0)
                {
                    return occur;
                }
            }
            return null;
        }



    }
}
