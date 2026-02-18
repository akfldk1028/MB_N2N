# UI_SettingsPopup Prefab Setup Guide

Unity Editor manual setup instructions for the Settings Popup prefab.
The child names **must match the enum names exactly** for `UI_Base` binding to work.

---

## 1. Create Prefab Hierarchy

Right-click in **Hierarchy** > **UI** > **Canvas**, rename to `UI_SettingsPopup`.

Build the following hierarchy:

```
UI_SettingsPopup          (Canvas - root)
  CloseArea               (Image - transparent full-screen tap area)
  Panel                   (Image - popup background)
    BGMSlider             (Slider)
    SFXSlider             (Slider)
    VibrationToggle       (Toggle)
    CloseButton           (Button)
    BGMValueText          (TextMeshPro - Text)
    SFXValueText          (TextMeshPro - Text)
```

> **WARNING:** Child names are case-sensitive and must match the enum names in
> `UI_SettingsPopup.cs` exactly: `CloseArea`, `BGMSlider`, `SFXSlider`,
> `VibrationToggle`, `CloseButton`, `BGMValueText`, `SFXValueText`.

---

## 2. Root: UI_SettingsPopup (Canvas)

The `UI_Popup.Init()` calls `Managers.UI.SetCanvas()` which automatically configures:
- Render Mode: **Screen Space - Overlay**
- Canvas Scaler: **Scale With Screen Size**, Reference Resolution **1080 x 1920**
- Graphic Raycaster added automatically
- Sorting Order managed by UIManager

**No manual Canvas configuration is needed on the root** - the code handles it.

Attach the `UI_SettingsPopup` script component to this root GameObject.

---

## 3. CloseArea (GameObject with Image)

Full-screen transparent overlay that dismisses the popup when tapped.

| Property | Value |
|----------|-------|
| Component | **Image** |
| Color | `(0, 0, 0, 0)` fully transparent (or `(0, 0, 0, 0.5)` for dim overlay) |
| Raycast Target | **true** (must receive clicks) |
| Anchors | Stretch-Stretch (all four corners) Min (0,0) Max (1,1) |
| Anchored Position | (0, 0) |
| Size Delta | (0, 0) |

> Must be the **first child** (rendered behind Panel) so it covers the full screen
> but the panel renders on top.

---

## 4. Panel (Popup Background)

Container for all settings controls. Centered on screen.

| Property | Value |
|----------|-------|
| Component | **Image** (popup background sprite/color) |
| Color | Use project popup background color/sprite |
| Anchors | Center (0.5, 0.5) |
| Pivot | (0.5, 0.5) |
| Size Delta | ~(700, 900) - adjust to fit content |

Layout suggestion: Use **Vertical Layout Group** on Panel or position children manually.

---

## 5. BGMSlider (Slider)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Slider** |
| Rename to | `BGMSlider` |
| Min Value | **0** |
| Max Value | **1** |
| Whole Numbers | **false** |
| Value | **1** (default full volume) |
| Direction | Left To Right |

Position near the top of the Panel. Add a label above/beside it (e.g., "BGM" TMP_Text - this label can have any name since it's not bound by code).

The slider's internal hierarchy (`Background`, `Fill Area/Fill`, `Handle Slide Area/Handle`) is created automatically by Unity. Leave as-is.

---

## 6. SFXSlider (Slider)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Slider** |
| Rename to | `SFXSlider` |
| Min Value | **0** |
| Max Value | **1** |
| Whole Numbers | **false** |
| Value | **1** (default full volume) |
| Direction | Left To Right |

Position below BGMSlider. Add a label (e.g., "SFX" TMP_Text - any name).

---

## 7. VibrationToggle (Toggle)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Toggle** |
| Rename to | `VibrationToggle` |
| Is On | **true** (default vibration on) |

Position below SFXSlider. The default Toggle hierarchy (`Background`, `Checkmark`, `Label`) is created by Unity. Rename the Label text to "Vibration" or localized equivalent.

---

## 8. CloseButton (Button)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Button - TextMeshPro** |
| Rename to | `CloseButton` |
| Button Text | "Close" / "X" / localized text |

Position at the bottom of Panel or top-right corner (X button style). Follow existing popup button styling from `UI_HeroInfoPopup`.

---

## 9. BGMValueText (TextMeshPro - Text)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Text - TextMeshPro** |
| Rename to | `BGMValueText` |
| Default Text | `100` |
| Font Size | Match project style (e.g., 28-36) |
| Alignment | Center |

Position next to or below BGMSlider to display the current volume percentage (0-100).

---

## 10. SFXValueText (TextMeshPro - Text)

| Property | Value |
|----------|-------|
| Create via | Right-click Panel > **UI > Text - TextMeshPro** |
| Rename to | `SFXValueText` |
| Default Text | `100` |
| Font Size | Match project style (e.g., 28-36) |
| Alignment | Center |

Position next to or below SFXSlider to display the current volume percentage (0-100).

---

## 11. Save as Prefab

1. Drag `UI_SettingsPopup` from **Hierarchy** into the **Project** window at:
   ```
   Assets/@Resources/Move/UI_SettingsPopup.prefab
   ```
   (Same folder as `@UI_StartUpScene.prefab` and other UI prefabs)

2. Delete the instance from the Hierarchy (the prefab is now saved as an asset).

---

## 12. Addressable Configuration

1. Select `UI_SettingsPopup.prefab` in the Project window
2. In the **Inspector**, check the **Addressable** checkbox
3. Set the **Addressable Name (key)** to: `UI_SettingsPopup`
4. Assign to the **UI** group (same group as `UI_BrickGameScene`)
5. Add the **`PreLoad`** label

This ensures the prefab is loaded at startup via:
```csharp
Managers.Resource.LoadAllAsync<Object>("PreLoad", callback)
```

And instantiated at runtime via:
```csharp
Managers.UI.ShowPopupUI<UI_SettingsPopup>()
// internally calls: Managers.Resource.Instantiate("UI_SettingsPopup")
```

---

## 13. Verification Checklist

After setup, verify in Unity Editor:

- [ ] Prefab exists at `Assets/@Resources/Move/UI_SettingsPopup.prefab`
- [ ] Root GameObject named `UI_SettingsPopup` has `UI_SettingsPopup` script attached
- [ ] All child names match enum values exactly (case-sensitive):
  - `CloseArea` has Image component
  - `BGMSlider` has Slider component (min=0, max=1, wholeNumbers=false)
  - `SFXSlider` has Slider component (min=0, max=1, wholeNumbers=false)
  - `VibrationToggle` has Toggle component
  - `CloseButton` has Button component
  - `BGMValueText` has TMP_Text component
  - `SFXValueText` has TMP_Text component
- [ ] Addressable key is `UI_SettingsPopup`
- [ ] Addressable group is `UI`
- [ ] `PreLoad` label is applied
- [ ] 0 compilation errors in Console
- [ ] Play mode: `Managers.UI.ShowPopupUI<UI_SettingsPopup>()` opens the popup correctly

---

## Enum Reference (from UI_SettingsPopup.cs)

```csharp
enum GameObjects { CloseArea }
enum Buttons { CloseButton }
enum Texts { BGMValueText, SFXValueText }
enum Sliders { BGMSlider, SFXSlider }
enum Toggles { VibrationToggle }
```

`UI_Base.Bind<T>()` uses `Util.FindChild()` to locate children by name matching
these enum values. Any mismatch will result in `"Failed to bind(name)"` errors in Console.
