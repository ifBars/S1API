using System.Globalization;
using S1API.Internal.Console;
using S1API.Internal.Rendering;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Tests.Rendering;

public sealed class PresentationWorkbenchTests
{
    [Fact]
    public void ConsoleCommandUsesTheCompactCommandWord()
    {
        Assert.Equal(
            "presentationworkbench",
            new PresentationWorkbenchCommand().CommandWord);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("PRODUCT")]
    [InlineData("item")]
    public void ResolverRecognizesInternalTargetKinds(string value)
    {
        Assert.True(PresentationWorkbenchResolver.IsTargetKind(value));
    }

    [Fact]
    public void ResolverDoesNotExposeStandaloneAvatarTargets()
    {
        Assert.False(PresentationWorkbenchResolver.IsTargetKind("avatar"));
    }

    [Fact]
    public void ResolverRejectsUnknownTargetWithoutUsingRuntimeRegistries()
    {
        Assert.False(
            PresentationWorkbenchResolver.TryResolve(
                "example",
                "unknown",
                out PresentationWorkbenchDefinition? definition,
                out string failure));
        Assert.Null(definition);
        Assert.Contains("Unknown presentation target", failure);
    }

    [Fact]
    public void WorkbenchDoesNotExportAModFacingApi()
    {
        Type[] exportedTypes = typeof(IconFactory).Assembly.GetExportedTypes();

        Assert.DoesNotContain(
            exportedTypes,
            type => type.Name.StartsWith(
                "PresentationWorkbench",
                StringComparison.Ordinal));
    }

    [Fact]
    public void InputBindingsUseDistinctDelegatesForSharedCallbacks()
    {
        Action<string> callback = _ => { };

        Action<string> first =
            PresentationWorkbenchView.CreateInputBindingAction(callback);
        Action<string> second =
            PresentationWorkbenchView.CreateInputBindingAction(callback);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData(".")]
    [InlineData("+.")]
    [InlineData("-.")]
    [InlineData("1e")]
    [InlineData("1E+")]
    [InlineData("1e-")]
    public void IncompleteNumericInputIsTreatedAsTransient(string value)
    {
        Assert.True(PresentationWorkbenchView.IsIncompleteNumericInput(value));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("--1")]
    [InlineData("1.5")]
    [InlineData("1e2")]
    public void CompleteOrInvalidNumericInputIsNotTransient(string value)
    {
        Assert.False(PresentationWorkbenchView.IsIncompleteNumericInput(value));
    }

#if MONOMELON
    [Fact]
    public void ExportUsesExistingApisAndInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            string product =
                PresentationWorkbenchExporter.FormatTransform(
                    PresentationWorkbenchExportKind.ProductTransform,
                    new Vector3(0f, -0.16f, 0f),
                    new Vector3(0f, 270f, 0f),
                    Vector3.one * 2.25f);
            string itemVisual =
                PresentationWorkbenchExporter.FormatTransform(
                    PresentationWorkbenchExportKind.LocalTransformAssignments,
                    Vector3.zero,
                    new Vector3(0f, 180f, 0f),
                    Vector3.one * 1.8f);
            string icon =
                PresentationWorkbenchExporter.FormatIcon(
                    new Vector3(18.5f, -32f, 0f),
                    Vector3.one * 0.45f,
                    fitToCamera: false,
                    cameraFill: 0.8f,
                    size: 512);

            Assert.StartsWith("new ProductPresentationTransform(", product);
            Assert.StartsWith(
                "visual.transform.localPosition =",
                itemVisual);
            Assert.Contains("IconFactory.GenerateIconSprite(", icon);
            Assert.Contains("new Vector3(18.5f, -32f, 0f)", icon);
            Assert.Contains("cameraFill: 0.8f", icon);
            Assert.DoesNotContain("PresentationWorkbench", icon);
            Assert.DoesNotContain("18,5", icon);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
#endif
}
