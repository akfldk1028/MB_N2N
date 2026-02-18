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

		// Slider callbacks for real-time volume preview
		GetSlider((int)Sliders.BGMSlider).onValueChanged.AddListener(OnBGMSliderChanged);
		GetSlider((int)Sliders.SFXSlider).onValueChanged.AddListener(OnSFXSliderChanged);

		// Toggle callback for vibration
		GetToggle((int)Toggles.VibrationToggle).onValueChanged.AddListener(OnVibrationToggleChanged);

		Refresh();

		return true;
	}

	void Refresh()
	{
		if (_init == false)
			return;

		// Load current sound settings from SoundManager
		float bgmVolume = Managers.Sound.BgmVolume;
		float sfxVolume = Managers.Sound.SfxVolume;

		GetSlider((int)Sliders.BGMSlider).value = bgmVolume;
		GetSlider((int)Sliders.SFXSlider).value = sfxVolume;

		GetText((int)Texts.BGMValueText).text = Mathf.RoundToInt(bgmVolume * 100).ToString();
		GetText((int)Texts.SFXValueText).text = Mathf.RoundToInt(sfxVolume * 100).ToString();

		// Load vibration setting from PlayerPrefs
		bool vibrationOn = PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
		GetToggle((int)Toggles.VibrationToggle).isOn = vibrationOn;
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
