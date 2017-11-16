using System;
using Rhino.Geometry;

namespace darray_test
{
  public class MyClass
  {
    public MyClass()
    {
      var arr1d = new Point3d[1];
      arr1d[0] = Point3d.Origin;
      var pt1 = arr1d[0];
      var arr2d = new Point3d[1, 1];
      arr2d[0, 0] = Point3d.Origin;
      var pt2 = arr2d[0, 0];
      var arr3d = new Point3d[,,] {};
    }
  }
}
