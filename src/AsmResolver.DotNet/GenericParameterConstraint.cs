using System.Collections.Generic;
using System.Threading;
using AsmResolver.Collections;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace AsmResolver.DotNet
{
    /// <summary>
    /// Represents an object that constrains a generic parameter to only be instantiated with a specific type.
    /// </summary>
    public class GenericParameterConstraint :
        MetadataMember,
        IHasCustomAttribute,
        IModuleProvider,
        IOwnedCollectionElement<GenericParameter>
    {
        private readonly LazyVariable<GenericParameterConstraint, GenericParameter?> _owner;
        private readonly LazyVariable<GenericParameterConstraint, ITypeDefOrRef?> _constraint;
        private IList<CustomAttribute>? _customAttributes;

        /// <summary>
        /// Initializes the generic parameter constraint with a metadata token.
        /// </summary>
        /// <param name="token">The metadata token.</param>
        protected GenericParameterConstraint(MetadataToken token)
            : base(token)
        {
            _owner = new LazyVariable<GenericParameterConstraint, GenericParameter?>(x => x.GetOwner());
            _constraint = new LazyVariable<GenericParameterConstraint, ITypeDefOrRef?>(x => x.GetConstraint());
        }

        /// <summary>
        /// Creates a new constraint for a generic parameter.
        /// </summary>
        /// <param name="constraint">The type to constrain the generic parameter to.</param>
        public GenericParameterConstraint(ITypeDefOrRef? constraint)
            : this(new MetadataToken(TableIndex.GenericParamConstraint, 0))
        {
            Constraint = constraint;
        }

        /// <summary>
        /// Gets the generic parameter that was constrained.
        /// </summary>
        public GenericParameter? Owner
        {
            get => _owner.GetValue(this);
            private set => _owner.SetValue(value);
        }

        /// <inheritdoc />
        GenericParameter? IOwnedCollectionElement<GenericParameter>.Owner
        {
            get => Owner;
            set => Owner = value;
        }

        /// <summary>
        /// Gets or sets the type that the generic parameter was constrained to.
        /// </summary>
        public ITypeDefOrRef? Constraint
        {
            get => _constraint.GetValue(this);
            set => _constraint.SetValue(value);
        }

        /// <inheritdoc />
        public ModuleDefinition? DeclaringModule => Owner?.DeclaringModule;

        ModuleDefinition? IModuleProvider.ContextModule => DeclaringModule;

        /// <inheritdoc />
        public IList<CustomAttribute> CustomAttributes
        {
            get{
                if (_customAttributes is null)
                    Interlocked.CompareExchange(ref _customAttributes, GetCustomAttributes(), null);
                return _customAttributes;

            }
        }

        /// <summary>
        /// Obtains the generic parameter that was constrained.
        /// </summary>
        /// <returns>The generic parameter</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Owner"/> property.
        /// </remarks>
        protected virtual GenericParameter? GetOwner() => null;

        /// <summary>
        /// Obtains the type that the generic parameter was constrained to.
        /// </summary>
        /// <returns>The type.</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="Constraint"/> property.
        /// </remarks>
        protected virtual ITypeDefOrRef? GetConstraint() => null;

        /// <summary>
        /// Obtains the list of custom attributes assigned to the member.
        /// </summary>
        /// <returns>The attributes</returns>
        /// <remarks>
        /// This method is called upon initialization of the <see cref="CustomAttributes"/> property.
        /// </remarks>
        protected virtual IList<CustomAttribute> GetCustomAttributes() =>
            new OwnedCollection<IHasCustomAttribute, CustomAttribute>(this);
    }
}
