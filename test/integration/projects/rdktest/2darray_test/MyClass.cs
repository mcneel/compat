using System;
using Rhino.Geometry;
using System.Collections.Generic;

namespace darray_test
{
  public class MyClass
  {
    public MyClass()
    {
      // old md array test
      var arr1d = new Point3d[1];
      arr1d[0] = Point3d.Origin;
      var pt1 = arr1d[0];
      var arr2d = new Point3d[1, 1];
      arr2d[0, 0] = Point3d.Origin;
      var pt2 = arr2d[0, 0];
      var arr3d = new Point3d[,,] {};
    }

    // new md array tests
    double ArrayStruct()
    {
      var arr = new Point3d[] { new Point3d(0, 0, 0), new Point3d(0, 1, 0) };
      var dist = arr[0].DistanceTo(arr[1]);
      return dist;
    }

    double Array2dStruct()
    {
      var arr2d = new Point3d[,] { { new Point3d(0, 0, 0), new Point3d(0, 1, 0) } };
      var dist2d = arr2d[0, 0].DistanceTo(arr2d[0, 1]);
      return dist2d;
    }

    bool ArrayRef()
    {
      var arr = new Mesh[] { Mesh.CreateFromBox(new Box(), 10, 10, 10), null };
      arr[1] = Mesh.CreateFromSphere(new Sphere(), 10, 10);
      var m = arr[0];
      return m.Scale(2);
    }

    bool Array2dRef()
    {
      var arr = new Mesh[,] { { Mesh.CreateFromBox(new Box(), 10, 10, 10), null } };
      arr[0, 1] = Mesh.CreateFromSphere(new Sphere(), 10, 10);
      var m = arr[0, 0];
      return m.Scale(2);
    }
    
    string StringArray()
    {
      var arr = new string[10, 20, 30];
      arr[0, 0, 0] = "woot";
      return arr[0, 0, 0];
    }
    

    List<string> UseGenericsWithArrays()
    {
      var arr = new List<string>[10, 20];
      arr[0, 0] = new List<string>();
      return arr[0, 0];
    }
    
    List<Point3d> UseGenericsWithArraysPoint3d()
    {
      var arr = new List<Point3d>[10, 20];
      arr[0, 0] = new List<Point3d>();
      return arr[0, 0];
    }
    
    ValueTuple<System.Version, System.Version> UseSystem()
    {
      var vers = new ValueTuple<System.Version, System.Version>[10, 10];
      vers[0, 0] = new ValueTuple<System.Version, System.Version>(new Version(), new Version());
      return vers[0, 0];
    }
  }
}
