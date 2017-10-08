using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Compat
{
  static class Utils
  {
    #region MethodMatching

    internal static MethodDefinition TryMatchMethod(TypeDefinition type, MethodDefinition candidate)
    {
      if (type == null)
        return null;

      if (!type.HasMethods)
        return null;

      Dictionary<string, string> gp = null;
      foreach (MethodDefinition method in type.Methods)
      {
        // when matching overrides, the base class' method must come first, otherwise, if
        // the derived class defines a type that was generic in the base class, TypeMatch
        // will fail when comparing methods with generic parameters
        // see RH-41841
        if (MethodMatch(candidate, method, ref gp))
          return candidate;
        if (gp != null)
          gp.Clear();
      }

      return null;
    }

    static bool MethodMatch(MethodDefinition candidate, MethodDefinition method, ref Dictionary<string, string> genericParameters)
    {
      if (!candidate.IsVirtual)
        return false;

      if (candidate.HasParameters != method.HasParameters)
        return false;

      if (candidate.Name != method.Name)
        return false;

      if (candidate.HasGenericParameters != method.HasGenericParameters)
        return false;

      // we need to track what the generic parameter represent - as we cannot allow it to
      // differ between the return type or any parameter
      if (!TypeMatch(candidate.ReturnType, method.ReturnType, ref genericParameters))
        return false;

      if (!candidate.HasParameters)
        return true;

      var cp = candidate.Parameters;
      var mp = method.Parameters;
      if (cp.Count != mp.Count)
        return false;

      for (int i = 0; i < cp.Count; i++)
      {
        if (!TypeMatch(cp[i].ParameterType, mp[i].ParameterType, ref genericParameters))
          return false;
      }

      return true;
    }

    static bool TypeMatch(IModifierType a, IModifierType b, ref Dictionary<string, string> gp)
    {
      if (!TypeMatch(a.ModifierType, b.ModifierType, ref gp))
        return false;

      return TypeMatch(a.ElementType, b.ElementType, ref gp);
    }

    static bool TypeMatch(TypeSpecification a, TypeSpecification b, ref Dictionary<string, string> gp)
    {
      var gita = a as GenericInstanceType;
      if (gita != null)
        return TypeMatch(gita, (GenericInstanceType)b, ref gp);

      var mta = a as IModifierType;
      if (mta != null)
        return TypeMatch(mta, (IModifierType)b, ref gp);

      return TypeMatch(a.ElementType, b.ElementType, ref gp);
    }

    static bool TypeMatch(GenericInstanceType a, GenericInstanceType b, ref Dictionary<string, string> gp)
    {
      if (!TypeMatch(a.ElementType, b.ElementType, ref gp))
        return false;

      if (a.HasGenericArguments != b.HasGenericArguments)
        return false;

      if (!a.HasGenericArguments)
        return true;

      var gaa = a.GenericArguments;
      var gab = b.GenericArguments;
      if (gaa.Count != gab.Count)
        return false;

      for (int i = 0; i < gaa.Count; i++)
      {
        if (!TypeMatch(gaa[i], gab[i], ref gp))
          return false;
      }

      return true;
    }

    static bool TypeMatch(TypeReference a, TypeReference b, ref Dictionary<string, string> gp)
    {
      var gpa = a as GenericParameter;
      if (gpa != null)
      {
        if (gp == null)
          gp = new Dictionary<string, string>();
        string match;
        if (!gp.TryGetValue(gpa.FullName, out match))
        {
          // first use, we assume it will always be used this way
          gp.Add(gpa.FullName, b.ToString());
          return true;
        }
        // re-use, it should match the previous usage
        return match == b.ToString();
      }

      if (a is TypeSpecification || b is TypeSpecification)
      {
        if (a.GetType() != b.GetType())
          return false;

        return TypeMatch((TypeSpecification)a, (TypeSpecification)b, ref gp);
      }

      return a.FullName == b.FullName;
    }

    static TypeDefinition GetBaseType(TypeDefinition type)
    {
      if (type == null || type.BaseType == null)
        return null;

      return type.BaseType.Resolve();
    }

    #endregion

    #region Accessibility

    /// <summary>
    /// Checks whether <paramref name="type"/> is derived from <param name="base_type"/>.
    /// </summary>
    /// <returns><c>true</c> if <paramref name="type"/> is derived from <paramref name="base_type"/>, <c>false</c>
    /// otherwise.</returns>
    internal static bool IsDerived(TypeDefinition type, TypeReference base_type)
    {
      if (type == null || type.BaseType == null || base_type == null)
        return false;
      if (type.BaseType.FullName == base_type.FullName)
        return true;
      try
      {
        return IsDerived(type.BaseType.Resolve(), base_type);
      }
      catch (AssemblyResolutionException)
      {
        return false;
      }
    }

    /// <summary>
    /// Checks the accessibility of a <see cref="FieldDefinition"/>.
    /// </summary>
    /// <returns><c>true</c>, if <paramref name="field"/> is accessible in the assembly's public API, <c>false</c>
    /// otherwise.</returns>
    /// <param name="calling_type">The calling type (used to assess accessibility in cases of <c>protected</c> and
    /// <c>protected internal</c>.</param>
    /// <remarks>Assumes that the calling assembly is not a friend of the referenced assembly.</remarks>
    internal static bool IsFieldAccessible(FieldDefinition field, TypeDefinition calling_type)
    {
      if (field.IsPrivate)
        return false;
      if (field.IsAssembly) // internal
        return false;
      if (field.IsFamilyAndAssembly)
        return false;
      if ((field.IsFamily || field.IsFamilyOrAssembly) && !IsDerived(calling_type, field.DeclaringType))
        // allow protected (and protected internal) if calling type is derived from declaring type
        return false;
      return true;
    }

    /// <summary>
    /// Checks the accessibility of a <see cref="MethodDefinition"/>.
    /// </summary>
    /// <returns><c>true</c>, if <paramref name="method"/> is accessible in the assembly's public API, <c>false</c>
    /// otherwise.</returns>
    /// <param name="method">A method.</param>
    /// <param name="calling_type">The calling type (used to assess accessibility in cases of <c>protected</c> and
    /// <c>protected internal</c>.</param>
    /// <remarks>Assumes that the calling assembly is not a friend of the referenced assembly.</remarks>
    internal static bool IsMethodAccessible(MethodDefinition method, TypeDefinition calling_type)
    {
      if (method.IsPrivate)
        return false;
      if (method.IsAssembly) // internal
        return false;
      if (method.IsFamilyAndAssembly) // private protected
        return false;
      if ((method.IsFamily || method.IsFamilyOrAssembly) && !IsDerived(calling_type, method.DeclaringType))
        // allow protected (and protected internal) if calling type is derived from declaring type
        return false;
      if (!IsTypeAccessible(method.DeclaringType, calling_type))
        return false;
      return true;
    }

    /// <summary>
    /// Checks the accessibility of a <see cref="TypeDefinition"/>.
    /// </summary>
    /// <returns><c>true</c>, if <paramref name="type"/> is accessible in the assembly's public API, <c>false</c>
    /// otherwise.</returns>
    /// <param name="type">A type.</param>
    /// <param name="calling_type">The calling type (used to assess accessibility in cases of <c>protected</c> and
    /// <c>protected internal</c>.</param>
    /// <remarks>Assumes that the calling assembly is not a friend of the referenced assembly.</remarks>
    internal static bool IsTypeAccessible(TypeDefinition type, TypeDefinition calling_type)
    {
      if (!CheckAccessibilityOfNestedType(type))
        return false;
      return CheckProtectedAccessibilityOfNestedType(type, calling_type);
    }

    static bool CheckAccessibilityOfNestedType(TypeDefinition type)
    {
      if (!type.IsNested)
      {
        if (type.IsNotPublic)
          return false;
        return true;
      }
      if (type.IsNestedPrivate)
        return false;
      if (type.IsNestedAssembly)
        return false;
      if (type.IsNestedFamilyAndAssembly)
        return false;
      return CheckAccessibilityOfNestedType(type.DeclaringType);
    }

    static bool CheckProtectedAccessibilityOfNestedType(TypeDefinition type, TypeDefinition calling_type)
    {
      if (!type.IsNested)
        return true;
      if ((type.IsNestedFamily || type.IsNestedFamilyOrAssembly) && !IsDerived(calling_type, type.DeclaringType))
        return false;
      return CheckProtectedAccessibilityOfNestedType(type.DeclaringType, calling_type);
    }

    #endregion
  }
}

