﻿namespace Obj2Tiles.Library.Geometry;

public class FaceT : Face
{

    public int TextureIndexA;
    public int TextureIndexB;
    public int TextureIndexC;


    public override string ToString()
    {
        return $"{IndexA} {IndexB} {IndexC} | {TextureIndexA} {TextureIndexB} {TextureIndexC} | {MaterialIndex}";
    }

    public FaceT(int indexA, int indexB, int indexC, int textureIndexA, int textureIndexB,
        int textureIndexC, int materialIndex) : base(indexA, indexB, indexC, materialIndex )
    {

        TextureIndexA = textureIndexA;
        TextureIndexB = textureIndexB;
        TextureIndexC = textureIndexC;
    }

    public override string ToObj()
    {
        return $"f {IndexA + 1}/{TextureIndexA + 1} {IndexB + 1}/{TextureIndexB + 1} {IndexC + 1}/{TextureIndexC + 1}";
    }
}