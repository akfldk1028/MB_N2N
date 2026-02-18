# UI_SettingsPopup Prefab Setup Guide

> Unity Editor prefab creation guide for the Settings Popup.
> Prefab hierarchy names must **exactly** match enum values in `UI_SettingsPopup.cs`.

---

## 1. Prefab Hierarchy (프리팹 계층 구조)

```
UI_SettingsPopup          [Root GameObject]
 +-- CloseArea            [Full-screen transparent background for tap-to-close]
 +-- Panel                [Visual background panel - optional decoration]
 |    +-- TitleText       [Optional: "Settings" / "설정" label]
 |    +-- BGMLabel        [Optional: "BGM" label]
 |    +-- BGMSlider       [Slider - BGM volume control]
 |    +-- BGMVolumeText   [TMP_Text - shows "75%" etc.]
 |    +-- SFXLabel        [Optional: "SFX" label]
 |    +-- SFXSlider       [Slider - SFX volume control]
 |    +-- SFXVolumeText   [TMP_Text - shows "75%" etc.]
 |    +-- VibrationToggle [Toggle - vibration on/off]
 |    +-- CloseButton     [Button - close/save settings]
```

> **CRITICAL**: The following 7 child names are **mandatory** and must match exactly (case-sensitive).
> Optional items (TitleText, BGMLabel, SFXLabel, Panel) are for visual styling only.
> `UI_Base.Bind<T>()` uses `Util.FindChild()` with `recursive=true`,
> so bound children can be nested at any depth under the root.

---

## 2. Required GameObjects & Components (필수 오브젝트 및 컴포넌트)

### 2.1 Root: `UI_SettingsPopup`

| Component | Settings |
|---|---|
| **RectTransform** | Stretch to fill parent canvas |
| **Canvas** | Auto-configured by `UI_Popup.Init()` via `Managers.UI.SetCanvas()` |
| **CanvasScaler** | Auto-configured: ScaleWithScreenSize, 1080x1920 |
| **GraphicRaycaster** | Auto-added by `SetCanvas()` |
| **UI_SettingsPopup.cs** | Drag `Assets/@Scripts/UI/Popup/UI_SettingsPopup.cs` onto this root object |

> `UI_Popup.Init()` calls `Managers.UI.SetCanvas(gameObject, true)` which automatically
> adds Canvas, CanvasScaler (1080x1920), and GraphicRaycaster. You only need to attach
> the `UI_SettingsPopup` script component.

---

### 2.2 `CloseArea` (enum: `GameObjects.CloseArea`)

Transparent full-screen area for "tap outside to close" behaviour.

| Component | Settings |
|---|---|
| **RectTransform** | Anchors: Stretch-Stretch (all corners), Offsets: all 0 (full screen) |
| **Image** | Color: `RGBA(0, 0, 0, 0.5)` or fully transparent `RGBA(0, 0, 0, 0)` — must have Image component for raycast detection |

> **Why Image is required**: The `BindEvent()` system adds a `UI_EventHandler` which needs
> `Graphic` for raycasting. An Image (even transparent) satisfies this requirement.
> Set **Raycast Target = true**.

---

### 2.3 `CloseButton` (enum: `Buttons.CloseButton`)

Standard close button, typically "X" icon in top-right of the panel.

| Component | Settings |
|---|---|
| **RectTransform** | Position: top-right of Panel. Suggested size: 80x80 |
| **Image** | Button background / "X" icon sprite |
| **Button** | Required — `UI_Base.Bind<Button>()` searches for this component |

> Child structure: `CloseButton` may have a child `Text (TMP)` or `Image` for the "X" icon.

---

### 2.4 `BGMVolumeText` (enum: `Texts.BGMVolumeText`)

Displays BGM volume as percentage (e.g., "75%").

| Component | Settings |
|---|---|
| **RectTransform** | Position: next to BGMSlider. Suggested size: 120x50 |
| **TextMeshPro - Text (UI)** | Font Size: 28-36, Alignment: Center, Default text: "100%" |

> `UI_Base.Bind<TMP_Text>()` requires **TextMeshProUGUI** (TMP_Text), not legacy `Text`.

---

### 2.5 `SFXVolumeText` (enum: `Texts.SFXVolumeText`)

Displays SFX volume as percentage (e.g., "75%").

| Component | Settings |
|---|---|
| **RectTransform** | Position: next to SFXSlider. Suggested size: 120x50 |
| **TextMeshPro - Text (UI)** | Font Size: 28-36, Alignment: Center, Default text: "100%" |

---

### 2.6 `BGMSlider` (enum: `Sliders.BGMSlider`)

Controls BGM volume. Value maps directly to `SoundManager.SetBgmVolume(float 0-1)`.

| Component | Settings |
|---|---|
| **RectTransform** | Suggested size: 600x50, centered horizontally in Panel |
| **Slider** | **Min Value: 0**, **Max Value: 1**, **Whole Numbers: false**, Direction: Left To Right, Value: 1 (default full volume) |

Standard Unity Slider child hierarchy (auto-created):
```
BGMSlider
 +-- Background          [Image - slider track background]
 +-- Fill Area
 |    +-- Fill            [Image - filled portion of track]
 +-- Handle Slide Area
      +-- Handle          [Image - draggable handle]
```

> **Important**: `Whole Numbers` must be **unchecked** (false). The slider value is a float 0.0-1.0
> that maps directly to `Managers.Sound.SetBgmVolume(value)`.

---

### 2.7 `SFXSlider` (enum: `Sliders.SFXSlider`)

Controls SFX volume. Same configuration as BGMSlider.

| Component | Settings |
|---|---|
| **RectTransform** | Suggested size: 600x50, centered horizontally in Panel |
| **Slider** | **Min Value: 0**, **Max Value: 1**, **Whole Numbers: false**, Direction: Left To Right, Value: 1 |

Same child hierarchy as BGMSlider (Background, Fill Area, Handle Slide Area).

---

### 2.8 `VibrationToggle` (enum: `Toggles.VibrationToggle`)

Vibration on/off toggle. State saved to `PlayerPrefs.SetInt("Settings_Vibration", isOn ? 1 : 0)`.

| Component | Settings |
|---|---|
| **RectTransform** | Suggested size: 200x60 |
| **Toggle** | **Is On: true** (default vibration enabled) |

Standard Unity Toggle child hierarchy:
```
VibrationToggle
 +-- Background          [Image - toggle track/background]
 |    +-- Checkmark       [Image - checkmark or "on" indicator]
 +-- Label               [Optional: TMP_Text "Vibration" / "진동"]
```

> Assign the `Checkmark` Image to the Toggle component's **Graphic** field in Inspector.

---

## 3. Layout Guidance — 1080x1920 Portrait Canvas (레이아웃 가이드)

The canvas reference resolution is **1080 x 1920** (portrait mobile), auto-configured by
`UIManager.SetCanvas()` with `CanvasScaler.ScaleMode.ScaleWithScreenSize`.

### Suggested Layout (approximate RectTransform values)

```
+-------------------------------------------+  (0, 1920)
|                                           |
|              CloseArea                    |  <- Full-screen stretch (anchors 0,0 to 1,1)
|         (transparent overlay)             |
|                                           |
|    +-------------------------------+      |
|    |        Settings Panel         |      |  <- Centered, ~800x900
|    |                               |      |
|    |   [X] CloseButton       (TR)  |      |  <- Top-right corner of panel
|    |                               |      |
|    |   BGM  [====|====] 75%        |      |  <- Y ~+200 from center
|    |         BGMSlider  BGMVolText  |      |
|    |                               |      |
|    |   SFX  [====|====] 50%        |      |  <- Y ~+50 from center
|    |         SFXSlider  SFXVolText  |      |
|    |                               |      |
|    |   Vibration  [v] On           |      |  <- Y ~-100 from center
|    |         VibrationToggle        |      |
|    |                               |      |
|    +-------------------------------+      |
|                                           |
+-------------------------------------------+  (1080, 0)
```

### Recommended Anchoring

| Element | Anchor | Pivot | Position (approx.) |
|---|---|---|---|
| CloseArea | Stretch-Stretch (0,0)-(1,1) | (0.5, 0.5) | Offsets all 0 |
| Panel | Center-Center | (0.5, 0.5) | (0, 0), Size: (800, 900) |
| CloseButton | Top-Right of Panel | (1, 1) | (-40, -40), Size: (80, 80) |
| BGMSlider | Mid-Center of Panel | (0.5, 0.5) | (-60, 200), Size: (500, 50) |
| BGMVolumeText | Mid-Right of BGMSlider | (0, 0.5) | (right of slider), Size: (120, 50) |
| SFXSlider | Mid-Center of Panel | (0.5, 0.5) | (-60, 50), Size: (500, 50) |
| SFXVolumeText | Mid-Right of SFXSlider | (0, 0.5) | (right of slider), Size: (120, 50) |
| VibrationToggle | Mid-Center of Panel | (0.5, 0.5) | (0, -100), Size: (200, 60) |

> These are **approximate** values. Adjust to match your game's visual design.
> Use **Vertical Layout Group** on the Panel for automatic spacing if preferred.

---

## 4. Addressable Registration (어드레서블 등록)

The `UIManager.ShowPopupUI<T>()` method loads prefabs via `Managers.Resource.Instantiate(name)`,
where `name` is the class type name. The prefab **must** be registered as an Addressable asset.

### Steps

1. **Save as Prefab**: Drag the completed `UI_SettingsPopup` root GameObject from the
   Hierarchy into `Assets/@Resources/Prefabs/UI/Popup/` folder (or your project's UI prefab folder).

2. **Mark as Addressable**:
   - Select the prefab asset in Project window.
   - In the Inspector, check the **Addressable** checkbox (top-left of Inspector).
   - The address key will auto-fill. **Change it to exactly**:
     ```
     UI_SettingsPopup
     ```
   - This must match the class name `UI_SettingsPopup` exactly (case-sensitive).

3. **Add to PreLoad Label**:
   - With the prefab still selected, in the Addressable section of Inspector, click the label dropdown.
   - Add the label: **`PreLoad`**
   - This ensures the prefab is loaded during the initial asset preloading phase.

4. **Verify in Addressables Groups Window**:
   - Open: **Window > Asset Management > Addressables > Groups**
   - Confirm `UI_SettingsPopup` appears in the correct group with:
     - Address: `UI_SettingsPopup`
     - Labels: `PreLoad`

> **Why PreLoad?** The `CacheAllPopups()` method in UIManager uses reflection to find all
> `UI_Popup` subclasses and pre-instantiate them. The PreLoad label ensures the asset is
> available when this caching occurs.

---

## 5. Script Attachment (스크립트 연결)

Attach the `UI_SettingsPopup.cs` script to the **root GameObject** (`UI_SettingsPopup`).

1. Select the root `UI_SettingsPopup` GameObject.
2. In Inspector, click **Add Component**.
3. Search for `UI_SettingsPopup` and add it.

> Do **not** add `UI_Popup`, `UI_Base`, or `Canvas` components manually.
> They are either inherited (`UI_Popup` is the base class) or auto-added by
> `Managers.UI.SetCanvas()` at runtime during `Init()`.

---

## 6. Verification Checklist (검증 체크리스트)

After creating the prefab, verify the following:

- [ ] Root GameObject named exactly `UI_SettingsPopup`
- [ ] `UI_SettingsPopup` script component attached to root
- [ ] Child `CloseArea` exists with **Image** component (Raycast Target = true)
- [ ] Child `CloseButton` exists with **Button** component
- [ ] Child `BGMVolumeText` exists with **TextMeshPro - Text (UI)** component
- [ ] Child `SFXVolumeText` exists with **TextMeshPro - Text (UI)** component
- [ ] Child `BGMSlider` exists with **Slider** component (Min=0, Max=1, WholeNumbers=false)
- [ ] Child `SFXSlider` exists with **Slider** component (Min=0, Max=1, WholeNumbers=false)
- [ ] Child `VibrationToggle` exists with **Toggle** component (IsOn=true)
- [ ] Prefab marked as Addressable with address: `UI_SettingsPopup`
- [ ] Addressable label: `PreLoad` applied
- [ ] No Slider `onValueChanged` listeners wired in Inspector (all wired in code via `Init()`)
- [ ] No Button `OnClick` listeners wired in Inspector (all wired in code via `BindEvent()`)
- [ ] Play mode test: StartUpScene > RecipeButton opens popup, sliders/toggle work, close saves

---

## 7. Quick Reference — Enum to GameObject Mapping

| Enum Type | Enum Value | GameObject Name | Required Component |
|---|---|---|---|
| `GameObjects` | `CloseArea` | `CloseArea` | Image |
| `Buttons` | `CloseButton` | `CloseButton` | Button |
| `Texts` | `BGMVolumeText` | `BGMVolumeText` | TMP_Text (TextMeshProUGUI) |
| `Texts` | `SFXVolumeText` | `SFXVolumeText` | TMP_Text (TextMeshProUGUI) |
| `Sliders` | `BGMSlider` | `BGMSlider` | Slider |
| `Sliders` | `SFXSlider` | `SFXSlider` | Slider |
| `Toggles` | `VibrationToggle` | `VibrationToggle` | Toggle |

> `UI_Base.Bind<T>()` uses `Enum.GetNames(type)` to get the enum value name as a string,
> then calls `Util.FindChild<T>(gameObject, name, recursive: true)` to locate the child.
> **If names don't match exactly, binding will fail** with log: `"Failed to bind(EnumName)"`.
