using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace Beam_Section_Plugin__01
{
    public class SectionInfo
    {
        public string Ten_MC { get; set; }
        public double Moment { get; set; }
        public double b { get; set; }
        public double h { get; set; }
        public double SL_CT { get; set; }
        public double DK_CT { get; set; }
        public double SL_BIEN1 { get; set; }
        public double DK_BIEN1 { get; set; }
        public double SL_GIUA1 { get; set; }
        public double DK_GIUA1 { get; set; }
        public double SL_BIEN2 { get; set; }
        public double DK_BIEN2 { get; set; }
        public double SL_GIUA2 { get; set; }
        public double DK_GIUA2 { get; set; }
        public string info_thepdai { get; set; }
    }
    public class Class1
    {
        [CommandMethod("TTNBSC")]
        public static void BeamSection()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;

            // Sử dụng PromptString để nhận giá trị từ người dùng
            PromptResult result = editor.GetString("\nEnter a string: ");

            string filePath = result.StringResult;
            if (result.Status == PromptStatus.OK)
            {
                editor.WriteMessage("\nYou entered: " + filePath);
            }
            else
            {
                editor.WriteMessage("\nInvalid input or user canceled.");
            }
            string input = ReadCSVFile(filePath);
            //string input = "Ten_MC Moment b h SL_CT DK_CT SL_BIEN1 DK_BIEN1 SL_GIUA1 DK_GIUA1 SL_BIEN2 DK_BIEN2 SL_GIUA2 DK_GIUA2 info_thepdai\nMC1_1 -250 250 450 2 20 2 20 2 20 2 20 2 20 Ø8a200\nMC2_2 -121 300 500 2 20 2 20 2 20 2 20 2 20 Ø8a200\nMC3_3 215 300 600 2 20 2 20 2 20 2 20 2 20 Ø8a200\nMC4_4 312 250 600 2 20 2 20 2 20 2 20 2 20 Ø10a150\nMC5_5 125 200 400 2 20 2 20 2 20 2 20 2 20 Ø10a150\nMC6_6 -231 200 350 2 20 2 20 2 20 2 20 2 20 Ø8a150";
            //string input = "Ten_MC,Moment,b,h,SL_CT,DK_CT,SL_BIEN1,DK_BIEN1,SL_GIUA1,DK_GIUA1,SL_BIEN2,DK_BIEN2,SL_GIUA2,DK_GIUA2,info_thepdai\nMC 1_1,-250,250,450,2,16,2,22,2,14,2,22,2,14,?8a200\nMC 2-2,-121,300,500,2,16,2,25,2,16,2,28,2,14,?8a200\nMC 3-3,215,300,600,2,16,2,28,2,14,2,28,2,16,?8a200\nMC 4-4,312,250,600,2,16,2,32,2,14,0,22,2,16,?10a150\nMC 5-5,125,200,400,2,16,2,36,2,20,0,32,2,18,?10a150\nMC 6-6,-231,200,350,2,16,2,18,2,22,0,32,2,16,?8a150";

            List<SectionInfo> sectionInfos = new List<SectionInfo>();

            string[] lines = input.Split('\n');
            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                SectionInfo sectionInfo = new SectionInfo();

                for (int j = 0; j < headers.Length; j++)
                {
                    var prop = typeof(SectionInfo).GetProperty(headers[j]);
                    if (prop != null)
                    {
                        if (j == 0)
                        {
                            prop.SetValue(sectionInfo, values[j]);
                        }
                        else if (j == headers.Length - 1)
                        {
                            prop.SetValue(sectionInfo, values[j]);
                        }
                        else
                        {
                            prop.SetValue(sectionInfo, double.Parse(values[j]));
                        }
                    }
                }
                sectionInfos.Add(sectionInfo);
            }
            int a = 0;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Tạo layer thép
                ObjectId steelLayerId = CreateLayer("Thep", 1, 5);
                foreach (SectionInfo section in sectionInfos)
                {
                    double width = section.b; // Use 'b' from SectionInfo
                    double height = section.h; // Use 'h' from SectionInfo

                    Polyline acPoly = CreateRectanglePolygon(new Point2d(a, 0), width, height);

                    Draw_Distributed_Layer(new Point3d(a, 0, 0), section, steelLayerId);
                    Draw_Rebar_Layers(new Point3d(a, 0, 0), section, steelLayerId);

                    // Add the new object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acPoly);
                    acTrans.AddNewlyCreatedDBObject(acPoly, true);
                    a = a + 500;
                }

                // Save the new objects to the database
                acTrans.Commit();
            }
        }

        // Hàm tạo một Layer
        public static ObjectId CreateLayer(string layerName, short a, int lineWeight)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
                return ObjectId.Null;

            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
                LineWeight[] lineWeights = (LineWeight[])Enum.GetValues(typeof(LineWeight));

                if (!layerTable.Has(layerName))
                {
                    LayerTableRecord layer = new LayerTableRecord();
                    layer.Name = layerName;
                    layer.Color = Color.FromColorIndex(ColorMethod.ByAci, a);
                    layer.LineWeight = lineWeights[lineWeight];

                    layerTable.UpgradeOpen();
                    ObjectId layerId = layerTable.Add(layer);
                    acTrans.AddNewlyCreatedDBObject(layer, true);

                    acTrans.Commit();
                    return layerId;
                }
                else
                {
                    acTrans.Abort();
                    return ObjectId.Null;
                }
            }
        }

        // Đọc file
        public static string ReadCSVFile(string filePath)
        {
            // Đọc nội dung từ tệp CSV
            string csvData = File.ReadAllText(filePath);

            // Loại bỏ các ký tự '\r'
            csvData = csvData.Replace("\r", "");

            return csvData;
        }

        // Hàm vẽ một hình chữ nhật
        private static Polyline CreateRectanglePolygon(Point2d FirstPoint, double width, double length)
        {
            Polyline poly = new Polyline();
            poly.AddVertexAt(0, FirstPoint, 0, 0, 0);
            poly.AddVertexAt(1, new Point2d(FirstPoint.X + width, FirstPoint.Y), 0, 0, 0);
            poly.AddVertexAt(2, new Point2d(FirstPoint.X + width, FirstPoint.Y + length), 0, 0, 0);
            poly.AddVertexAt(3, new Point2d(FirstPoint.X, FirstPoint.Y + length), 0, 0, 0);
            poly.AddVertexAt(4, FirstPoint, 0, 0, 0);
            poly.Closed = true;

            return poly;
        }

        // Hàm vẽ hình tròn
        public static void DrawCircle(Point3d center, double radius, ObjectId layerId)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                using (Circle acCirc = new Circle())
                {
                    acCirc.Center = center;
                    acCirc.Radius = radius;

                    acCirc.LayerId = layerId; // Gán layer cho hình tròn

                    acBlkTblRec.AppendEntity(acCirc);
                    acTrans.AddNewlyCreatedDBObject(acCirc, true);
                    // Adds the circle to an object id array
                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                    acObjIdColl.Add(acCirc.ObjectId);

                    // Create the hatch object and append it to the block table record
                    Hatch acHatch = new Hatch();
                    acBlkTblRec.AppendEntity(acHatch);
                    acTrans.AddNewlyCreatedDBObject(acHatch, true);

                    // Set the properties of the hatch object
                    // Associative must be set after the hatch object is appended to the 
                    // block table record and before AppendLoop
                    acHatch.SetDatabaseDefaults();
                    acHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                    acHatch.Associative = true;
                    acHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                    acHatch.EvaluateHatch(true);
                    acHatch.LayerId = layerId;
                }

                acTrans.Commit();
            }
        }


        // Hàm vẽ thép lớp rải đều
        public static void Draw_Distributed_Layer(Point3d Bot_Left_Point, SectionInfo beam, ObjectId layerId)
        {
            double r = beam.DK_CT / 2;
            double h = beam.h;
            double b = beam.b;
            double n = beam.SL_CT;
            Point3d FirstCenter = new Point3d();

            // Định nghĩa lớp bê tông bảo vệ
            double bv = 25;
            if (beam.Moment<0)
            {
                FirstCenter = new Point3d(Bot_Left_Point.X + bv + r, Bot_Left_Point.Y + bv + r, 0);
            }
            else
            {
                FirstCenter = new Point3d(Bot_Left_Point.X + bv + r, Bot_Left_Point.Y + h - bv - r, 0);
            }
            DrawCircle(FirstCenter, r, layerId);
            Point3d NextPoint = new Point3d(FirstCenter.X + (b - 2 * bv - 2 * r) / (n - 1), FirstCenter.Y, 0);
            for (int i = 1; i < n; i++)
            {
                DrawCircle(NextPoint, r, layerId);
                NextPoint = new Point3d(NextPoint.X + (b - 2 * bv - 2 * r) / (n - 1), NextPoint.Y, 0);
            }
        }

        // Hàm vẽ lớp thép có 2 đường kính
        public static void Draw_Rebar_Layers(Point3d Bot_Left_Point, SectionInfo beam, ObjectId layerId)
        {
            double bv = 25;
            double n1 = beam.SL_BIEN1 + beam.SL_GIUA1;
            double n2 = beam.SL_BIEN2 + beam.SL_GIUA2;
            double kc1 = (beam.b - (2 * bv + beam.SL_GIUA1 * beam.DK_GIUA1 + beam.SL_BIEN1 * beam.DK_BIEN1)) / (n1 - 1);
            double kc2 = (beam.b - (2 * bv + beam.SL_GIUA2 * beam.DK_GIUA2 + beam.SL_BIEN2 * beam.DK_BIEN2)) / (n2 - 1);
            double e = (beam.Moment < 0) ? 1 : 0;
            double f = (beam.Moment < 0) ? -1 : 1;

            double ra = beam.DK_BIEN1 / 2;
            double rb = beam.DK_GIUA1 / 2;
            Point3d FirstCenter1 = new Point3d(Bot_Left_Point.X + bv + ra, Bot_Left_Point.Y + e*beam.h + f*(bv + ra), 0);
            DrawCircle(FirstCenter1, ra, layerId);
            Point3d NextPoint = new Point3d(FirstCenter1.X + kc1 + ra + rb , FirstCenter1.Y, 0);
            for (int i = 1; i < n1; i++)
            {
                if (i == n1-1)
                {
                    rb = beam.DK_BIEN1 / 2;
                }
                else
                {
                    rb = beam.DK_GIUA1 / 2;
                }
                DrawCircle(NextPoint, rb, layerId);
                NextPoint = new Point3d(NextPoint.X + kc1 + ra + rb, NextPoint.Y, 0);
                ra = rb;
            }
            ra = beam.DK_BIEN2 / 2;
            rb = beam.DK_GIUA2 / 2;
            Point3d FirstCenter2 = new Point3d(Bot_Left_Point.X + bv + ra, Bot_Left_Point.Y + e*beam.h + f*(bv*2 + beam.DK_BIEN1 + ra), 0);
            DrawCircle(FirstCenter2, ra, layerId);
            NextPoint = new Point3d(FirstCenter2.X + kc2 + ra + rb, FirstCenter2.Y, 0);
            for (int i = 1; i < n2; i++)
            {
                if (i == n2-1)
                {
                    rb = beam.DK_BIEN2 / 2;
                }
                else
                {
                    rb = beam.DK_GIUA2 / 2;
                }
                DrawCircle(NextPoint, rb, layerId);
                NextPoint = new Point3d(NextPoint.X + kc2 + ra + rb, NextPoint.Y, 0);
                ra = rb;
            }
        }
        private static void BeamSection01( Point2d firstPoint, SectionInfo beam)
        {
            CreateRectanglePolygon(firstPoint, beam.b, beam.h);
        }
    }
}