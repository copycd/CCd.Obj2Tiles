namespace Obj2Tiles.Library.Geometry;

public class Face
{

    public int IndexA;
    public int IndexB;
    public int IndexC;
    public int MaterialIndex;

    public override string ToString()
    {
        return $"{IndexA} {IndexB} {IndexC}";
    }

    public Face(int indexA, int indexB, int indexC, int materialIndex )
    {
        IndexA = indexA;
        IndexB = indexB;
        IndexC = indexC;
        MaterialIndex = materialIndex;
    }

    public virtual string ToObj()
    {
        return $"f {IndexA + 1} {IndexB + 1} {IndexC + 1}";
    }
}