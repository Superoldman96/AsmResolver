using System;

namespace AsmResolver.PE.DotNet.Metadata.Tables
{
    /// <summary>
    /// Provides members defining all flags that can be assigned to a generic parameter.
    /// </summary>
    [Flags]
    public enum GenericParameterAttributes : ushort
    {
        /// <summary>
        /// Specifies the generic parameter has no special variance rules applied to it.
        /// </summary>
        NonVariant = 0x0000,
        /// <summary>
        /// Specifies the generic parameter is covariant and can appear as the result type of a method, the type of a read-only field, a declared base type or an implemented interface.
        /// </summary>
        Covariant = 0x0001,
        /// <summary>
        /// Specifies the generic parameter is contravariant and can appear as a parameter type in method signatures.
        /// </summary>
        Contravariant = 0x0002,
        /// <summary>
        /// Provides a mask for variance of type parameters, only applicable to generic parameters for generic interfaces and delegates
        /// </summary>
        VarianceMask = 0x0003,
        /// <summary>
        /// Provides a mask for additional constraint rules.
        /// </summary>
        SpecialConstraintMask = 0x001C,
        /// <summary>
        /// Specifies the generic parameter's type argument must be a type reference.
        /// </summary>
        ReferenceTypeConstraint = 0x0004,
        /// <summary>
        /// Specifies the generic parameter's type argument must be a value type and not nullable.
        /// </summary>
        NotNullableValueTypeConstraint = 0x0008,
        /// <summary>
        /// Specifies the generic parameter's type argument must have a public default constructor.
        /// </summary>
        DefaultConstructorConstraint = 0x0010,
        /// <summary>
        /// Specifies the generic parameter can be a ref struct type.
        /// </summary>
        AllowByRefLike = 0x0020,
    }
}
