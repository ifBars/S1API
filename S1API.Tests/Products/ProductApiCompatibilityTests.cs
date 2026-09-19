#if (IL2CPPMELON)
using NativeDrugType = Il2CppScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = Il2CppScheduleOne.Product.DrugTypeContainer;
#elif MONOMELON
using NativeDrugType = ScheduleOne.Product.EDrugType;
using NativeDrugTypeContainer = ScheduleOne.Product.DrugTypeContainer;
#endif

using S1API.Items;
using S1API.Entities;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using S1API.Rendering;

namespace S1API.Tests.Products;

public sealed class ProductApiCompatibilityTests
{
    [Fact]
    public void ExistingPublicMemberSignaturesRemainAvailable()
    {
#pragma warning disable CS0618
        var nativeDrugType = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugType));
        var nativeDrugTypes = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugTypes));
#pragma warning restore CS0618
        var wrapperMethod = typeof(ProductDefinitionWrapper).GetMethod(
            nameof(ProductDefinitionWrapper.Wrap),
            new[] { typeof(ProductDefinition) });
        var legacyLookup = typeof(ItemManager).GetMethod(
            nameof(ItemManager.GetItemDefinition),
            new[] { typeof(string) });
        var createInstance = typeof(ProductDefinition).GetMethod(
            nameof(ProductDefinition.CreateInstance),
            new[] { typeof(int) });
        var createPackagedInstance = typeof(ProductDefinition).GetMethod(
            nameof(ProductDefinition.CreatePackagedInstance),
            new[] { typeof(int), typeof(PackagingDefinition) });

        Assert.NotNull(nativeDrugType);
        Assert.Equal(typeof(NativeDrugType), nativeDrugType.PropertyType);
        AssertObsoleteCompatibilityShim(nativeDrugType, "Use PrimaryDrugType instead.");
        Assert.NotNull(nativeDrugTypes);
#if IL2CPPMELON
        Assert.Equal(typeof(IReadOnlyList<NativeDrugTypeContainer>), nativeDrugTypes.PropertyType);
#else
        Assert.Equal(typeof(List<NativeDrugTypeContainer>), nativeDrugTypes.PropertyType);
#endif
        AssertObsoleteCompatibilityShim(nativeDrugTypes, "Use DrugTypeValues instead.");
        Assert.NotNull(wrapperMethod);
        Assert.Equal(typeof(ProductDefinition), wrapperMethod.ReturnType);
        Assert.NotNull(legacyLookup);
        Assert.Equal(typeof(ItemDefinition), legacyLookup.ReturnType);
        Assert.NotNull(createInstance);
        Assert.Equal(typeof(ItemInstance), createInstance.ReturnType);
        Assert.True(createInstance.IsVirtual);
        Assert.False(createInstance.IsAbstract);
        AssertSingleOptionalParameter(createInstance, "quantity", 1);
        Assert.NotNull(createPackagedInstance);
        Assert.Equal(typeof(ProductInstance), createPackagedInstance.ReturnType);
        Assert.False(createPackagedInstance.IsVirtual);
        Assert.Equal(
            new[] { "quantity", "packaging" },
            createPackagedInstance.GetParameters().Select(parameter => parameter.Name));

        Assert.Equal(
            typeof(bool),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.IsPackaged))!
                .PropertyType);
        Assert.Equal(
            typeof(PackagingDefinition),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.AppliedPackaging))!
                .PropertyType);
        Assert.Equal(
            typeof(Quality),
            typeof(ProductInstance)
                .GetProperty(nameof(ProductInstance.Quality))!
                .PropertyType);
        Assert.Equal(
            typeof(ProductDefinition),
            typeof(ProductInstance)
                .GetProperty(
                    nameof(ProductInstance.Definition),
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly)!
                .PropertyType);
    }

    [Fact]
    public void NewRuntimeAgnosticMembersAreAdditive()
    {
        var primaryDrugType = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.PrimaryDrugType));
        var drugTypeValues = typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.DrugTypeValues));
        var propertyColorMixing = typeof(ProductMixingProfileBuilder).GetMethod(
            nameof(ProductMixingProfileBuilder.WithPropertyColorMixing),
            Type.EmptyTypes);
        var usesPropertyColorMixing = typeof(ProductMixingProfile).GetProperty(
            nameof(ProductMixingProfile.UsePropertyColorMixing));

        Assert.NotNull(primaryDrugType);
        Assert.Equal(typeof(DrugType), primaryDrugType.PropertyType);
        Assert.NotNull(drugTypeValues);
        Assert.Equal(typeof(IReadOnlyList<DrugType>), drugTypeValues.PropertyType);
        Assert.NotNull(propertyColorMixing);
        Assert.Equal(typeof(ProductMixingProfileBuilder), propertyColorMixing.ReturnType);
        Assert.NotNull(usesPropertyColorMixing);
        Assert.Equal(typeof(bool), usesPropertyColorMixing.PropertyType);
    }

    [Fact]
    public void ProductEffectClearCallbackApiIsAdditiveAndApplyOnlySignaturesAreUnchanged()
    {
        AssertCallbackRegistration(nameof(ProductManager.SetEffectCallback), typeof(Player));
        AssertCallbackRegistration(nameof(ProductManager.SetNpcEffectCallback), typeof(NPC));
        AssertCallbackRemoval(nameof(ProductManager.RemoveEffectCallback));
        AssertCallbackRemoval(nameof(ProductManager.RemoveNpcEffectCallback));
        Assert.NotNull(typeof(ProductManager).GetMethod(nameof(ProductManager.ClearEffectCallbacks), Type.EmptyTypes));
        Assert.NotNull(typeof(ProductManager).GetMethod(nameof(ProductManager.ClearNpcEffectCallbacks), Type.EmptyTypes));

        AssertCallbackRegistration(nameof(ProductManager.SetEffectClearCallback), typeof(Player));
        AssertCallbackRegistration(nameof(ProductManager.SetNpcEffectClearCallback), typeof(NPC));
        AssertCallbackRemoval(nameof(ProductManager.RemoveEffectClearCallback));
        AssertCallbackRemoval(nameof(ProductManager.RemoveNpcEffectClearCallback));
        Assert.NotNull(typeof(ProductManager).GetMethod(nameof(ProductManager.ResetEffectClearCallbacks), Type.EmptyTypes));
        Assert.NotNull(typeof(ProductManager).GetMethod(nameof(ProductManager.ResetNpcEffectClearCallbacks), Type.EmptyTypes));

        Assert.Equal(
            typeof(CustomEffectBuilder),
            typeof(CustomEffectBuilder).GetMethod(
                nameof(CustomEffectBuilder.WithBehavior),
                new[] { typeof(Action<Player>) })!.ReturnType);
        Assert.Equal(
            typeof(CustomEffectBuilder),
            typeof(CustomEffectBuilder).GetMethod(
                nameof(CustomEffectBuilder.WithNpcBehavior),
                new[] { typeof(Action<NPC>) })!.ReturnType);
        Assert.Null(typeof(CustomEffectBuilder).GetMethod(
            nameof(CustomEffectBuilder.WithBehavior),
            new[] { typeof(Action<Player>), typeof(Action<Player>) }));
        Assert.Equal(
            typeof(CustomEffectBuilder),
            typeof(CustomEffectBuilder).GetMethod(
                nameof(CustomEffectBuilder.WithClearBehavior),
                new[] { typeof(Action<Player>) })!.ReturnType);
        Assert.Equal(
            typeof(CustomEffectBuilder),
            typeof(CustomEffectBuilder).GetMethod(
                nameof(CustomEffectBuilder.WithNpcClearBehavior),
                new[] { typeof(Action<NPC>) })!.ReturnType);
    }

    [Fact]
    public void GenericCustomProductApiIsAdditiveAndKeepsOptInDefaults()
    {
        var createBuilder = typeof(CustomProductItemCreator).GetMethod(
            nameof(CustomProductItemCreator.CreateBuilder),
            new[] { typeof(string), typeof(ProductKind) });
        var constructor = typeof(CustomProductDefinitionBuilder).GetConstructor(
            new[] { typeof(string), typeof(ProductKind) });
        var createDefault = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.CreateInstance),
            new[] { typeof(int) });
        var discover = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.Discover),
            new[] { typeof(bool) });
        var setListed = typeof(CustomProductDefinition).GetMethod(
            nameof(CustomProductDefinition.SetListed),
            new[] { typeof(bool) });

        Assert.NotNull(createBuilder);
        Assert.Equal(
            typeof(CustomProductDefinitionBuilder),
            createBuilder.ReturnType);
        Assert.NotNull(constructor);
        Assert.True(typeof(CustomProductDefinition).IsSealed);
        Assert.True(
            typeof(ProductDefinition).IsAssignableFrom(
                typeof(CustomProductDefinition)));
        Assert.NotNull(createDefault);
        Assert.Equal(typeof(ItemInstance), createDefault.ReturnType);
        Assert.True(createDefault.IsVirtual);
        AssertSingleOptionalParameter(createDefault, "quantity", 1);
        Assert.NotNull(discover);
        AssertSingleOptionalParameter(discover, "listForSale", false);
        Assert.NotNull(setListed);
        AssertSingleOptionalParameter(setListed, "listed", true);
    }

    [Fact]
    public void CustomProductSaveProviderApiIsAdditiveAndVersioned()
    {
        var providerMethod = typeof(CustomProductDefinitionBuilder).GetMethod(
            nameof(CustomProductDefinitionBuilder.WithSaveProvider),
            new[] { typeof(string), typeof(int), typeof(string) });
        var register = typeof(CustomProductSaveProviderRegistry).GetMethod(
            nameof(CustomProductSaveProviderRegistry.Register),
            new[] { typeof(ICustomProductSaveProvider) });

        Assert.NotNull(providerMethod);
        Assert.Equal(typeof(CustomProductDefinitionBuilder), providerMethod.ReturnType);
        Assert.Equal(new[] { "providerId", "providerVersion", "providerData" },
            providerMethod.GetParameters().Select(parameter => parameter.Name));
        var providerData = providerMethod.GetParameters()[2];
        Assert.Equal("providerData", providerData.Name);
        Assert.True(providerData.HasDefaultValue);
        Assert.Equal(string.Empty, providerData.DefaultValue);
        Assert.NotNull(register);
        Assert.Equal(typeof(ICustomProductSaveProvider), register.ReturnType);
        Assert.Equal(typeof(int), typeof(CustomProductSaveDescriptor)
            .GetProperty(nameof(CustomProductSaveDescriptor.FormatVersion))!.PropertyType);
    }

    [Fact]
    public void CustomProductMultiplayerApiIsAdditiveAndFailClosed()
    {
        Assert.True(typeof(CustomProductMultiplayer).IsAbstract);
        Assert.True(typeof(CustomProductMultiplayer).IsSealed);
        Assert.Equal(
            CustomProductMultiplayerPolicy.Reject,
            CustomProductMultiplayer.MissingContentPolicy);
        Assert.NotNull(typeof(CustomProductMultiplayer).GetMethod(
            nameof(CustomProductMultiplayer.GetCompatibilityManifestHash),
            Type.EmptyTypes));
        Assert.Equal(
            new[] { 0 },
            Enum.GetValues<CustomProductMultiplayerPolicy>()
                .Select(value => (int)value));
    }

    [Fact]
    public void ProductPresentationProfileApiIsAdditiveAndRuntimeAgnostic()
    {
        Assert.True(typeof(ProductPresentationProfile).IsSealed);
        Assert.True(typeof(ProductPresentationProfileBuilder).IsSealed);
        Assert.True(typeof(ProductPresentationTransform).IsSealed);
        Assert.True(typeof(ProductPresentationProfileRegistry).IsAbstract);
        Assert.True(typeof(ProductPresentationProfileRegistry).IsSealed);
        Assert.Equal(
            new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6
            },
            Enum.GetValues<ProductPresentationContext>()
                .Select(value => (int)value));

        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithLooseVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithStoredVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithHeldVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithAvatarHeldVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithAvatarHeldVisual),
            typeof(Func<UnityEngine.GameObject>),
            typeof(ProductPresentationTransform));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithAvatarHeldTransform),
            typeof(ProductPresentationTransform));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithStationVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(
                ProductPresentationProfileBuilder
                    .WithFunctionalProductVisual),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(
                ProductPresentationProfileBuilder
                    .WithFunctionalProductConvexMeshColliders));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithIcon),
            typeof(Func<UnityEngine.Sprite>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithConsumptionPrefab),
            typeof(Func<UnityEngine.GameObject>));
        AssertBuilderMethod(
            nameof(ProductPresentationProfileBuilder.WithLooseVisual),
            typeof(Func<UnityEngine.GameObject>),
            typeof(ProductPresentationTransform));

        System.Reflection.ConstructorInfo? presentationTransform =
            typeof(ProductPresentationTransform).GetConstructor(
                new[]
                {
                    typeof(UnityEngine.Vector3),
                    typeof(UnityEngine.Vector3),
                    typeof(UnityEngine.Vector3)
                });
        Assert.NotNull(presentationTransform);
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalPosition)));
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalEulerAngles)));
        Assert.NotNull(
            typeof(ProductPresentationTransform).GetProperty(
                nameof(ProductPresentationTransform.LocalScale)));

        System.Reflection.MethodInfo generatedIcon =
            typeof(ProductPresentationProfileBuilder).GetMethod(
                nameof(
                    ProductPresentationProfileBuilder
                        .WithGeneratedIconFromLooseVisual),
                new[] { typeof(int) })!;
        Assert.NotNull(generatedIcon);
        AssertSingleOptionalParameter(generatedIcon, "size", 512);
        Assert.NotNull(
            typeof(ProductPresentationProfileBuilder).GetMethod(
                nameof(
                    ProductPresentationProfileBuilder
                        .WithGeneratedIconFromLooseVisual),
                new[]
                {
                    typeof(int),
                    typeof(bool),
                    typeof(float)
                }));
        AssertBuilderMethod(
            nameof(
                ProductPresentationProfileBuilder
                    .WithGeneratedIconTransform),
            typeof(ProductPresentationTransform));

        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIcon),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIcon),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool),
                    typeof(bool),
                    typeof(float)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIconSprite),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool)
                }));
        Assert.NotNull(
            typeof(IconFactory).GetMethod(
                nameof(IconFactory.GenerateIconSprite),
                new[]
                {
                    typeof(UnityEngine.Transform),
                    typeof(int),
                    typeof(bool),
                    typeof(bool),
                    typeof(float)
                }));

        System.Reflection.MethodInfo registerProduct =
            typeof(ProductPresentationProfileRegistry).GetMethod(
                nameof(ProductPresentationProfileRegistry.RegisterForProduct),
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(ProductPresentationProfile)
                })!;
        System.Reflection.MethodInfo registerKind =
            typeof(ProductPresentationProfileRegistry).GetMethod(
                nameof(
                    ProductPresentationProfileRegistry.RegisterForProductKind),
                new[]
                {
                    typeof(string),
                    typeof(ProductKind),
                    typeof(ProductPresentationProfile)
                })!;
        Assert.NotNull(registerProduct);
        Assert.NotNull(registerKind);
        Assert.Equal(typeof(ProductPresentationProfile), registerProduct.ReturnType);
        Assert.Equal(typeof(ProductPresentationProfile), registerKind.ReturnType);
    }

    [Fact]
    public void ProductConsumptionProfileApiIsAdditiveAndRuntimeAgnostic()
    {
        Assert.True(typeof(ProductConsumptionProfile).IsSealed);
        Assert.True(typeof(ProductConsumptionProfileBuilder).IsSealed);
        Assert.True(typeof(ProductConsumptionContext).IsSealed);
        Assert.True(typeof(ProductConsumptionProfileRegistry).IsAbstract);
        Assert.True(typeof(ProductConsumptionProfileRegistry).IsSealed);

        Assert.Equal(typeof(string), typeof(ProductConsumptionProfile)
            .GetProperty(nameof(ProductConsumptionProfile.ProviderId))!.PropertyType);
        Assert.Equal(typeof(int), typeof(ProductConsumptionProfile)
            .GetProperty(nameof(ProductConsumptionProfile.ProviderVersion))!.PropertyType);
        Assert.Equal(typeof(string), typeof(ProductConsumptionContext)
            .GetProperty(nameof(ProductConsumptionContext.ProductId))!.PropertyType);
        Assert.Equal(typeof(ProductKind), typeof(ProductConsumptionContext)
            .GetProperty(nameof(ProductConsumptionContext.ProductKind))!.PropertyType);

        Assert.Equal(
            typeof(ProductConsumptionProfileBuilder),
            typeof(ProductConsumptionProfileBuilder).GetMethod(
                nameof(ProductConsumptionProfileBuilder.WithProviderCompatibility),
                new[] { typeof(string), typeof(int) })!.ReturnType);
        Assert.Equal(
            typeof(ProductConsumptionProfileBuilder),
            typeof(ProductConsumptionProfileBuilder).GetMethod(
                nameof(ProductConsumptionProfileBuilder.OnPlayerApply),
                new[] { typeof(Action<ProductConsumptionContext>) })!.ReturnType);
        foreach (var callbackName in new[]
                 {
                     nameof(ProductConsumptionProfileBuilder.OnPlayerClear),
                     nameof(ProductConsumptionProfileBuilder.OnNpcApply),
                     nameof(ProductConsumptionProfileBuilder.OnNpcClear)
                 })
        {
            Assert.Equal(
                typeof(ProductConsumptionProfileBuilder),
                typeof(ProductConsumptionProfileBuilder).GetMethod(
                    callbackName,
                    new[] { typeof(Action<ProductConsumptionContext>) })!.ReturnType);
        }
        Assert.Equal(
            typeof(ProductConsumptionProfile),
            typeof(ProductConsumptionProfileBuilder).GetMethod(
                nameof(ProductConsumptionProfileBuilder.Build), Type.EmptyTypes)!.ReturnType);
        Assert.Equal(
            typeof(ProductConsumptionProfile),
            typeof(ProductConsumptionProfileRegistry).GetMethod(
                nameof(ProductConsumptionProfileRegistry.RegisterForProduct),
                new[] { typeof(string), typeof(ProductConsumptionProfile) })!.ReturnType);
        Assert.Equal(
            typeof(ProductConsumptionProfile),
            typeof(ProductConsumptionProfileRegistry).GetMethod(
                nameof(ProductConsumptionProfileRegistry.RegisterForProductKind),
                new[] { typeof(ProductKind), typeof(ProductConsumptionProfile) })!.ReturnType);
    }

    [Fact]
    public void ProductKindMetadataApiIsAdditiveAndKeepsOptInDefaults()
    {
        Assert.True(typeof(ProductKindMetadata).IsSealed);
        Assert.True(typeof(ProductKindMetadataBuilder).IsSealed);
        Assert.True(typeof(ProductKindMetadataRegistry).IsAbstract);
        Assert.True(typeof(ProductKindMetadataRegistry).IsSealed);

        Assert.Equal(
            typeof(ProductKind),
            typeof(ProductKindMetadata)
                .GetProperty(nameof(ProductKindMetadata.ProductKind))!
                .PropertyType);
        Assert.Equal(
            typeof(UnityEngine.Color),
            typeof(ProductKindMetadata)
                .GetProperty(nameof(ProductKindMetadata.Color))!
                .PropertyType);
        Assert.Equal(
            typeof(UnityEngine.Sprite),
            typeof(ProductKindMetadata)
                .GetProperty(nameof(ProductKindMetadata.Icon))!
                .PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<string>),
            typeof(ProductKindMetadata)
                .GetProperty(nameof(ProductKindMetadata.SearchAliases))!
                .PropertyType);

        var visibility = typeof(ProductKindMetadataBuilder).GetMethod(
            nameof(ProductKindMetadataBuilder.WithProductManagerVisibility),
            new[] { typeof(bool) });
        Assert.NotNull(visibility);
        Assert.Equal(
            typeof(ProductKindMetadataBuilder),
            visibility.ReturnType);
        AssertSingleOptionalParameter(visibility, "visible", true);

        Assert.Equal(
            new[] { "productKind" },
            typeof(ProductKindMetadataBuilder)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(parameter => parameter.Name));
        Assert.NotNull(
            typeof(ProductKindMetadataRegistry).GetMethod(
                nameof(ProductKindMetadataRegistry.Get),
                new[] { typeof(string) }));
        Assert.NotNull(
            typeof(ProductKindMetadataRegistry).GetMethod(
                nameof(ProductKindMetadataRegistry.Get),
                new[] { typeof(ProductKind) }));
        Assert.NotNull(
            typeof(ProductKindMetadata).GetMethod(
                nameof(ProductKindMetadata.MatchesSearch),
                new[] { typeof(string) }));
    }

    private static void AssertObsoleteCompatibilityShim(
        System.Reflection.MemberInfo member,
        string expectedMessage)
    {
        var obsolete = member.GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .Cast<ObsoleteAttribute>()
            .Single();

        Assert.Equal(expectedMessage, obsolete.Message);
        Assert.False(obsolete.IsError);
    }

    private static void AssertSingleOptionalParameter(
        System.Reflection.MethodInfo method,
        string expectedName,
        object expectedDefault)
    {
        System.Reflection.ParameterInfo parameter =
            Assert.Single(method.GetParameters());
        Assert.Equal(expectedName, parameter.Name);
        Assert.True(parameter.IsOptional);
        Assert.Equal(expectedDefault, parameter.DefaultValue);
    }

    private static void AssertBuilderMethod(
        string name,
        params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo? method =
            typeof(ProductPresentationProfileBuilder).GetMethod(
                name,
                parameterTypes);
        Assert.NotNull(method);
        Assert.Equal(typeof(ProductPresentationProfileBuilder), method.ReturnType);
    }

    private static void AssertCallbackRegistration(string methodName, Type targetType)
    {
        var callbackType = typeof(Action<>).MakeGenericType(targetType);

        foreach (var identifierType in new[] { typeof(string), typeof(PropertyBase) })
        {
            var method = typeof(ProductManager).GetMethod(
                methodName,
                new[] { identifierType, callbackType, typeof(bool) });

            Assert.NotNull(method);
            Assert.Equal(typeof(void), method.ReturnType);
            Assert.Equal("allowDefaultEffect", method.GetParameters()[2].Name);
            Assert.True(method.GetParameters()[2].IsOptional);
            Assert.Equal(false, method.GetParameters()[2].DefaultValue);
        }
    }

    private static void AssertCallbackRemoval(string methodName)
    {
        foreach (var identifierType in new[] { typeof(string), typeof(PropertyBase) })
        {
            var method = typeof(ProductManager).GetMethod(methodName, new[] { identifierType });

            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method.ReturnType);
        }
    }
}
