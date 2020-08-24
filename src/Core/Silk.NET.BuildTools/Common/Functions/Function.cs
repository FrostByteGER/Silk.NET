// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Silk.NET.BuildTools.Overloading;

namespace Silk.NET.BuildTools.Common.Functions
{
    /// <summary>
    /// Represents a C# function.
    /// </summary>
    public class Function : IEquatable<Function>
    {
        /// <summary>
        /// Gets or sets the name of this function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the return type of this function.
        /// </summary>
        public Type ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the name of this function, as defined by the API specification.
        /// </summary>
        public string NativeName { get; set; }

        [JsonIgnore]
        public string ProfileName { get; set; }
        [JsonIgnore]
        public Version ProfileVersion { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the function.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        /// <summary>
        /// Gets or sets the categories in which this function falls under.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        [JsonIgnore]
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the generic type parameters of the function.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public List<GenericTypeParameter> GenericTypeParameters { get; set; } =
            new List<GenericTypeParameter>();

        /// <summary>
        /// Gets or sets a list of attributes to be applied to this function.
        /// </summary>
        public List<Attribute> Attributes { get; set; } = new List<Attribute>();

        /// <summary>
        /// Gets or sets the formatted XML documentation for this function.
        /// </summary>
        public string Doc { get; set; } = "/// <summary>To be added.</summary>";

        /// <summary>
        /// Gets or sets the name (vendor) of the given extension.
        /// </summary>
        public string ExtensionName { get; set; }

        /// <summary>
        /// Gets or sets the calling convention of this function.
        /// </summary>
        public CallingConvention Convention { get; set; }

        /// <summary>
        /// The accessibility modifier to add to this function's declaration.
        /// </summary>
        public Accessibility Accessibility { get; set; }

        /// <summary>
        /// The signature kind of this function, used to determine whether unique steps are needed in binding.
        /// </summary>
        public SignatureKind Kind { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(null);
        }

        /// <inheritdoc cref="ToString()" />
        public string ToString(bool? @unsafe, bool partial = false, bool accessibility = false, bool @static = false)
        {
            var sb = new StringBuilder();

            GetDeclarationString(sb, @unsafe, partial, accessibility, @static);

            sb.Append("(");
            if (Parameters.Count > 0)
            {
                var parameterDeclarations = Parameters.Select(GetDeclarationString).ToList();
                for (var index = 0; index < parameterDeclarations.Count; index++)
                {
                    if (index != 0)
                    {
                        sb.Append(", ");
                    }

                    var parameterDeclaration = parameterDeclarations[index];
                    sb.Append(parameterDeclaration);
                }
            }

            sb.Append(")");

            if (GenericTypeParameters.Count != 0)
            {
                sb.Append(" ");
                for (var index = 0; index < GenericTypeParameters.Count; index++)
                {
                    var p = GenericTypeParameters[index];
                    var constraints = p.Constraints.Any()
                        ? string.Join(", ", p.Constraints)
                        : "struct";

                    sb.Append($"where {p.Name} : {constraints}");
                    if (index != GenericTypeParameters.Count - 1)
                    {
                        sb.Append(" ");
                    }
                }
            }

            sb.Append(";");
            return sb.ToString();
        }

        private void GetDeclarationString(StringBuilder sb,
            bool? @unsafe,
            bool partial = false,
            bool accessibility = false,
            bool @static = false)
        {
            if (accessibility)
            {
                sb.Append
                (
                    Accessibility switch
                    {
                        Accessibility.Public => "public ",
                        Accessibility.Protected => "protected ",
                        _ => string.Empty
                    }
                );
            }

            if (@static)
            {
                sb.Append("static ");
            }
            
            if (Parameters.Any(p => p.Type.IsPointer) || ReturnType.IsPointer || @unsafe.HasValue && @unsafe.Value)
            {
                sb.Append("unsafe ");
            }

            if (partial)
            {
                sb.Append("partial ");
            }

            sb.Append(ReturnType);
            sb.Append(" ");

            sb.Append(Name);
            if (GenericTypeParameters.Count != 0)
            {
                sb.Append("<");
                var genericParameterNames = GenericTypeParameters.Select(p => p.Name);
                var genericParameterList = string.Join(", ", genericParameterNames);
                sb.Append(genericParameterList);
                sb.Append(">");
            }
        }

        [NotNull]
        private static string GetDeclarationString([NotNull] Parameter parameter)
        {
            var sb = new StringBuilder();

            var attributes = new List<string>();

            if (!(parameter.Count is null))
            {
                if (parameter.Count.IsStatic)
                {
                    attributes.Add($"Count(Count = {parameter.Count.StaticCount})");
                }
                else if (parameter.Count.IsComputed)
                {
                    var parameterList = string.Join(", ", parameter.Count.ComputedFromNames);
                    attributes.Add($"Count(Computed = \"{parameterList}\")");
                }
                else if (parameter.Count.IsReference)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    attributes.Add($"Count(Parameter = \"{parameter.Count.ValueReference}\")");
                }
            }

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (parameter.Flow)
            {
                case FlowDirection.In:
                    attributes.Add("Flow(FlowDirection.In)");
                    break;
                case FlowDirection.Out:
                    attributes.Add("Flow(FlowDirection.Out)");
                    break;
            }

            attributes.AddRange(parameter.Attributes.Select(x => x.Name + "(" + string.Join(", ", x.Arguments) + ")"));

            if (attributes.Count != 0)
            {
                sb.Append("[");
                sb.Append(string.Join(", ", attributes));
                sb.Append("] ");
            }

            sb.Append(parameter.Type);

            sb.Append(" ");
            sb.Append(Utilities.CSharpKeywords.Contains(parameter.Name) ? $"@{parameter.Name}" : parameter.Name);

            return sb.ToString();
        }

        /// <inheritdoc />
        public bool Equals(Function other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name) &&
                   Parameters.Select(x => x.Type.ToString()).SequenceEqual(other.Parameters.Select(x => x.Type.ToString())) &&
                   GenericTypeParameters.SequenceEqual(other.GenericTypeParameters);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is Function function && Equals(function);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NativeName != null ? NativeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
                hashCode = (hashCode * 397) ^ Categories.GetHashCode();
                hashCode = (hashCode * 397) ^ GenericTypeParameters.GetHashCode();
                hashCode = (hashCode * 397) ^ (Attributes != null ? Attributes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Doc != null ? Doc.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ExtensionName != null ? ExtensionName.GetHashCode() : 0);
                // ReSharper restore NonReadonlyMemberInGetHashCode
                return hashCode;
            }
        }
    }
}