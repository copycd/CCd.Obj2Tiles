using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Obj2Tiles.Library.Geometry;

namespace Obj2Tiles.Stages;

public static partial class StagesFacade
{
    public static async Task<Dictionary<string, Box3>[]> Split(string[] sourceLODFiles, string destFolder, double limitLength,
        bool zsplit, Box3 bounds, bool keepOriginalTextures = false)
    {
      
        var tasks = new List<Task<Dictionary<string, Box3>>>();

        for (var index = 0; index < sourceLODFiles.Length; index++)
        {
            var file = sourceLODFiles[index];
            var dest = Path.Combine(destFolder, "LOD-" + index);
            
            // We compress textures except the first one (the original one)
            var textureStrategy = keepOriginalTextures ? TexturesStrategy.KeepOriginal :
                index == 0 ? TexturesStrategy.Repack : TexturesStrategy.RepackCompressed;

            var splitTask = Split(file, dest, limitLength, zsplit, bounds, textureStrategy);

            tasks.Add(splitTask);
        }

        await Task.WhenAll(tasks);

        return tasks.Select(task => task.Result).ToArray();
    }

    public static async Task<Dictionary<string, Box3>> Split(string sourcePath, string destPath, double limitLength,
        bool zSplit = false,
        Box3? bounds = null,
        TexturesStrategy textureStrategy = TexturesStrategy.Repack,
        SplitPointStrategy splitPointStrategy = SplitPointStrategy.VertexBaricenter)
    {
        var sw = new Stopwatch();
        var tilesBounds = new Dictionary<string, Box3>();

        Directory.CreateDirectory(destPath);
        
        Console.WriteLine($" -> Loading OBJ file \"{sourcePath}\"");
        var sourceFileName = Path.GetFileName(sourcePath);

        sw.Start();
        var mesh = MeshUtils.LoadMesh(sourcePath, out var deps);

        Console.WriteLine(
            $" ?> Loaded {mesh.VertexCount} vertices, {mesh.FacesCount} faces in {sw.ElapsedMilliseconds}ms");

        if (limitLength == 0)
        {
            Console.WriteLine(" -> Skipping split stage, just compressing textures and cleaning up the mesh");

            if (mesh is MeshT t)
                t.TexturesStrategy = TexturesStrategy.Compress;
            
            mesh.WriteObj(Path.Combine(destPath, $"{mesh.Name}.obj"));
            
            return new Dictionary<string, Box3> { { mesh.Name, mesh.Bounds } };
            
        }
                
        Console.WriteLine(
            $" -> Splitting with a depth of {limitLength}{(zSplit ? " with z-split" : "")}");

        var meshes = new ConcurrentBag<IMesh>();

        sw.Restart();

        int count;

        if (bounds != null)
        {
            count = await MeshUtils.RecurseSplitXY(mesh, limitLength, bounds, zSplit, meshes);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(splitPointStrategy));
            /*
            Func<IMesh, Vertex3> getSplitPoint = splitPointStrategy switch
            {
                SplitPointStrategy.AbsoluteCenter => m => m.Bounds.Center,
                SplitPointStrategy.VertexBaricenter => m => m.GetVertexBaricenter(),
                _ => throw new ArgumentOutOfRangeException(nameof(splitPointStrategy))
            };

            count = zSplit
                ? await MeshUtils.RecurseSplitXYZ(mesh, limitLength, getSplitPoint, meshes)
                : await MeshUtils.RecurseSplitXY(mesh, limitLength, getSplitPoint, meshes);*/
        }

        sw.Stop();

        Console.WriteLine(
            $" ?> Done {count} edge splits in {sw.ElapsedMilliseconds}ms ({(double)count / sw.ElapsedMilliseconds:F2} split/ms)");

        Console.WriteLine(" -> Writing tiles");

        sw.Restart();

        var ms = meshes.ToArray();
        // 전체 FaceCount
        int totalFaceCount = 0;
        for (var index = 0; index < ms.Length; index++)
            totalFaceCount += ms[index].FacesCount;

        int progressFaceCount = 0;
        for (var index = 0; index < ms.Length; index++)
        {
            var m = ms[index];

            if (m is MeshT t)
                t.TexturesStrategy = textureStrategy;

            var filePath = Path.Combine(destPath, $"{m.Name}.obj");
            m.WriteObj(filePath);

            progressFaceCount += m.FacesCount;
            double percent = progressFaceCount / totalFaceCount;
            Console.WriteLine($"writing splated {sourceFileName} ... {(percent * 100):F2}%");

            tilesBounds.Add(m.Name, m.Bounds);
        }

        Console.WriteLine($" ?> {meshes.Count} tiles written in {sw.ElapsedMilliseconds}ms");

        return tilesBounds;
    }
}

public enum SplitPointStrategy
{
    AbsoluteCenter,
    VertexBaricenter
}