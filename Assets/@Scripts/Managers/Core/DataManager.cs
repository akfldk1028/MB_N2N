/*
 * 데이터 매니저 (DataManager)
 * 
 * 역할:
 * 1. 게임에서 사용되는 모든 데이터(몬스터, 영웅, 스킬 등의 정보)를 로드하고 관리
 * 2. JSON 형태로 저장된 데이터를 Dictionary 형태로 변환하여 빠른 접근 제공
 * 3. 각 데이터 유형별로 별도의 Dictionary를 관리하여 체계적인 데이터 구조 유지
 * 4. 데이터 로딩 인터페이스(ILoader)를 통해 다양한 데이터 유형의 로딩 지원
 * 5. Managers 클래스를 통해 다른 시스템에서 쉽게 접근 가능
 */

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
	Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
	public DataManager()
	{
		GameLogger.SystemStart("DataManager", "생성됨");
	}
	
	public Dictionary<int, Data.MonsterData> MonsterDic { get; private set; } = new Dictionary<int, Data.MonsterData>();
	public Dictionary<int, Data.HeroData> HeroDic { get; private set; } = new Dictionary<int, Data.HeroData>();
	public Dictionary<int, Data.HeroInfoData> HeroInfoDic { get; private set; } = new Dictionary<int, Data.HeroInfoData>();
	public Dictionary<int, Data.SkillData> SkillDic { get; private set; } = new Dictionary<int, Data.SkillData>();
	public Dictionary<int, Data.ProjectileData> ProjectileDic { get; private set; } = new Dictionary<int, Data.ProjectileData>();
	public Dictionary<int, Data.EnvData> EnvDic { get; private set; } = new Dictionary<int, Data.EnvData>();
	public Dictionary<int, Data.EffectData> EffectDic { get; private set; } = new Dictionary<int, Data.EffectData>();
	public Dictionary<int, Data.AoEData> AoEDic { get; private set; } = new Dictionary<int, Data.AoEData>();
	public Dictionary<string, Data.TextData> TextDic { get; private set; } = new Dictionary<string, Data.TextData>();
	


	public void Init()
	{
		MonsterDic = LoadJson<Data.MonsterDataLoader, int, Data.MonsterData>("MonsterData").MakeDict();
		HeroDic = LoadJson<Data.HeroDataLoader, int, Data.HeroData>("HeroData").MakeDict();
		SkillDic = LoadJson<Data.SkillDataLoader, int, Data.SkillData>("SkillData").MakeDict();
		ProjectileDic = LoadJson<Data.ProjectileDataLoader, int, Data.ProjectileData>("ProjectileData").MakeDict();

		TextDic = LoadJson<Data.TextDataLoader, string, Data.TextData>("TextData").MakeDict();


		// ItemDic.Clear();

		// foreach (var item in EquipmentDic)
		// 	ItemDic.Add(item.Key, item.Value);

		// foreach (var item in ConsumableDic)
		// 	ItemDic.Add(item.Key, item.Value);
	}

	private Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
	{
		TextAsset textAsset = Managers.Resource.Load<TextAsset>(path);
		return JsonConvert.DeserializeObject<Loader>(textAsset.text);
	}
}
