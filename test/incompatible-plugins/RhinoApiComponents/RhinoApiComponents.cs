namespace RhinoApiComponents
{
  // Built against a Rhino 8 Grasshopper, and calls a method Rhino 9's Grasshopper doesn't have.
  public static class UsesRemovedApi
  {
    public static void Run()
    {
      Grasshopper.Instances.RemovedInRhino9();
    }
  }
}
