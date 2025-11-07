#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

using System;
using S1API.Internal.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace S1API.UI
{
    /// <summary>
    /// Utility class for constructing and configuring various UI elements in Unity.
    /// </summary>
    /// <remarks>
    /// Provides static helpers for building complex hierarchies—panels, text, layouts, scroll views, buttons, etc.—
    /// while handling RectTransform setup, layout components, and consistent styling in one place.
    /// </remarks>
    public static class UIFactory
    {
        /// <summary>
        /// Creates a UI panel GameObject with an Image background and configurable anchoring.
        /// </summary>
        /// <param name="name">Name of the panel GameObject.</param>
        /// <param name="parent">Transform that becomes the parent of the panel.</param>
        /// <param name="bgColor">Color applied to the panel background.</param>
        /// <param name="anchorMin">Optional minimum anchor; defaults to centered anchor.</param>
        /// <param name="anchorMax">Optional maximum anchor; defaults to centered anchor.</param>
        /// <param name="fullAnchor">When true, stretches the panel to fill its parent regardless of anchor arguments.</param>
        /// <returns>The created panel GameObject (with RectTransform/Image components attached).</returns>
        public static GameObject Panel(string name, Transform parent, Color bgColor, Vector2? anchorMin = null,
            Vector2? anchorMax = null, bool fullAnchor = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (fullAnchor)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
                rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            }

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            return go;
        }

        /// <summary>
        /// Creates a `UnityEngine.UI.Text` element configured with the supplied content and styling.
        /// </summary>
        /// <param name="name">Name of the GameObject to create.</param>
        /// <param name="content">Initial string displayed inside the text component.</param>
        /// <param name="parent">Transform that will contain the new text element.</param>
        /// <param name="fontSize">Font size to apply, defaults to 14.</param>
        /// <param name="anchor">Text alignment; defaults to <see cref="TextAnchor.UpperLeft"/>.</param>
        /// <param name="style">Font style flag; defaults to <see cref="FontStyle.Normal"/>.</param>
        /// <returns>The configured <see cref="Text"/> component.</returns>
        public static Text Text(string name, string content, Transform parent, int fontSize = 14,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.fontStyle = style;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        /// <summary>
        /// Builds a ScrollRect hierarchy configured for vertical scrolling and returns the content RectTransform.
        /// </summary>
        /// <param name="name">Name of the root scroll view GameObject.</param>
        /// <param name="parent">Transform that will own the scroll view.</param>
        /// <param name="scrollRect">Outputs the created <see cref="ScrollRect"/> component.</param>
        /// <returns>The RectTransform of the content container where list items should be added.</returns>
        public static RectTransform ScrollableVerticalList(string name, Transform parent, out ScrollRect scrollRect)
        {
            var scrollGO = new GameObject(name);
            scrollGO.transform.SetParent(parent, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.05f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRT;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            return contentRT;
        }

        /// <summary>
        /// Ensures a content <see cref="RectTransform"/> grows tall enough to fit its children.
        /// </summary>
        /// <remarks>
        /// Adds a <see cref="ContentSizeFitter"/> if one is missing and configures it for preferred vertical sizing.
        /// </remarks>
        /// <param name="content">The RectTransform whose vertical size should adapt to its children.</param>
        public static void FitContentHeight(RectTransform content)
        {
            var fitter = content.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// Cached sprite with rounded corners generated on demand by <see cref="GetRoundedSprite"/>.
        /// </summary>
        private static Sprite roundedSprite;

        /// <summary>
        /// Creates a rounded button composed of a mask container, button, and centered text label.
        /// </summary>
        /// <param name="name">Name of the underlying button GameObject.</param>
        /// <param name="label">Text shown inside the button.</param>
        /// <param name="parent">Parent transform for the mask container.</param>
        /// <param name="bgColor">Background color applied to the inner button.</param>
        /// <param name="width">Preferred width for the control (also applied to a LayoutElement).</param>
        /// <param name="height">Preferred height for the control.</param>
        /// <param name="fontSize">Font size for the label.</param>
        /// <param name="textColor">Color of the label text.</param>
        /// <returns>The tuple (mask container GameObject, Button component, Text component).</returns>
        public static (GameObject, Button, Text) RoundedButtonWithLabel(string name, string label, Transform parent,
                                        Color bgColor, float width, float height ,   int fontSize ,Color textColor)
        {
            // Create mask container with rounded corners
            var maskGO = new GameObject(name + "_RoundedMask");
            maskGO.transform.SetParent(parent, false);

            var maskRT = maskGO.AddComponent<RectTransform>();
            maskRT.sizeDelta = new Vector2(width, height);

            // ✅ Add layout element for layout sizing
            var layoutElement = maskGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;

            var maskImage = maskGO.AddComponent<Image>();
            maskImage.sprite = GetRoundedSprite();
            maskImage.type = Image.Type.Sliced;
            maskImage.color = Color.white;

            var mask = maskGO.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Create actual button inside mask
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(maskGO.transform, false);

            var rt = buttonGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = buttonGO.AddComponent<Image>();
            img.color = bgColor;
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;

            var btn = buttonGO.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label
            var textGO = new GameObject("Label");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = textColor;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return (maskGO, btn, txt);
        }


        /// <summary>
        /// Generates (and caches) a rounded-corner sprite suitable for sliced UI backgrounds.
        /// </summary>
        /// <returns>An in-memory sprite with uniform borders that can be reused across controls.</returns>
        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
                return roundedSprite;

            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Color32 transparent = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);

            float radius = 6f; // radius for rounded corners

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isCorner =
                        (x < radius && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius) || // bottom-left
                        (x > size - radius - 1 && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, radius)) > radius) || // bottom-right
                        (x < radius && y > size - radius - 1 && Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius - 1)) > radius) || // top-left
                        (x > size - radius - 1 && y > size - radius - 1 && Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, size - radius - 1)) > radius); // top-right

                    tex.SetPixel(x, y, isCorner ? transparent : white);
                }
            }

            tex.Apply();

            // Border = safe slicing margins (try 8px for all sides)
            var border = new Vector4(8, 8, 8, 8);
            roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return roundedSprite;
        }

        /// <summary>
        /// Creates a container configured with a <see cref="HorizontalLayoutGroup"/> for arranging buttons in a row.
        /// </summary>
        /// <param name="name">Name of the row GameObject.</param>
        /// <param name="parent">Transform that will hold the row.</param>
        /// <param name="spacing">Spacing between children in the layout (defaults to 12).</param>
        /// <param name="alignment">Child alignment; defaults to <see cref="TextAnchor.MiddleCenter"/>.</param>
        /// <returns>The created row GameObject.</returns>
        public static GameObject ButtonRow(string name, Transform parent, float spacing = 12f, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);

            var rt = row.AddComponent<RectTransform>();
            HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = spacing;
            hLayout.childAlignment = alignment;

            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            return row;
        }

        /// <summary>
        /// Creates a rectangular button with a centered text label and returns useful components.
        /// </summary>
        /// <param name="name">Name assigned to the button GameObject.</param>
        /// <param name="label">Text displayed in the button.</param>
        /// <param name="parent">Parent transform for the button.</param>
        /// <param name="bgColor">Background color for the button image.</param>
        /// <param name="Width">Desired width in pixels.</param>
        /// <param name="Height">Desired height in pixels.</param>
        /// <returns>A tuple of (GameObject, Button, Text) for further configuration.</returns>
        public static (GameObject, Button, Text) ButtonWithLabel(string name, string label, Transform parent,
            Color bgColor, float Width, float Height)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Height, Width);

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return (go, btn, txt);
        }

        /// <summary>
        /// Sets an icon as a child of the specified parent transform with the given sprite.
        /// </summary>
        /// <param name="sprite">The sprite to be used as the icon.</param>
        /// <param name="parent">The transform that will act as the parent of the icon.</param>
        public static void SetIcon(Sprite sprite, Transform parent)
        {
            var icon = new GameObject("Icon");
            icon.transform.SetParent(parent, false);

            var rt = icon.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = icon.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
        }

        /// <summary>
        /// Creates a stacked title/subtitle block and optionally appends a completed label.
        /// </summary>
        /// <param name="parent">Transform that will receive the text elements.</param>
        /// <param name="title">Primary heading displayed in bold.</param>
        /// <param name="subtitle">Secondary descriptive text below the title.</param>
        /// <param name="isCompleted">Adds an "Already Delivered" status line when true.</param>
        public static void CreateTextBlock(Transform parent, string title, string subtitle, bool isCompleted)
        {
            Text(parent.name + "Title", title, parent, 16, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text(parent.name + "Subtitle", subtitle, parent, 14, TextAnchor.UpperLeft);
            if (isCompleted)
                Text("CompletedLabel", "<color=#888888><i>Already Delivered</i></color>", parent, 12,
                    TextAnchor.UpperLeft);
        }

        /// <summary>
        /// Adds a <see cref="Button"/> component to a row container and wires up click/interactability.
        /// </summary>
        /// <param name="go">Target GameObject that already has an <see cref="Image"/> for visuals.</param>
        /// <param name="clickHandler">Callback to invoke on button click.</param>
        /// <param name="enabled">Whether the resulting button should start as interactable.</param>
        public static void CreateRowButton(GameObject go, UnityAction clickHandler, bool enabled)
        {
            var btn = go.AddComponent<Button>();
            var img = go.GetComponent<Image>();
            btn.targetGraphic = img;
            btn.interactable = enabled;

            btn.onClick.AddListener(clickHandler);
        }

        /// <summary>
        /// Destroys all child GameObjects of the provided parent Transform.
        /// </summary>
        /// <param name="parent">Transform to clear.</param>
        public static void ClearChildren(Transform parent)
        {
            // Use index-based iteration for IL2CPP compatibility to avoid invalid cast during enumeration
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Adds and configures a <see cref="VerticalLayoutGroup"/> on the supplied GameObject.
        /// </summary>
        /// <param name="go">GameObject that should host the layout.</param>
        /// <param name="spacing">Spacing between children in pixels (default 10).</param>
        /// <param name="padding">Optional padding override; defaults to 10px on every side.</param>
        public static void VerticalLayoutOnGO(GameObject go, int spacing = 10, RectOffset? padding = null)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset(10, 10, 10, 10);
        }

        /// <summary>
        /// Constructs a quest row entry with dedicated icon/text panels and common layout settings.
        /// </summary>
        /// <param name="name">Name suffix applied to generated GameObjects.</param>
        /// <param name="parent">Parent transform for the row.</param>
        /// <param name="iconPanel">Outputs the panel intended for quest icons.</param>
        /// <param name="textPanel">Outputs the panel intended for quest title/description.</param>
        /// <returns>The fully constructed row GameObject.</returns>
        public static GameObject CreateQuestRow(string name, Transform parent, out GameObject iconPanel,
            out GameObject textPanel)
        {
            // Create the main row object
            var row = new GameObject("Row_" + name);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 90f); // Let layout handle width
            row.AddComponent<LayoutElement>().minHeight = 50f;
            row.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.2f); // or Image line separator below
            
            
            var line = UIFactory.Panel("Separator", row.transform, new Color(1,1,1,0.05f));
            line.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 1f);

            // Add background + target graphic
            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f);

            var button = row.AddComponent<Button>();
            button.targetGraphic = bg;

            // Layout group
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(75, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = 90f;
            rowLE.flexibleWidth = 1;

            // Icon panel
            iconPanel = Panel("IconPanel", row.transform, new Color(0.12f, 0.12f, 0.12f));
            var iconRT = iconPanel.GetComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(80f, 80f);
            var iconLE = iconPanel.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 80f;
            iconLE.preferredHeight = 80f;

            // Text panel
            textPanel = Panel("TextPanel", row.transform, Color.clear);
            VerticalLayoutOnGO(textPanel, spacing: 2);
            var textLE = textPanel.AddComponent<LayoutElement>();
            textLE.minWidth = 200f;
            textLE.flexibleWidth = 1;

            return row;
        }

        /// <summary>
        /// Creates a top-bar container with padding, title text, and layout metadata.
        /// </summary>
        /// <param name="name">Name of the bar GameObject.</param>
        /// <param name="parent">Transform that will contain the bar.</param>
        /// <param name="title">Display text shown in the bar.</param>
        /// <param name="topbarSize">Normalized height (Y anchor) reserved for the bar.</param>
        /// <param name="paddingLeft">Left padding applied by the layout group.</param>
        /// <param name="paddingRight">Right padding applied by the layout group.</param>
        /// <param name="paddingTop">Top padding applied by the layout group.</param>
        /// <param name="paddingBottom">Bottom padding applied by the layout group.</param>
        /// <returns>The instantiated bar GameObject.</returns>
        public static GameObject TopBar(string name, Transform parent, string title,
            float topbarSize,
            int paddingLeft, int paddingRight, int paddingTop, int paddingBottom)
        {
            var topBar = Panel(name, parent, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(0f, topbarSize), new Vector2(1f, 1f));

            var layout = topBar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Title
            var titleText = Text("TopBarTitle", title, topBar.transform, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minWidth = 300;
            titleLayout.flexibleWidth = 1;

            return topBar;
        }

        /// <summary>
        /// Adds a <see cref="HorizontalLayoutGroup"/> to a GameObject and preconfigures its sizing behavior.
        /// </summary>
        /// <param name="go">GameObject that should host the layout group.</param>
        /// <param name="spacing">Spacing between children; defaults to 10.</param>
        /// <param name="padLeft">Left padding value.</param>
        /// <param name="padRight">Right padding value.</param>
        /// <param name="padTop">Top padding value.</param>
        /// <param name="padBottom">Bottom padding value.</param>
        /// <param name="alignment">Child alignment; defaults to <see cref="TextAnchor.MiddleCenter"/>.</param>
        public static void HorizontalLayoutOnGO(GameObject go, int spacing = 10, int padLeft = 0, int padRight = 0, int padTop = 0, int padBottom = 0, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(padLeft, padRight, padTop, padBottom);
        }

        /// <summary>
        /// Convenience helper to configure the padding on any <see cref="LayoutGroup"/>.
        /// </summary>
        /// <param name="layoutGroup">Target layout group.</param>
        /// <param name="left">Left padding value.</param>
        /// <param name="right">Right padding value.</param>
        /// <param name="top">Top padding value.</param>
        /// <param name="bottom">Bottom padding value.</param>
        public static void SetLayoutGroupPadding(LayoutGroup layoutGroup, int left, int right, int top, int bottom)
        {
            layoutGroup.padding = new RectOffset(left, right, top, bottom);
        }


        /// <summary>
        /// Updates a button's display text and binds the supplied click callback.
        /// </summary>
        /// <param name="btn">Button to configure.</param>
        /// <param name="label">Text component associated with the button.</param>
        /// <param name="text">Text to assign to the label.</param>
        /// <param name="callback">Delegate invoked when the button is clicked.</param>
        public static void BindAcceptButton(Button btn, Text label, string text, UnityAction callback)
        {
            label.text = text;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(callback);
        }
    }
}

        /// <summary>
        /// Represents a handler that encapsulates a callback action to be invoked when a click event occurs.
        /// </summary>
        /// <remarks>
        /// This class provides a mechanism to handle and execute logic when a click event is triggered.
        /// It associates an action defined by a UnityAction delegate with the click event.
        /// </remarks>
        public class ClickHandler
        {
            /// <summary>
            /// A private field that stores the UnityAction delegate to be executed when a specific UI interaction or click event occurs.
            /// </summary>
            private readonly UnityAction _callback;

            /// <summary>
            /// Initializes a new <see cref="ClickHandler"/> with the callback invoked on `OnClick`.
            /// </summary>
            /// <param name="callback">UnityAction delegate to execute when a click event is triggered.</param>
            public ClickHandler(UnityAction callback)
            {
                _callback = callback;
            }

            /// <summary>
            /// Invokes the stored callback to satisfy whatever click interaction this handler represents.
            /// </summary>
            /// <remarks>
            /// Simply forwards to the UnityAction instance supplied at construction time.
            /// </remarks>
            public void OnClick()
            {
                _callback.Invoke();
            }
        }
