using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.ControlFlow.ReflectionInspection.TypeSystem
{
  public class ReSharperType : Type
  {
    private readonly IType myUnderlyingType;

    public ReSharperType(IType type)
    {
      myUnderlyingType = type;
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
      throw new NotImplementedException();
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
      var declaredType = myUnderlyingType as IDeclaredType;
      if (declaredType != null)
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement != null)
        {
          return typeElement.HasAttributeInstance(
            new ClrTypeName(attributeType.FullName), inherit);
        }
      }

      return false;
    }

    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override Type GetInterface(string name, bool ignoreCase)
    {
      throw new NotImplementedException();
    }

    public override Type[] GetInterfaces()
    {
      throw new NotImplementedException();
    }

    public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override Type[] GetNestedTypes(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override Type GetNestedType(string name, BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override Type GetElementType()
    {
      throw new NotImplementedException();
    }

    protected override bool HasElementTypeImpl()
    {
      throw new NotImplementedException();
    }

    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types,
      ParameterModifier[] modifiers)
    {
      throw new NotImplementedException();
    }

    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
      Type[] types, ParameterModifier[] modifiers)
    {
      throw new NotImplementedException();
    }

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override FieldInfo GetField(string name, BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override FieldInfo[] GetFields(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
    {
      throw new NotImplementedException();
    }

    protected override TypeAttributes GetAttributeFlagsImpl()
    {
      throw new NotImplementedException();
    }

    protected override bool IsArrayImpl()
    {
      throw new NotImplementedException();
    }

    protected override bool IsByRefImpl()
    {
      throw new NotImplementedException();
    }

    protected override bool IsPointerImpl()
    {
      throw new NotImplementedException();
    }

    protected override bool IsPrimitiveImpl()
    {
      throw new NotImplementedException();
    }

    protected override bool IsCOMObjectImpl()
    {
      throw new NotImplementedException();
    }

    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
      ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
    {
      throw new NotImplementedException();
    }

    public override Type UnderlyingSystemType
    {
      get { throw new NotImplementedException(); }
    }

    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
      Type[] types, ParameterModifier[] modifiers)
    {
      throw new NotImplementedException();
    }

    public override string Name
    {
      get { throw new NotImplementedException(); }
    }

    public override Guid GUID
    {
      get { throw new NotImplementedException(); }
    }

    public override Module Module
    {
      get { throw new NotImplementedException(); }
    }

    public override Assembly Assembly
    {
      get { throw new NotImplementedException(); }
    }

    public override string FullName
    {
      get { throw new NotImplementedException(); }
    }

    public override string Namespace
    {
      get { throw new NotImplementedException(); }
    }

    public override string AssemblyQualifiedName
    {
      get { throw new NotImplementedException(); }
    }

    public override Type BaseType
    {
      get { throw new NotImplementedException(); }
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
      throw new NotImplementedException();
    }

    public override Type MakePointerType()
    {
      return base.MakePointerType();
    }

    public override Type MakeByRefType()
    {
      return base.MakeByRefType();
    }

    public override Type MakeArrayType()
    {
      return base.MakeArrayType();
    }

    public override Type MakeArrayType(int rank)
    {
      return base.MakeArrayType(rank);
    }

    public override int GetArrayRank()
    {
      return base.GetArrayRank();
    }

    public override Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
    {
      return base.FindInterfaces(filter, filterCriteria);
    }

    public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
    {
      return base.GetMember(name, bindingAttr);
    }

    public override EventInfo[] GetEvents()
    {
      return base.GetEvents();
    }

    public override int MetadataToken
    {
      get { return base.MetadataToken; }
    }

    public override bool ContainsGenericParameters
    {
      get { return base.ContainsGenericParameters; }
    }

    public override int GenericParameterPosition
    {
      get { return base.GenericParameterPosition; }
    }

    public override bool IsGenericParameter
    {
      get { return base.IsGenericParameter; }
    }

    public override bool IsGenericTypeDefinition
    {
      get { return base.IsGenericTypeDefinition; }
    }

    public override bool IsGenericType
    {
      get { return base.IsGenericType; }
    }

    public override GenericParameterAttributes GenericParameterAttributes
    {
      get { return base.GenericParameterAttributes; }
    }

    public override RuntimeTypeHandle TypeHandle
    {
      get { return base.TypeHandle; }
    }

    public override StructLayoutAttribute StructLayoutAttribute
    {
      get { return base.StructLayoutAttribute; }
    }

    public override Type ReflectedType
    {
      get { return base.ReflectedType; }
    }

    public override MethodBase DeclaringMethod
    {
      get { return base.DeclaringMethod; }
    }

    public override Type DeclaringType
    {
      get { return base.DeclaringType; }
    }

    public override MemberTypes MemberType
    {
      get { return base.MemberType; }
    }

    public override InterfaceMapping GetInterfaceMap(Type interfaceType)
    {
      return base.GetInterfaceMap(interfaceType);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public override bool Equals(object o)
    {
      return base.Equals(o);
    }

    public override string ToString()
    {
      return base.ToString();
    }

    public override bool IsAssignableFrom(Type c)
    {
      return base.IsAssignableFrom(c);
    }

    public override bool IsInstanceOfType(object o)
    {
      return base.IsInstanceOfType(o);
    }

    public override bool IsSubclassOf(Type c)
    {
      return base.IsSubclassOf(c);
    }

    public override Type GetGenericTypeDefinition()
    {
      return base.GetGenericTypeDefinition();
    }

    public override Type[] GetGenericArguments()
    {
      return base.GetGenericArguments();
    }

    protected override bool IsMarshalByRefImpl()
    {
      return base.IsMarshalByRefImpl();
    }

    protected override bool IsContextfulImpl()
    {
      return base.IsContextfulImpl();
    }

    public override Type MakeGenericType(params Type[] typeArguments)
    {
      return base.MakeGenericType(typeArguments);
    }

    protected override bool IsValueTypeImpl()
    {
      return base.IsValueTypeImpl();
    }

    public override Type[] GetGenericParameterConstraints()
    {
      return base.GetGenericParameterConstraints();
    }

    public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
    {
      return base.FindMembers(memberType, bindingAttr, filter, filterCriteria);
    }

    public override MemberInfo[] GetDefaultMembers()
    {
      return base.GetDefaultMembers();
    }

    public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
    {
      return base.GetMember(name, type, bindingAttr);
    }
  }
}