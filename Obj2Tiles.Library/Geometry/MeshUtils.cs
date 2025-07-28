using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Obj2Tiles.Library.Materials;

namespace Obj2Tiles.Library.Geometry;

public class MeshUtils
{
    public static IMesh LoadMesh(string fileName)
    {
        return LoadMesh(fileName, out _);
    }
    
    public static IMesh LoadMesh(string fileName, out string[] dependencies)
    {
        using var reader = new StreamReader(fileName);

        var vertices = new List<Vertex3>();
        var textureVertices = new List<Vertex2>();
        var facesT = new List<FaceT>();
        var faces = new List<Face>();
        var materials = new List<Material>();
        var materialsDict = new Dictionary<string, int>();
        var currentMaterial = string.Empty;
        var deps = new List<string>();

        while (true)
        {
            var line = reader.ReadLine();

            if (line == null) break;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var segs = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (segs[0])
            {
                case "v" when segs.Length >= 4:
                    vertices.Add(new Vertex3(
                        double.Parse(segs[1], CultureInfo.InvariantCulture),
                        double.Parse(segs[2], CultureInfo.InvariantCulture),
                        double.Parse(segs[3], CultureInfo.InvariantCulture)));
                    break;
                case "vt" when segs.Length >= 3:

                    var vtx = new Vertex2(
                        double.Parse(segs[1], CultureInfo.InvariantCulture),
                        double.Parse(segs[2], CultureInfo.InvariantCulture));
                    
                    if (vtx.X < 0 || vtx.Y < 0)
                        throw new Exception("Invalid texture coordinates: " + vtx);
                    
                    textureVertices.Add(vtx);
                    break;
                case "vn" when segs.Length == 3:
                    // Skipping normals
                    break;
                case "usemtl" when segs.Length == 2:
                {
                    if (!materialsDict.ContainsKey(segs[1]))
                        throw new Exception($"Material {segs[1]} not found");

                    currentMaterial = segs[1];
                    break;
                }
                case "f" when segs.Length == 4:
                {
                    var first = segs[1].Split('/');
                    var second = segs[2].Split('/');
                    var third = segs[3].Split('/');

                    var hasTexture = first.Length > 1 && first[1].Length > 0 && second.Length > 1 &&
                                     second[1].Length > 0 && third.Length > 1 && third[1].Length > 0;

                    // We ignore this
                    // var hasNormals = vertexIndices[0][2] != null && vertexIndices[1][2] != null && vertexIndices[2][2] != null;

                    var v1 = int.Parse(first[0]);
                    var v2 = int.Parse(second[0]);
                    var v3 = int.Parse(third[0]);

                    if (hasTexture)
                    {
                        var vt1 = int.Parse(first[1]);
                        var vt2 = int.Parse(second[1]);
                        var vt3 = int.Parse(third[1]);

                        var faceT = new FaceT(
                            v1 - 1,
                            v2 - 1,
                            v3 - 1,
                            vt1 - 1,
                            vt2 - 1,
                            vt3 - 1,
                            materialsDict[currentMaterial]);

                        facesT.Add(faceT);
                    }
                    else
                    {
                        var face = new Face(
                            v1 - 1,
                            v2 - 1,
                            v3 - 1,
                            materialsDict[currentMaterial]);

                        faces.Add(face);
                    }

                    break;
                }
                case "mtllib" when segs.Length == 2:
                {
                    var mtlFileName = segs[1];
                    var mtlFilePath = Path.Combine(Path.GetDirectoryName(fileName) ?? string.Empty, mtlFileName);
                    
                    var mats = Material.ReadMtl(mtlFilePath, out var mtlDeps);

                    deps.AddRange(mtlDeps);
                    deps.Add(mtlFilePath);
                    
                    foreach (var mat in mats)
                    {
                        materials.Add(mat);
                        materialsDict.Add(mat.Name, materials.Count - 1);
                    }

                    break;
                }
                case "l" or "cstype" or "deg" or "bmat" or "step" or "curv" or "curv2" or "surf" or "parm" or "trim"
                    or "end" or "hole" or "scrv" or "sp" or "con":

                    throw new NotSupportedException("Element not supported: '" + line + "'");
            }
        }

        dependencies = deps.ToArray();
        
        return textureVertices.Count != 0
            ? new MeshT(vertices, textureVertices, facesT, materials)
            : new Mesh(vertices, faces, materials);
    }

    #region Splitters

    private static readonly IVertexUtils yutils3 = new VertexUtilsY();
    private static readonly IVertexUtils xutils3 = new VertexUtilsX();
    private static readonly IVertexUtils zutils3 = new VertexUtilsZ();

    public static async Task<int> RecurseSplitXY(IMesh mesh, double limitLength, Box3 bounds, bool splitZ, ConcurrentBag<IMesh> resultMeshes)
    {
        Console.WriteLine($"RecurseSplitXY('{mesh.Name}' {mesh.VertexCount}, {limitLength}, {bounds})");

        var center = bounds.Center;
        int count = 0;
        // 제한크기의 80~90%수준선까지는 분할을 허용함.
        double allowRatio = 1.7;
        // x축분할.
        if (bounds.Width >= limitLength * allowRatio)
        {
            count = mesh.Split(xutils3, center.X, out var left, out var right);
            var xbounds = bounds.Split(Axis.X);

            var tasks = new List<Task<int>>();
            if (left.FacesCount > 0) tasks.Add(RecurseSplitXY(left, limitLength, xbounds[0], splitZ, resultMeshes));
            if (right.FacesCount > 0) tasks.Add(RecurseSplitXY(right, limitLength, xbounds[1], splitZ, resultMeshes));
            await Task.WhenAll(tasks);
            count += tasks.Sum(t => t.Result);
        }
        // y축분할.
        else if (bounds.Height >= limitLength * allowRatio)
        {
            count = mesh.Split(yutils3, center.Y, out var left, out var right);
            var xbounds = bounds.Split(Axis.Y);

            var tasks = new List<Task<int>>();
            if (left.FacesCount > 0) tasks.Add(RecurseSplitXY(left, limitLength, xbounds[0], splitZ,resultMeshes));
            if (right.FacesCount > 0) tasks.Add(RecurseSplitXY(right, limitLength, xbounds[1], splitZ,resultMeshes));
            await Task.WhenAll(tasks);
            count += tasks.Sum(t => t.Result);
        }
        // z축분할.
        else if (splitZ && bounds.Depth >= limitLength * allowRatio)
        {
            count = mesh.Split(zutils3, center.Z, out var left, out var right);
            var xbounds = bounds.Split(Axis.Z);

            var tasks = new List<Task<int>>();
            if (left.FacesCount > 0) tasks.Add(RecurseSplitXY(left, limitLength, xbounds[0], splitZ, resultMeshes));
            if (right.FacesCount > 0) tasks.Add(RecurseSplitXY(right, limitLength, xbounds[1], splitZ, resultMeshes));
            await Task.WhenAll(tasks);
            count += tasks.Sum(t => t.Result);
        }
        // 더이상 분할할것이 없으면.
        else
        {
            if (mesh.FacesCount > 0)
                resultMeshes.Add(mesh);
            return 0;
        }

        return count;
    }
    #endregion
}