using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SettingsPopup : UI_Popup
{
	enum GameObjects
	{
		CloseArea,
	}

	enum Buttons
	{
		CloseButton,
	}

	enum Texts
	{
		BGMValueText,
		SFXValueText,
	}

	enum Sliders
	{
		BGMSlider,
		SFXSlider,
	}

	enum Toggles
	{
		VibrationToggle,
	}

	private const string PREF_VIBRATION = "Settings_Vibration";

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		BindObjects(typeof(GameObjects));
		BindButtons(typeof(Buttons));
		BindTexts(typeof(Texts));
		BindSliders(typeof(Sliders));
		BindToggles(typeof(Toggles));

		GetObject((int)GameObjects.CloseArea).BindEvent(OnClickCloseArea);
		GetButton((int)Buttons.CloseButton).gameObject.BindEvent(OnClickCloseButton);

		GetSlider((int)Sliders.BGMSlider).onValueChanged.AddListener(OnBGMSliderChanged);
		GetSlider((int)Sliders.SFXSlider).onValueChanged.AddListener(OnSFXSliderChanged);
		GetToggle((int)Toggles.VibrationToggle).onValueChanged.AddListener(OnVibrationToggleChanged);

		Refresh();

		return true;
	}

	void Refresh()
	{
		if (_init == false)
			return;

		Slider bgmSlider = GetSlider((int)Sliders.BGMSlider);
		Slider sfxSlider = GetSlider((int)Sliders.SFXSlider);
		Toggle vibrationToggle = GetToggle((int)Toggles.VibrationToggle);

		// Remove listeners before setting values to avoid redundant callbacks
		bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
		sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
		vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChanged);

		// Load current values
		bgmSlider.value = Managers.Sound.BgmVolume;
		sfxSlider.value = Managers.Sound.SfxVolume;
		vibrationToggle.isOn = PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;

		// Update volume text displays
		GetText((int)Texts.BGMValueText).text = Mathf.RoundToInt(bgmSlider.value * 100).ToString();
		GetText((int)Texts.SFXValueText).text = Mathf.RoundToInt(sfxSlider.value * 100).ToString();

		// Re-add listeners
		bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
		sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
		vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
	}

	void OnBGMSliderChanged(float value)
	{
		Managers.Sound.SetBgmVolume(value);
		GetText((int)Texts.BGMValueText).text = Mathf.RoundToInt(value * 100).ToString();
	}

	void OnSFXSliderChanged(float value)
	{
		Managers.Sound.SetSfxVolume(value);
		GetText((int)Texts.SFXValueText).text = Mathf.RoundToInt(value * 100).ToString();
	}

	void OnVibrationToggleChanged(bool isOn)
	{
		PlayerPrefs.SetInt(PREF_VIBRATION, isOn ? 1 : 0);
	}

	void SaveAndClose()
	{
		Managers.Sound.SaveSettings();
		PlayerPrefs.Save();
		Managers.UI.ClosePopupUI(this);
	}

	void OnClickCloseArea(PointerEventData evt)
	{
		SaveAndClose();
	}

	void OnClickCloseButton(PointerEventData evt)
	{
		SaveAndClose();
	}
}
