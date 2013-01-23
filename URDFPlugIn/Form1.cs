using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using Inventor;

using URDF;

namespace URDFPlugIn
{
    public partial class Form1 : Form
    {
        Inventor.Application _invApp;
        bool _started = false;

        public Form1()
        {
            InitializeComponent();
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

            
        }

        public void PrintAllProperties(string xmlfilename)
        {
            UnitsOfMeasure oUOM = _invApp.ActiveDocument.UnitsOfMeasure;
            AssemblyDocument oAsmDoc = (AssemblyDocument)_invApp.ActiveDocument;
            AssemblyComponentDefinition oAsmCompDef = oAsmDoc.ComponentDefinition;
            ComponentOccurrence Parent;
            string ParentName, AbsolutePosition, name, mirname, mirParentName;
            double[] ParentCOM, Offset;

            Robot hubo = new Robot("HuboPlus");

            // Iterate over all of the components and isolate the Body_* named ones
            foreach (ComponentOccurrence oCompOccur in oAsmCompDef.Occurrences)
            {
                // Generate links from available subassemblies in main assembly
                name = oCompOccur.Name;
                hubo.Links.Add(new Link(FormatName(name)));
                int c = hubo.Links.Count - 1;
                for (int i = 0; i < hubo.Links.Count; i++)
                {
                    if (String.Equals(hubo.Links[i].Name, ReturnParentName(oCompOccur)))
                        hubo.Links[c].Parent = hubo.Links[i];
                }

                // Generate joints between connected links
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

                // Get mass properties for each link
                hubo.Links[c].Intertial = new Link.Inertial();
                hubo.Links[c].Intertial.Mass = oCompOccur.MassProperties.Mass;
                hubo.Links[c].Intertial.XYZ = FindCenterOfMassOffset(oCompOccur);

                double[] iXYZ = new double[6];
                oCompOccur.MassProperties.XYZMomentsOfInertia(out iXYZ[0], out iXYZ[1], out iXYZ[2], out iXYZ[3], out iXYZ[4], out iXYZ[5]); // Ixx, Iyy, Izz, Ixy, Iyz, Ixz
                hubo.Links[c].Intertial.Inertia = new double[,] { { iXYZ[0], iXYZ[3], iXYZ[5] }, { iXYZ[3], iXYZ[1], iXYZ[4] }, { iXYZ[5], iXYZ[4], iXYZ[2] } };

                //MessageBox.Show(FindOrigin(oCompOccur)[0].ToString());

                hubo.Links[c].Geometry = new Link.Visual.Geometry(new Shape());
                hubo.Links[c].Geometry.Shape = Shape.Mesh;

                hubo.Links[c].Geometry.XYZ = FindOrigin(oCompOccur);
                
                if (hubo.Links[c].Parent != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        hubo.Links[c].Geometry.XYZ[i] -= hubo.Links[c].Parent.Geometry.XYZ[i];
                    }
                }

                // Mirror bodies
                if (hubo.Links[c].Name.IndexOf("_R") != -1)
                {
                    hubo.Links.Add((Link)hubo.Links[c].Clone());
                    int d = hubo.Links.Count - 1;
                    // Rename the mirrored body
                    hubo.Links[d].Name = "Body_L" + hubo.Links[c].Name.Substring(6, 2);

                    // Mirror the inertial origin
                    hubo.Links[d].Intertial.XYZ[1] *= -1;

                    // Mirror the moment of inertia
                    hubo.Links[d].Intertial.Inertia[0, 1] *= -1;
                    hubo.Links[d].Intertial.Inertia[1, 0] *= -1;
                    hubo.Links[d].Intertial.Inertia[2, 1] *= -1;
                    hubo.Links[d].Intertial.Inertia[1, 2] *= -1;

                    // Mirror the geometric origin
                    hubo.Links[d].Geometry.XYZ[1] *= -1;

                    // Clone the joint 
                    int j = hubo.Joints.Count - 1;
                    hubo.Joints.Add((Joint)hubo.Joints[j].Clone());
                    int k = hubo.Joints.Count - 1;
                    hubo.Joints[k].Name = "L" + hubo.Joints[k].Name.Substring(1, 2);
                    if (hubo.Joints[k].Parent.Name.IndexOf("_R") != -1)
                        hubo.Joints[k].Parent.Name = "Body_L" + hubo.Joints[k].Parent.Name.Substring(6, 2);
                    hubo.Joints[k].Child.Name = hubo.Links[d].Name;

                }
            }

            hubo.PrintURDF(xmlfilename);
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
            // Dump translation data to debug window in case you want to do relative positions
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

        private void button1_Click(object sender, EventArgs e)
        {
            PrintAllProperties("hubo.xml");
        }
    }
}
