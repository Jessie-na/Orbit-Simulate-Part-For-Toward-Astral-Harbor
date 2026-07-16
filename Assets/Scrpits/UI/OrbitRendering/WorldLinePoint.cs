using Unity.Mathematics;

/// <summary>
/// double3.x = t (坐标时), double3.y = x (空间), double3.z = y (空间)
/// </summary>
public struct WorldLinePoint
{
    public double3 data;

    public double T => data.x;
    public double2 Pos => data.yz;

    public WorldLinePoint(double t, double2 pos)
    {
        data = new double3(t, pos.x, pos.y);
    }
}