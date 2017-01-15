//#define ACCESS
using System;

namespace AccessTestLib
{
  public class MyClass
  {
    /////////////
    // METHODS //
    /////////////

#if ACCESS
    private void PublicMethodBecomesPrivate()
#else
    public void PublicMethodBecomesPrivate()
#endif
    { }

#if ACCESS
    protected void PublicMethodBecomesProtected()
#else
    public void PublicMethodBecomesProtected()
#endif
    { }

#if ACCESS
    internal void PublicMethodBecomesInternal()
#else
    public void PublicMethodBecomesInternal()
#endif
    { }

    ////////////
    // FIELDS //
    ////////////

#if ACCESS
    private bool PublicFieldBecomesPrivate;
#else
    public bool PublicFieldBecomesPrivate;
#endif

#if ACCESS
    protected bool PublicFieldBecomesProtected;
#else
    public bool PublicFieldBecomesProtected;
#endif

#if ACCESS
    internal bool PublicFieldBecomesInternal;
#else
    public bool PublicFieldBecomesInternal;
#endif

    ////////////////
    // PROPERTIES //
    ////////////////

#if ACCESS
    private bool PublicPropertyBecomesPrivate
#else
    public bool PublicPropertyBecomesPrivate
#endif
    { get; set; }

#if ACCESS
    protected bool PublicPropertyBecomesProtected
#else
    public bool PublicPropertyBecomesProtected
#endif
    { get; set; }

#if ACCESS
    internal bool PublicPropertyBecomesInternal
#else
    public bool PublicPropertyBecomesInternal
#endif
    { get; set; }
  }

  ///////////
  // TYPES //
  ///////////

#if ACCESS
  internal class PublicClassBecomesPrivate
#else
  public class PublicClassBecomesPrivate
#endif
  { }

#if ACCESS
  internal class PublicClassBecomesInternal
#else
  public class PublicClassBecomesInternal
#endif
  { }
}
