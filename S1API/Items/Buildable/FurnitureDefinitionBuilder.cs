using System;
using System.Collections.Generic;
using S1API.Internal.Building;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Composes a custom model into a native furniture definition, placement prefab, and inventory prefab.
    /// </summary>
    public sealed class FurnitureDefinitionBuilder
    {
        private string? _id;
        private string? _name;
        private string? _description;
        private GameObject? _model;
        private string? _donorId;
        private IReadOnlyList<FurnitureFootprintCoordinate>? _donorFootprint;
        private bool _modelIsBuilderOwned;
        private bool _modelConfigured;
        private bool _centerModelOnFootprint = true;
        private bool _isolateRepresentationMaterials;
        private FurniturePlacementMode _placementMode = FurniturePlacementMode.Grid;
        private int _footprintWidth = 1;
        private int _footprintDepth = 1;
        private FurnitureSurfaceType _surfaceTypes = FurnitureSurfaceType.Wall;
        private bool _allowSurfaceRotation = true;
        private BuildSoundType _buildSound = FurnitureBuildSoundMapper.Default;
        private int _stackLimit = 10;
        private float _purchasePrice = 10f;
        private float _resellMultiplier = 0.5f;
        private Sprite? _icon;
        private Sprite? _fallbackIcon;
        private bool _generateIcon = true;
        private int _generatedIconResolution = 512;

        internal FurnitureDefinitionBuilder()
        {
        }

        internal FurnitureDefinitionBuilder(FurnitureCloneSource source)
        {
            _donorId = source.DonorId;
            _model = source.Model;
            _modelIsBuilderOwned = true;
            _centerModelOnFootprint = false;
            _isolateRepresentationMaterials = true;
            _placementMode = source.PlacementMode;
            _donorFootprint = source.Footprint;
            if (source.Footprint != null)
            {
                foreach (FurnitureFootprintCoordinate coordinate in source.Footprint)
                {
                    _footprintWidth = Math.Max(_footprintWidth, coordinate.X + 1);
                    _footprintDepth = Math.Max(_footprintDepth, coordinate.Y + 1);
                }
            }

            _surfaceTypes = source.SurfaceTypes;
            _allowSurfaceRotation = source.AllowSurfaceRotation;
            _buildSound = source.BuildSound;
            _stackLimit = source.StackLimit;
            _purchasePrice = source.PurchasePrice;
            _resellMultiplier = source.ResellMultiplier;
            _fallbackIcon = source.Icon;
        }

        /// <summary>Sets the stable registry ID and player-facing text.</summary>
        /// <param name="id">The stable item ID shared by every multiplayer peer.</param>
        /// <param name="name">The player-facing item name.</param>
        /// <param name="description">The player-facing item description. An empty description is allowed.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public FurnitureDefinitionBuilder WithBasicInfo(string id, string name, string description)
        {
            _id = id;
            _name = name;
            _description = description;
            return this;
        }

        /// <summary>
        /// Sets the model used for the placed object, placement ghost, stored item, and generated icon.
        /// The supplied object is cloned and is never modified by S1API.
        /// </summary>
        /// <param name="model">The model root to clone. It must contain at least one mesh renderer.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
        public FurnitureDefinitionBuilder WithModel(GameObject model)
        {
            if (ReferenceEquals(model, null) || model == null)
                throw new ArgumentNullException(nameof(model));

            if (_modelIsBuilderOwned && ReferenceEquals(_model, model))
                return this;
            if (_modelIsBuilderOwned && _model != null)
                Object.DestroyImmediate(_model);

            _model = model;
            _modelIsBuilderOwned = false;
            _centerModelOnFootprint = true;
            _isolateRepresentationMaterials = false;
            return this;
        }

        /// <summary>
        /// Configures the isolated visual owned by a builder returned from
        /// <see cref="FurnitureCreator.CloneFrom(string)"/>.
        /// Renderer materials are independent from the donor before this callback runs.
        /// </summary>
        /// <param name="configure">A callback that modifies the builder-owned visual clone.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this builder was not created by <c>CloneFrom</c>, or the model was already configured.
        /// </exception>
        public FurnitureDefinitionBuilder ConfigureModel(Action<GameObject> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            if (!_modelIsBuilderOwned || _model == null)
            {
                throw new InvalidOperationException(
                    "ConfigureModel is available only for an unmodified FurnitureCreator.CloneFrom builder.");
            }
            if (_modelConfigured)
                throw new InvalidOperationException("The cloned furniture model is already configured.");

            try
            {
                configure(_model);
                if (_model == null)
                    throw new InvalidOperationException("The furniture model callback destroyed its visual root.");
                _modelConfigured = true;
                return this;
            }
            catch
            {
                if (_model != null)
                    Object.DestroyImmediate(_model);
                _model = null;
                _modelIsBuilderOwned = false;
                throw;
            }
        }

        /// <summary>Chooses the native placement family.</summary>
        /// <param name="placementMode">The grid or surface placement family to compose.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for an undefined placement mode.</exception>
        public FurnitureDefinitionBuilder WithPlacement(FurniturePlacementMode placementMode)
        {
            if (!Enum.IsDefined(typeof(FurniturePlacementMode), placementMode))
                throw new ArgumentOutOfRangeException(nameof(placementMode));

            _placementMode = placementMode;
            return this;
        }

        /// <summary>Sets the floor-grid footprint in 0.5 metre tiles.</summary>
        /// <param name="width">The positive footprint width in grid tiles.</param>
        /// <param name="depth">The positive footprint depth in grid tiles.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either dimension is less than one.</exception>
        public FurnitureDefinitionBuilder WithFootprint(int width, int depth)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Footprint width must be at least one tile.");
            if (depth < 1)
                throw new ArgumentOutOfRangeException(nameof(depth), "Footprint depth must be at least one tile.");

            _footprintWidth = width;
            _footprintDepth = depth;
            _donorFootprint = null;
            return this;
        }

        /// <summary>Configures valid surfaces and rotation for surface-placed furniture.</summary>
        /// <param name="surfaceTypes">One or more supported wall or roof surface flags.</param>
        /// <param name="allowRotation">Whether the player can rotate the item during surface placement.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="surfaceTypes"/> is empty or contains an unknown flag.
        /// </exception>
        public FurnitureDefinitionBuilder WithSurfacePlacement(
            FurnitureSurfaceType surfaceTypes,
            bool allowRotation = true)
        {
            if (surfaceTypes == FurnitureSurfaceType.None ||
                (surfaceTypes & ~FurnitureSurfaceType.All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(surfaceTypes));
            }

            _surfaceTypes = surfaceTypes;
            _allowSurfaceRotation = allowRotation;
            return this;
        }

        /// <summary>Sets the sound family used when placement completes.</summary>
        /// <param name="buildSound">
        /// The public sound family. Plastic uses the native metal sound because the game has no plastic family.
        /// </param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for an undefined sound family.</exception>
        public FurnitureDefinitionBuilder WithBuildSound(BuildSoundType buildSound)
        {
            if (!Enum.IsDefined(typeof(BuildSoundType), buildSound))
                throw new ArgumentOutOfRangeException(nameof(buildSound));

            _buildSound = buildSound;
            return this;
        }

        /// <summary>Sets the purchase and resale values.</summary>
        /// <param name="basePurchasePrice">The non-negative base purchase price.</param>
        /// <param name="resellMultiplier">The resale fraction, clamped between zero and one.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public FurnitureDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _purchasePrice = Mathf.Max(0f, basePurchasePrice);
            _resellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>Sets the inventory stack limit.</summary>
        /// <param name="stackLimit">The stack limit, clamped between 1 and 999.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public FurnitureDefinitionBuilder WithStackLimit(int stackLimit)
        {
            _stackLimit = Mathf.Clamp(stackLimit, 1, 999);
            return this;
        }

        /// <summary>Uses an existing inventory icon.</summary>
        /// <param name="icon">The icon to assign without using the native render rig.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="icon"/> is null.</exception>
        public FurnitureDefinitionBuilder WithIcon(Sprite icon)
        {
            if (ReferenceEquals(icon, null) || icon == null)
                throw new ArgumentNullException(nameof(icon));

            _icon = icon;
            _generateIcon = false;
            return this;
        }

        /// <summary>
        /// Queues inventory-icon generation from the supplied model. The definition registers immediately,
        /// and S1API replaces its fallback icon when the gameplay rendering rig becomes available.
        /// </summary>
        /// <param name="resolution">The square icon resolution from 64 through 2048 pixels.</param>
        /// <returns>This builder for fluent chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="resolution"/> is outside the supported range.
        /// </exception>
        public FurnitureDefinitionBuilder WithGeneratedIcon(int resolution = 512)
        {
            if (resolution < 64 || resolution > 2048)
                throw new ArgumentOutOfRangeException(nameof(resolution), "Icon resolution must be between 64 and 2048 pixels.");

            _generateIcon = true;
            _icon = null;
            _generatedIconResolution = resolution;
            return this;
        }

        /// <summary>
        /// Creates and registers the complete native buildable definition.
        /// Call this after the game's item registry and vanilla furniture definitions are available.
        /// Every multiplayer peer must perform the same registration.
        /// </summary>
        /// <returns>The registered custom furniture definition.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required identity or model configuration is missing, or a native donor is unavailable.
        /// </exception>
        public BuildableItemDefinition Build()
        {
            Validate();

            FurnitureComposition composition = FurniturePrefabComposer.Compose(
                _id!,
                _model!,
                _placementMode,
                _footprintWidth,
                _footprintDepth,
                _surfaceTypes,
                _allowSurfaceRotation,
                _donorFootprint,
                _centerModelOnFootprint,
                _isolateRepresentationMaterials);

            var builder = new BuildableItemDefinitionBuilder(composition.TemplateDefinition)
                .WithBasicInfo(_id!, _name!, _description!, ItemCategory.Furniture)
                .WithNativeBuildSound(FurnitureBuildSoundMapper.ToNative(_buildSound))
                .WithPricing(_purchasePrice, _resellMultiplier)
                .WithStackLimit(_stackLimit)
                .WithBuiltItem(composition.BuiltItem)
                .WithStoredItem(composition.StoredItem.gameObject)
                .WithEquippable(composition.Equippable);
            Sprite? initialIcon = _icon != null ? _icon : _fallbackIcon;
            if (initialIcon != null)
                builder.WithIcon(initialIcon);

            BuildableItemDefinition definition = builder.Build();
            Transform? visual = composition.BuiltItem.transform.Find(
                BuildableGhostRuntime.FurnitureVisualName);
            if (visual == null)
                throw new InvalidOperationException("Composed furniture has no placement visual source.");

            if (_isolateRepresentationMaterials)
            {
                BuildableGhostRuntime.RegisterVisual(
                    _id!,
                    parent =>
                    {
                        GameObject ghostVisual = FurnitureVisualCloner.CloneOwnedVisual(
                            visual.gameObject);
                        ghostVisual.name = BuildableGhostRuntime.FurnitureGhostVisualName;
                        ghostVisual.transform.SetParent(parent, false);
                        return ghostVisual;
                    },
                    replaceExistingVisual: true);
            }
            else
            {
                BuildableGhostRuntime.RegisterVisualSource(
                    _id!,
                    visual.gameObject,
                    BuildableGhostRuntime.FurnitureGhostVisualName,
                    replaceExistingVisual: true);
            }
            if (_generateIcon)
            {
                FurnitureIconRuntime.Queue(
                    definition,
                    visual,
                    _generatedIconResolution,
                    _isolateRepresentationMaterials);
            }

            return definition;
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                throw new InvalidOperationException("Furniture ID must be configured before Build().");
            if (string.IsNullOrWhiteSpace(_name))
                throw new InvalidOperationException("Furniture name must be configured before Build().");
            if (_description == null)
                throw new InvalidOperationException("Furniture description must be configured before Build().");
            if (_model == null)
                throw new InvalidOperationException("Furniture model must be configured before Build().");
            FurnitureClonePolicy.ValidateNewId(_id!, _donorId);
        }
    }
}
