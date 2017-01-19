using System;
namespace AccessTest
{
  public class MyClass
  {
    public MyClass()
    {
      var cls = new AccessTestLib.MyClass();

      // NOTE: everything here should FAIL the compat test against AccessTestLib
      // compiled with ACCESS constant defined

      /////////////
      // METHODS //
      /////////////

      // call method that will later be private
      cls.PublicMethodBecomesPrivate();

      // call method that will later be protected
      cls.PublicMethodBecomesProtected();

      // call method that will later be internal
      cls.PublicMethodBecomesInternal();

      ////////////
      // FIELDS //
      ////////////

      // call field that will later be private
      var a = cls.PublicFieldBecomesPrivate;

      // call field that will later be protected
      var b = cls.PublicFieldBecomesProtected;

      // call field that will later be internal
      var c = cls.PublicFieldBecomesInternal;

      ////////////////
      // PROPERTIES //
      ////////////////

      // call property setter that will later be private
      cls.PublicPropertyBecomesPrivate = a;

      // call property setter that will later be protected
      cls.PublicPropertyBecomesProtected = b;

      // call property setter that will later be internal
      cls.PublicPropertyBecomesInternal = c;

      ///////////
      // TYPES //
      ///////////

      // instanstiate class that will later be private
      var cls2 = new AccessTestLib.PublicClassBecomesPrivate();

      // instanstiate class that will later be internal
      var cls3 = new AccessTestLib.PublicClassBecomesInternal();

      // get type of class that will later be private
      var cls4 = typeof(AccessTestLib.PublicClassBecomesPrivate);

      // get type of class that will later be internal
      var cls5 = typeof(AccessTestLib.PublicClassBecomesInternal);

      //////////////////
      // NESTED TYPES //
      //////////////////

      // instanstiate nested class that will later be private
      var cls6 = new AccessTestLib.MyClass.NestedClassBecomesPrivate();

      // instanstiate nested class that will later be protected
      var cls7 = new AccessTestLib.MyClass.NestedClassBecomesProtected();

      // instanstiate nested class that will later be internal
      var cls8 = new AccessTestLib.MyClass.NestedClassBecomesInternal();

      // get type of nested class that will later be private
      var cls9 = typeof(AccessTestLib.MyClass.NestedClassBecomesPrivate);

      // get type of nested class that will later be protected
      var cls10 = typeof(AccessTestLib.MyClass.NestedClassBecomesProtected);

      // get type of nested class that will later be internal
      var cls11 = typeof(AccessTestLib.MyClass.NestedClassBecomesInternal);
    }
  }

  ///////////////
  // PROTECTED //
  ///////////////

  public class DerivedClass : AccessTestLib.MyClass
  {
    public DerivedClass()
    {
      // NOTE: everything here should PASS the compat test against AccessTestLib
      // compiled with ACCESS constant defined

      // call method that will later be protected
      this.PublicMethodBecomesProtected();

      // call field that will later be protected
      var b = this.PublicFieldBecomesProtected;

      // call property setter that will later be protected
      this.PublicPropertyBecomesProtected = b;

      // instanstiate class that will later be protected
      var cls = new NestedClassBecomesProtected();

      // get type of class that will later be protected
      var cls2 = typeof(NestedClassBecomesProtected);
    }
  }
}
