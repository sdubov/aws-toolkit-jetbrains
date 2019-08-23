using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.ThemedIcons;
using JetBrains.Util;

namespace ReSharper.AWS.Lambda
{
    public static class LambdaFinder
    {
        private static readonly IClrTypeName LambdaContextTypeName = new ClrTypeName("Amazon.Lambda.Core.ILambdaContext");
        private static readonly IClrTypeName AmazonLambdaNamespaceTypeName = new ClrTypeName("Amazon.Lambda");
        private static readonly IClrTypeName StreamTypeName = new ClrTypeName("System.IO.Stream");
        private static readonly IClrTypeName AmazonSerializerTypeName = new ClrTypeName("Amazon.Lambda.Core.ILambdaSerializer");
        private static readonly IClrTypeName AmazonAttributeTypeName = new ClrTypeName("Amazon.Lambda.Core.LambdaSerializerAttribute");

        /// <summary>
        /// Check whether method can be defined as Amazon Lambda function.
        ///
        /// Please see info about Amazon Lambda handler in C# here -
        ///     https://docs.aws.amazon.com/lambda/latest/dg/dotnet-programming-model-handler-types.html
        ///
        /// The logic perform the following checks:
        /// 1. Public static or instance method.
        /// 2. Function class should have not be inherited from other class.
        /// 3. Should be a public class with default constructor.
        /// 4. Check for method parameters:
        ///    a) Parameter of <see cref="System.IO.Stream"/> type can be used without Serializer. Any class derived from Stream is a match.
        ///    b) Check for Amazon Event type (set of pre-defined Amazon types used in Amazon Lambda functions)
        ///       and for default Amazon serializer.
        ///    c) Check for any Custom Data type that could be serialized using <see cref="LambdaRunMarkersThemedIcons.Lambda.Core.ILambdaSerializer"/> serializer.
        /// 5. Check for method return type:
        ///    a) If method is async - return type should be <see cref="System.Void"/> or <see cref="System.Threading.Tasks.Task"/> are allowed.
        ///    b) If method is sync - return type could be <see cref="System.IO.Stream"/>,
        ///       or Amazon Event type or Custom Data type with defined <see cref="LambdaRunMarkersThemedIcons.Lambda.Core.ILambdaSerializer"/> serializer.
        /// </summary>
        /// <param name="method">Method to check if it matches Amazon Lambda definition.</param>
        /// <param name="logger">The <see cref="ILogger"/> logger instance.</param>
        /// <returns>The <see cref="bool"/> value if a method matches Amazon Lambda definition.</returns>
        public static bool IsSuitableLambdaMethod(IMethod method, ILogger logger)
        {
            return method != null &&
                   method.GetAccessRights() == AccessRights.PUBLIC &&
                   !HasSuperTypes(method.GetContainingType() as IClass) &&
                   IsValidInstanceOrStaticMethod(method) &&
                   HasRequiredParameters(method, logger) &&
                   HasRequiredReturnType(method, logger);
        }

        private static bool HasSuperTypes(IClass clazz)
        {
            return clazz != null && clazz.GetSuperTypes().Count > 1;
        }

        private static bool IsValidInstanceOrStaticMethod(IMethod method)
        {
            if (!(method.GetContainingType() is IClass classElement)) return false;
            if (method.IsStatic) return true;

            return CanBeInstantiatedByLambda(classElement);
        }

        private static bool CanBeInstantiatedByLambda(IClass classElement)
        {
            return classElement.GetAccessRights() == AccessRights.PUBLIC &&
                   classElement.CanInstantiateWithPublicDefaultConstructor();
        }

        private static bool HasRequiredParameters(IMethod method, ILogger logger)
        {
            var parameters = method.Parameters;

            switch (parameters.Count)
            {
                case 1:
                {
                    var firstParameterType = parameters[0].Type;
                    return IsStreamType(firstParameterType) ||
                           (IsAmazonEventType(firstParameterType) || IsCustomDataType(firstParameterType, new HashSet<IType>(), logger)) && IsSerializerDefined(method);
                }

                case 2:
                {
                    var firstParameterType = parameters[0].Type;
                    var secondParameterType = parameters[1].Type;

                    return IsStreamType(firstParameterType) ||
                           (IsAmazonEventType(firstParameterType) || IsCustomDataType(firstParameterType, new HashSet<IType>(), logger)) && IsSerializerDefined(method) &&
                           IsLambdaContextType(secondParameterType);
                }

                default:
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check for custom data type for input and output parameters specified for Lambda function.
        /// </summary>
        /// <param name="type">The <see cref="T:JetBrains.ReSharper.Psi.IType" /> to verify against custom user type</param>
        /// <param name="typesUnderProcess">The hash set to store all types that are processing right now. Is used to avoid falling into infinitive recursion</param>
        /// <param name="logger">The <see cref="ILogger"/> logger instance.</param>
        /// <returns>Whether type is a custom data type</returns>
        private static bool IsCustomDataType(IType type, HashSet<IType> typesUnderProcess, ILogger logger)
        {
            if (!typesUnderProcess.Add(type)) return true;
            logger.Trace("Check is Custom Data for a type: {0}", type.GetPresentableName(CSharpLanguage.Instance));

            if (type.IsVoid()) return false;

            // Skip any primitive types, DateTime, and DateTimeOffset according to Newtonsoft.Json.Serialization logic.
            if (type.IsSimplePredefined() || type.IsDateTime() || type.IsDateTimeOffset()) return true;

            if (type is IArrayType arrayType)
            {
                return IsCustomDataType(arrayType.ElementType, typesUnderProcess, logger);
            }

            if (type is IDeclaredType declaredType)
            {
                var predefinedType = declaredType.Module.GetPredefinedType();

                var typeElement = declaredType.GetTypeElement();
                logger.Trace("Check type element: {0}", typeElement?.GetClrName());
                if (typeElement == null) return false;

                // Define a substitution to verify generic types.
                var substitution = declaredType.GetSubstitution();

                // Check for dictionary types.
                var genericDictionaryTypeElement = predefinedType.GenericIDictionary.GetTypeElement();
                if (genericDictionaryTypeElement != null && typeElement.IsDescendantOf(genericDictionaryTypeElement))
                {
                    var keyTypeParameter = genericDictionaryTypeElement.TypeParameters[0];
                    var valueTypeParameter = genericDictionaryTypeElement.TypeParameters[1];

                    foreach (var ancestorSubstitution in typeElement.GetAncestorSubstitution(genericDictionaryTypeElement))
                    {
                        // Define a case when inner class override one TKey or TValue, e.g.
                        // class MyType<T> : IDictionary<int, T> {}
                        var effectiveSubstitution = ancestorSubstitution.Apply(substitution);

                        var keyType = effectiveSubstitution.Apply(keyTypeParameter);
                        if (!IsCustomDataType(keyType, typesUnderProcess, logger)) return false;

                        var valueType = effectiveSubstitution.Apply(valueTypeParameter);
                        if (!IsCustomDataType(valueType, typesUnderProcess, logger)) return false;
                    }

                    return true;
                }

                // Check for collection types.
                var elementTypes =
                    CollectionTypeUtil.GetElementTypesForGenericType(
                        declaredType, predefinedType.GenericIEnumerable, 0)
                    ?? CollectionTypeUtil.GetElementTypesForGenericType(
                        declaredType, predefinedType.GenericIList, 0);

                if (elementTypes != null)
                {
                    return elementTypes.All(elementType => IsCustomDataType(elementType, typesUnderProcess, logger));
                }

                // Check non-generic collection and map types
                // assuming that value is of type Object and is always valid option.
                if (declaredType.IsSubtypeOf(predefinedType.IList)
                    || declaredType.IsSubtypeOf(predefinedType.IEnumerable)
                    || declaredType.IsSubtypeOf(predefinedType.IDictionary))
                {
                    return true;
                }

                // Check for POCO types
                if (typeElement is IClass classTypeElement)
                {
                    var superClass = classTypeElement.GetBaseClassType();
                    if (superClass != null && !predefinedType.Object.Equals(superClass)) return false;

                    if (!classTypeElement.CanInstantiateWithPublicDefaultConstructor()) return false;

                    return CheckMemberTypes(classTypeElement.GetMembers(), substitution);
                }

                if (typeElement is IStruct structTypeElement)
                {
                    return CheckMemberTypes(structTypeElement.GetMembers(), substitution);
                }
            }

            return false;

            // Check all fields and properties inside a class or struct for a custom data type
            bool CheckMemberTypes(IEnumerable<ITypeMember> members, ISubstitution substitution)
            {
                var typeMembers = members.AsArray();
                logger.Trace("Verify members: {0}", string.Join(", ", typeMembers.Select(member => member.ShortName)));
                foreach (var typeMember in typeMembers)
                {
                    switch (typeMember)
                    {
                        case IField field when field.IsField:
                        {
                            var fieldType = substitution.Apply(field.Type);
                            if (!IsCustomDataType(fieldType, typesUnderProcess, logger)) return false;
                            break;
                        }
                        case IProperty property when !property.IsDefault:
                        {
                            var propertyType = substitution.Apply(property.Type);
                            if (!IsCustomDataType(propertyType, typesUnderProcess, logger)) return false;
                            break;
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Check for predefined Amazon event types declared in Amazon namespaces
        /// </summary>
        /// <param name="type">The <see cref="T:JetBrains.ReSharper.Psi.IType" /> to verify against Amazon type</param>
        /// <returns>Whether type is Amazon event type</returns>
        private static bool IsAmazonEventType(IType type)
        {
            var clrName = (type as IDeclaredType)?.GetClrName();
            if (clrName == null) return false;

            var typeElement = type.GetTypeElement();
            if (typeElement == null) return false;

            var symbolCache = type.GetPsiServices().Symbols;
            var symbolScope = symbolCache.GetSymbolScope(type.Module, true, true);

            var amazonLambdaNamespace = symbolScope.GetNamespace(AmazonLambdaNamespaceTypeName.FullName);
            if (amazonLambdaNamespace == null) return false;

            var amazonLambdaEventNamespaces = amazonLambdaNamespace.GetNestedNamespaces(symbolScope)
                .Where(@namespace => @namespace.QualifiedName.EndsWith("Events"))
                .Select(@namespace => @namespace.QualifiedName);

            foreach (var amazonEventNamespaceName in amazonLambdaEventNamespaces)
            {
                var namespaceElement = symbolScope.GetNamespace(amazonEventNamespaceName);
                var amazonEventClasses = namespaceElement?.GetNestedTypeElements(symbolScope).Where(element => element is IClass);

                if (amazonEventClasses == null) continue;

                if (amazonEventClasses.Any(eventClass => typeElement.IsDescendantOf(eventClass)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStreamType(IType type)
        {
            var streamType = TypeFactory.CreateTypeByCLRName(StreamTypeName, NullableAnnotation.Unknown, type.Module);
            return type.IsSubtypeOf(streamType);
        }

        /// <summary>
        /// Check if we have a method or assembly level serializer implementing ILambdaSerializer
        /// Please see - https://docs.aws.amazon.com/lambda/latest/dg/dotnet-programming-model-handler-types.html for details
        /// </summary>
        /// <param name="method">The <see cref="T:JetBrains.ReSharper.Psi.IMethod" /> instance to validate</param>
        /// <returns>Whether serializer is defined</returns>
        private static bool IsSerializerDefined(IMethod method)
        {
            var psiModule = method.Module;

            var amazonSerializerType =
                TypeFactory.CreateTypeByCLRName(AmazonSerializerTypeName, NullableAnnotation.Unknown, psiModule);

            var methodAttributes = method.GetAttributeInstances(AmazonAttributeTypeName, true);
            if (!methodAttributes.IsEmpty())
            {
                if (methodAttributes.Any(attribute =>
                    attribute.PositionParameters().Any(parameter =>
                        parameter.TypeValue?.IsSubtypeOf(amazonSerializerType) == true)))
                {
                    return true;
                }
            }

            var symbolCache = psiModule.GetPsiServices().Symbols;
            var assemblyAttributes = symbolCache.GetModuleAttributes(psiModule)
                .GetAttributeInstances(AmazonAttributeTypeName, true);

            return !assemblyAttributes.IsEmpty() && assemblyAttributes.Any(attribute =>
                       attribute.PositionParameters().Any(parameter =>
                           parameter.TypeValue?.IsSubtypeOf(amazonSerializerType) == true));
        }

        /// <summary>
        /// Lambda function can provide a meta-information that could be obtained from Lambda context.
        /// Check if type is <see cref="T:Amazon.Lambda.Core.ILambdaContext" /> or inherited from it.
        /// </summary>
        /// <param name="type">The <see cref="T:JetBrains.ReSharper.Psi.IType" /> to verify against Lambda context</param>
        /// <returns>Whether type is Lambda context</returns>
        private static bool IsLambdaContextType(IType type)
        {
            var clrName = (type as IDeclaredType)?.GetClrName();
            return clrName != null && clrName.Equals(LambdaContextTypeName);
        }

        private static bool HasRequiredReturnType(IMethod method, ILogger logger)
        {
            var returnType = method.ReturnType;

            if (method.IsAsync)
            {
                if (returnType.IsVoid() || returnType.IsTask()) return true;

                if (returnType.IsGenericTask())
                {
                    var underlyingType = returnType.GetGenericUnderlyingType(returnType.GetTypeElement());
                    return (IsAmazonEventType(underlyingType) || IsCustomDataType(underlyingType, new HashSet<IType>(), logger)) && IsSerializerDefined(method);
                }

                return false;
            }

            return IsStreamType(returnType) || (IsAmazonEventType(returnType) || IsCustomDataType(returnType, new HashSet<IType>(), logger)) && IsSerializerDefined(method);
        }
    }
}