﻿using System;
using System.Collections.Generic;
using System.IO;
using ToxicRagers.Helpers;
using ToxicRagers.Carmageddon2.Helpers;

namespace ToxicRagers.Carmageddon2.Formats
{
    public class c2Dat
    {
        public List<DatMesh> DatMeshes;

        public c2Dat()
        {
            DatMeshes = new List<DatMesh>();
        }

        public c2Dat(DatMesh dm)
        {
            DatMeshes = new List<DatMesh>();
            DatMeshes.Add(dm);
        }

        public bool Load(string Path)
        {
            int Length, Count;
            DatMesh D = new DatMesh();
            bool bSuccess = true;

            BEBinaryReader br = new BEBinaryReader(new FileStream(Path, FileMode.Open));
            br.ReadBytes(16); // Header

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                int Tag = (int)br.ReadUInt32();

                switch (Tag)
                {
                    case 54: // 00 00 00 36
                        D = new DatMesh();
                        // Name
                        Length = (int)br.ReadUInt32() - 2;
                        D.UnknownAttribute = br.ReadUInt16();
                        D.Name = br.ReadStringOfLength(Length);
                        Console.WriteLine("{0}", D.Name);
                        break;

                    case 23: // 00 00 00 17
                        // vertex data
                        Length = (int)br.ReadUInt32();
                        Count = (int)br.ReadUInt32();
                        Console.WriteLine("V: {0}", Count);
                        for (int i = 0; i < Count; i++)
                        {
                            Single x, y, z;
                            x = br.ReadSingle(); y = br.ReadSingle(); z = br.ReadSingle();
                            D.Mesh.AddListVertex(x, y, z);
                            Console.WriteLine("x :\t{0:R}\ty :\t{1:R}\tz :\t{2:R}", x, y, z);
                        }
                        break;

                    case 24: // 00 00 00 18
                        // UV co-ordinates
                        Length = (int)br.ReadUInt32();
                        Count = (int)br.ReadUInt32();
                        Console.WriteLine("UV: {0}", Count);
                        for (int i = 0; i < Count; i++)
                        {
                            Single u, v;
                            u = br.ReadSingle(); v = br.ReadSingle();
                            D.Mesh.AddListUV(u, v);
                            //Console.WriteLine("u :\t{0:R}\tv :\t{1:R}", u, v);
                        }
                        break;

                    case 53:    // 00 00 00 35
                        // Faces
                        Length = (int)br.ReadUInt32();
                        Count = (int)br.ReadUInt32();
                        Console.WriteLine("F: {0}", Count);

                        for (int i = 0; i < Count; i++)
                        {
                            UInt16 a, b, c;
                            a = br.ReadUInt16(); b = br.ReadUInt16(); c = br.ReadUInt16();
                            D.Mesh.AddFace(a, b, c);
                            br.ReadByte(); // smoothing groups 9 - 16
                            br.ReadByte(); // smoothing groups 1 - 8
                            br.ReadByte(); // number of edges, 0 and 3 = tri.  4 = quad.
                            //Console.WriteLine("a :\t{0}\tb :\t{1}\tc :\t{2}", a, b, c);
                            Console.WriteLine("model->faces[{0}].vertices[0] = {1};", i, a);
                            Console.WriteLine("model->faces[{0}].vertices[1] = {1};", i, b);
                            Console.WriteLine("model->faces[{0}].vertices[2] = {1};", i, c);
                        }

                        break;

                    case 22: // 00 00 00 16
                        // material list
                        Length = (int)br.ReadUInt32();
                        Count = (int)br.ReadUInt32();
                        Console.WriteLine("M: {0}", Count);

                        string[] Materials = br.ReadStrings(Count);
                        for (int i = 0; i < Count; i++)
                        {
                            D.Mesh.Materials.Add(Materials[i]);
                        }
                        break;

                    case 26:
                        // face textures
                        Length = (int)br.ReadUInt32();
                        Count = (int)br.ReadUInt32();
                        br.ReadBytes(4); // fuck knows what this is
                        for (int i = 0; i < Count; i++)
                        {
                            D.Mesh.SetMaterialForFace(i, br.ReadUInt16() - 1);
                        }
                        break;

                    case 0:
                        // EndOfFile
                        D.Mesh.ProcessMesh();
                        DatMeshes.Add(D);
                        br.ReadUInt32();
                        break;

                    default:
                        Console.WriteLine("Unknown DAT tag: " + Tag + " (" + br.BaseStream.Position + " :: " + br.BaseStream.Length + ")");
                        br.BaseStream.Position = br.BaseStream.Length;
                        bSuccess = false;
                        break;
                }
            }

            br.Close();
            return bSuccess;
        }

        public void Save(string Path)
        {
            BEBinaryWriter bw = new BEBinaryWriter(new FileStream(Path, FileMode.Create));
            int iMatListLength;
            string name;

            //output header
            bw.WriteInt32(18);
            bw.WriteInt32(8);
            bw.WriteInt32(64206);
            bw.WriteInt32(2);

            for (int i = 0; i < DatMeshes.Count; i++)
            {
                DatMesh dm = DatMeshes[i];
                iMatListLength = 0;

                for (int j = 0; j < dm.Mesh.Materials.Count; j++)
                {
                    iMatListLength += dm.Mesh.Materials[j].Length + 1;
                }

                name = dm.Name;
                //Console.WriteLine(name + " : " + dm.Mesh.Verts.Count);

                //begin name section
                bw.WriteInt32(54);
                bw.WriteInt32(name.Length + 3);
                bw.WriteByte(0);
                bw.WriteByte(0);
                bw.Write(name.ToCharArray());
                bw.WriteByte(0);
                //end name section

                //begin vertex data
                bw.WriteInt32(23);
                bw.WriteInt32((dm.Mesh.Verts.Count * 12) + 4);
                bw.WriteInt32(dm.Mesh.Verts.Count);

                for (int j = 0; j < dm.Mesh.Verts.Count; j++)
                {
                    bw.WriteSingle(dm.Mesh.Verts[j].X);
                    bw.WriteSingle(dm.Mesh.Verts[j].Y);
                    bw.WriteSingle(dm.Mesh.Verts[j].Z);
                }
                //end vertex data

                //begin uv data (00 00 00 18)
                bw.WriteInt32(24);
                bw.WriteInt32((dm.Mesh.UVs.Count * 8) + 4);
                bw.WriteInt32(dm.Mesh.UVs.Count);

                for (int j = 0; j < dm.Mesh.UVs.Count; j++)
                {
                    bw.WriteSingle(dm.Mesh.UVs[j].X);
                    bw.WriteSingle(dm.Mesh.UVs[j].Y);
                }
                //end uv data

                //begin face data (00 00 00 35)
                bw.WriteInt32(53);
                bw.WriteInt32((dm.Mesh.Faces.Count * 9) + 4);
                bw.WriteInt32(dm.Mesh.Faces.Count);

                for (int j = 0; j < dm.Mesh.Faces.Count; j++)
                {
                    bw.WriteInt16(dm.Mesh.Faces[j].V1);
                    bw.WriteInt16(dm.Mesh.Faces[j].V2);
                    bw.WriteInt16(dm.Mesh.Faces[j].V3);
                    bw.WriteByte(0); // smoothing groups 9 - 16
                    bw.WriteByte(1);   // smoothing groups 1 - 8
                    bw.WriteByte(0);   // number of edges, 0 and 3 = tri.  4 = quad.
                }
                //end face data

                //begin material list
                bw.WriteInt32(22);
                bw.WriteInt32(iMatListLength + 4);
                bw.WriteInt32(dm.Mesh.Materials.Count);

                for (int j = 0; j < dm.Mesh.Materials.Count; j++)
                {
                    bw.Write(dm.Mesh.Materials[j].ToCharArray());
                    bw.WriteByte(0);
                }
                //end material list

                //begin face textures
                bw.WriteInt32(26);
                bw.WriteInt32((dm.Mesh.Faces.Count * 2) + 4);
                bw.WriteInt32(dm.Mesh.Faces.Count);
                bw.WriteInt32(2);

                for (int j = 0; j < dm.Mesh.Faces.Count; j++)
                {
                    bw.WriteInt16(dm.Mesh.Faces[j].MaterialID + 1);
                }
                //end face textures

                bw.WriteInt32(0);
                bw.WriteInt32(0);
            }

            bw.Close();
        }

        public void AddMesh(string Name, byte Flag, c2Mesh Mesh)
        {
            DatMesh d = new DatMesh();
            d.Name = Name;
            d.UnknownAttribute = Flag;
            d.Mesh = Mesh;
            DatMeshes.Add(d);
        }

        // aggregate functions, apply to all submeshes
        #region Aggregate functions
        public MeshExtents Extents
        {
            get
            {
                Vector3 min, max;
                min = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
                max = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);

                foreach (DatMesh d in DatMeshes)
                {
                    d.Mesh.ProcessMesh();

                    if (min.X > d.Mesh.Extents.Min.X) { min.X = d.Mesh.Extents.Min.X; }
                    if (min.Y > d.Mesh.Extents.Min.Y) { min.Y = d.Mesh.Extents.Min.Y; }
                    if (min.Z > d.Mesh.Extents.Min.Z) { min.Z = d.Mesh.Extents.Min.Z; }

                    if (max.X < d.Mesh.Extents.Max.X) { max.X = d.Mesh.Extents.Max.X; }
                    if (max.Y < d.Mesh.Extents.Max.Y) { max.Y = d.Mesh.Extents.Max.Y; }
                    if (max.Z < d.Mesh.Extents.Max.Z) { max.Z = d.Mesh.Extents.Max.Z; }
                }

                return new MeshExtents(min, max);
            }
        }

        public void Optimise()
        {
            foreach (DatMesh d in DatMeshes)
            {
                d.Mesh.Optimise();
            }
        }

        public void CentreOn(Single x, Single y, Single z)
        {
            MeshExtents extents = Extents;
            Vector3 offset = (extents.Min + extents.Max) / 2;

            Console.WriteLine(offset);

            foreach (DatMesh d in DatMeshes)
            {
                d.Mesh.Translate(-offset);
                d.Mesh.ProcessMesh();
            }
        }

        public void Scale(Single scale)
        {
            foreach (DatMesh d in DatMeshes)
            {
                d.Mesh.Scale(scale);
                d.Mesh.ProcessMesh();
            }
        }
        #endregion
    }

    public class DatMesh
    {
        #region Variables
        private string _name = "";
        private int _attribUnknown = 0;
        private c2Mesh _mesh = new c2Mesh();
        #endregion

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int UnknownAttribute
        {
            get { return _attribUnknown; }
            set { _attribUnknown = value; }
        }

        public c2Mesh Mesh
        {
            get { return _mesh; }
            set { _mesh = value; }
        }

        public DatMesh() { }

        public DatMesh(string Name, c2Mesh m)
        {
            _name = Name;
            _mesh = m;
        }
    }
}