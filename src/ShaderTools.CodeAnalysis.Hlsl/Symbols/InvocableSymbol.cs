using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ShaderTools.CodeAnalysis.Symbols;

namespace ShaderTools.CodeAnalysis.Hlsl.Symbols
{
    public abstract class InvocableSymbol : Symbol
    {
        private readonly List<ParameterSymbol> _parameters;
        private readonly List<ParameterSymbol> _templateArguments;
        private readonly List<TemplateTypeSymbol> _templateTypeArguments;
        private ImmutableArray<ParameterSymbol> _parametersArray = ImmutableArray<ParameterSymbol>.Empty;
        private ImmutableArray<ParameterSymbol> _templateArgumentsArray = ImmutableArray<ParameterSymbol>.Empty;
        private ImmutableArray<TemplateTypeSymbol> _templateTypeArgumentsArray = ImmutableArray<TemplateTypeSymbol>.Empty;

        public ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parametersArray == ImmutableArray<ParameterSymbol>.Empty)
                    _parametersArray = _parameters.ToImmutableArray();
                return _parametersArray;
            }
        }

        public ImmutableArray<ParameterSymbol> TemplateArguments
        {
            get
            {
                if (_templateArgumentsArray == ImmutableArray<ParameterSymbol>.Empty)
                    _templateArgumentsArray = _templateArguments.ToImmutableArray();

                return _templateArgumentsArray;
            }
        }

        public ImmutableArray<TemplateTypeSymbol> TemplateTypeArguments
        {
            get
            {
                if (_templateTypeArgumentsArray == ImmutableArray<TemplateTypeSymbol>.Empty)
                    _templateTypeArgumentsArray = _templateTypeArguments.ToImmutableArray();

                return _templateTypeArgumentsArray;
            }
        }

        public TypeSymbol ReturnType { get; }

        internal InvocableSymbol(SymbolKind kind, string name, string documentation, Symbol parent, TypeSymbol returnType, Func<InvocableSymbol, IEnumerable<ParameterSymbol>> lazyParameters = null)
            : base(kind, name, documentation, parent)
        {
            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            _parameters = new List<ParameterSymbol>();
            _templateArguments = new List<ParameterSymbol>();
            _templateTypeArguments = new List<TemplateTypeSymbol>();

            if (lazyParameters != null)
                foreach (var parameter in lazyParameters(this))
                    AddParameter(parameter);

            ReturnType = returnType;
        }

        internal void ClearParameters()
        {
            _parameters.Clear();
            _parametersArray = ImmutableArray<ParameterSymbol>.Empty;
        }

        internal void AddParameter(ParameterSymbol parameter)
        {
            _parameters.Add(parameter);
            _parametersArray = ImmutableArray<ParameterSymbol>.Empty;
        }

        internal void ClearTemplateArguments()
        {
            _templateArguments.Clear();
            _templateArgumentsArray = ImmutableArray<ParameterSymbol>.Empty;
        }

        internal void AddTemplateArgument(ParameterSymbol argument) 
        {
            _templateArguments.Add(argument);
            _templateArgumentsArray = ImmutableArray<ParameterSymbol>.Empty;
        }
        internal void ClearTemplateTypeArguments()
        {
            _templateTypeArguments.Clear();
            _templateTypeArgumentsArray = ImmutableArray<TemplateTypeSymbol>.Empty;
        }

        internal void AddTemplateTypeArgument(TemplateTypeSymbol argument)
        {
            _templateTypeArguments.Add(argument);
            _templateTypeArgumentsArray = ImmutableArray<TemplateTypeSymbol>.Empty;
        }

        protected bool Equals(InvocableSymbol other)
        {
            return base.Equals(other)
                   && _parameters.Count == other._parameters.Count
                   && _parameters.Zip(other._parameters, (x, y) => x.Equals(y)).All(x => x)
                   && _templateArguments.Count == other._templateArguments.Count
                   && _templateArguments.Zip(other._templateArguments, (x, y) => x.Equals(y)).All(x => x)
                   && _templateTypeArguments.Count == other._templateTypeArguments.Count
                   && _templateTypeArguments.Zip(other._templateTypeArguments, (x, y) => x.Equals(y)).All(x => x)
                   && ReturnType.Equals(other.ReturnType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InvocableSymbol) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ _parameters.GetHashCode();
                hashCode = (hashCode * 397) ^ _templateArguments.GetHashCode();
                hashCode = (hashCode * 397) ^ _templateTypeArguments.GetHashCode();
                hashCode = (hashCode * 397) ^ ReturnType.GetHashCode();
                return hashCode;
            }
        }
    }
}