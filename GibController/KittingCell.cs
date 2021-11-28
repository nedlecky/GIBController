using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GibController
{
    public class CellContents
    {
        public List<Part> parts = new List<Part>();
        public List<Slot> slots = new List<Slot>();
        public List<Box> boxes = new List<Box>();
        public List<ProductType> productTypes = new List<ProductType>();
        public List<BoxType> boxTypes = new List<BoxType>();
        public enum KittingZone
        {
            KittingOut = 1,
            KittingIn = 2,
            VFX = 3
        }
        public enum Contents
        {
            FOD = -1,
            Empty = 0,
            Part = 1,
        }

        public CellContents()
        {
            InitProductTypes();
            InitBoxTypes();
            InitBoxes();
        }

        public void InitProductTypes()
        {
            ProductType thisProductType = new ProductType();
            productTypes.Add(new ProductType
            {
                ID = 0,
                Name = "HDD",
                CameraMode = 7,
                ToolID = 1
            });
            productTypes.Last<ProductType>().CompatibleBoxTypes.Add(0);
            productTypes.Last<ProductType>().CompatibleBoxTypes.Add(1);

            thisProductType = new ProductType();
            productTypes.Add(new ProductType
            {
                ID = 1,
                Name = "HDD",
                CameraMode = 7,
                ToolID = 2
            });
            productTypes.Last<ProductType>().CompatibleBoxTypes.Add(0);
            productTypes.Last<ProductType>().CompatibleBoxTypes.Add(1);
        }
        public class ProductType : Object
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int CameraMode { get; set; }
            public int ToolID { get; set; }
            public List<int> CompatibleBoxTypes { get; set; } = new List<int>();
        }

        public void InitBoxTypes()
        {
            boxTypes.Add(new BoxType
            {
                ID = 0,
                Name = "tote",
                ProductType = productTypes.FirstOrDefault(o => o.Name == "HDD"),
                RowPitch = 42000,
                ColPitch = 128000,
                NumRows = 12,
                NumCols = 2,
                EmptyDistance = 24000,
                ProductDistance = 16500,
                Fiducial1World = new Position
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                Fiducial2Frame = new Position
                {
                    X = 580000,
                    Y = 0,
                    Z = -220000,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                Slot00Frame = new Position
                {
                    X = 50647,
                    Y = 60054,
                    Z = -305000,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                FiducialCameraMode = 1
            });

            boxTypes.Add(new BoxType
            {
                ID = 1,
                Name = "carton1",
                ProductType = productTypes.FirstOrDefault(o => o.Name == "HDD"),
                RowPitch = 47444,
                ColPitch = 125000,
                NumRows = 10,
                NumCols = 2,
                EmptyDistance = 24000,
                ProductDistance = 16500,
                Fiducial1World = new Position
                {
                    X = 100000,
                    Y = 40000,
                    Z = 0,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                Fiducial2Frame = new Position
                {
                    X = 377000,
                    Y = 0,
                    Z = -220000,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                Slot00Frame = new Position
                {
                    X = -27033,
                    Y = 59886,
                    Z = -300000,
                    W = 0,
                    P = 0,
                    R = 0,
                },
                FiducialCameraMode = 2
            });
        }
        public class BoxType : Object
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public ProductType ProductType { get; set; }
            public int RowPitch { get; set; }
            public int ColPitch { get; set; }
            public int NumRows { get; set; }
            public int NumCols { get; set; }
            public Position Fiducial1World { get; set; }
            public Position Fiducial2Frame { get; set; }
            public Position Slot00Frame { get; set; }
            public int FiducialCameraMode { get; set; }
            public int EmptyDistance { get; set; }
            public int ProductDistance { get; set; }

        }

        public void InitBoxes()
        {
            new Box(boxTypes.FirstOrDefault(o => o.Name == "tote"), boxes, slots)
            {
                ConveyorID = 1,
                Zone = KittingZone.KittingIn,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "tote"), boxes, slots)
            {
                ConveyorID = 2,
                Zone = KittingZone.KittingIn,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "tote"), boxes, slots)
            {
                ConveyorID = 3,
                Zone = KittingZone.KittingIn,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "carton1"), boxes, slots)
            {
                ConveyorID = 4,
                Zone = KittingZone.KittingOut,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "carton1"), boxes, slots)
            {
                ConveyorID = 5,
                Zone = KittingZone.KittingOut,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "carton1"), boxes, slots)
            {
                ConveyorID = 6,
                Zone = KittingZone.KittingOut,
                IsLocated = false
            };
            new Box(boxTypes.FirstOrDefault(o => o.Name == "tote"), boxes, slots)
            {
                ConveyorID = 7,
                Zone = KittingZone.VFX,
                IsLocated = false
            };
        }

        public class Box : Object
        {
            public BoxType Type { get; set; }
            public int ConveyorID { get; set; }
            public string DunnageID { get; set; }

            public KittingZone Zone { get; set; }

            public bool IsLocated { get; set; } = false;
            public bool IsInitialized { get; set; } = false;

            public Box(BoxType boxType, List<Box> boxes, List<Slot> slots)
            {
                this.Type = boxType;
                for (int c = 0; c < this.Type.NumCols; c++)
                {
                    for (int r = 0; r < this.Type.NumRows; r++)
                    {
                        Slot slot = new Slot
                        {
                            Box = this,
                            RowNum = r,
                            ColNum = c,
                            IsChecked = false,
                        };
                        slots.Add(slot);
                    }
                }
                boxes.Add(this);
            }
        }

        public void EjectBox(Box box)
        {
            List<Part> boxParts = parts.Where(o => o.Slot.Box == box).ToList();
            List<Slot> boxSlots = slots.Where(o => o.Box == box).ToList();

            parts.RemoveAll(o => o.Slot.Box == box);
            slots.RemoveAll(o => o.Box == box);
            boxes.RemoveAll(o => o == box);

            for (int i = 0; i < boxParts.Count; i++) boxParts[i] = null;
            for (int i = 0; i < boxSlots.Count; i++) boxSlots[i] = null;
            box = null;
        }

        public class Slot : Object
        {
            public int ID { get; set; } 
            public Box Box { get; set; }
            public int RowNum { get; set; }
            public int ColNum { get; set; }
            public Contents Contains { get; set; }
            public float LaserDistance { get; set; }
            public bool IsChecked { get; set; }

        }

        public struct Position
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int W { get; set; }
            public int P { get; set; }
            public int R { get; set; }
        }

        public class Part : Object
        {
            public string Barcode { get; set; }
            public float Orientation { get; set; }
            public ProductType ProductType { get; set; }
            public Slot Slot { get; set; }
            public string Barcode2 { get; internal set; }
        }


        
    }
}
